using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    [Serializable]
    public class BattleConfig
    {
        public int handSize = 6;
        [Tooltip("제한 턴 수. 추후 적/전투별 데이터로 이동 가능")]
        public int turnLimit = 3;
        public int teleportHpCost = 3;
        [Tooltip("턴당 순간이동 횟수. -1 = 무제한 (미정)")]
        public int teleportLimitPerTurn = -1;
    }

    /// <summary>
    /// 전투 문맥. 전투 하나 동안의 모든 상태 + 상호작용 계약(함수) + 사이클.
    /// 효과·적·타일·유물은 반드시 이 클래스의 public 함수로만 상태를 바꾼다.
    ///
    /// 상태를 바꾸는 함수는 전부 Awaitable이고, 반환되면 연출·연쇄 반응까지 모두 끝난 상태다.
    /// 전투가 끝난 뒤(IsOver)에 호출된 상태 변경 함수는 아무것도 하지 않는다.
    ///
    /// 파일 구성(partial) — 담당자는 자기 파일만 수정한다:
    ///   BattleContext.cs         상태, 조회, 사이클 (전투 흐름)
    ///   BattleContext.Units.cs   피해·사기·상태이상 (효과/상태)
    ///   BattleContext.Board.cs   이동·장애물·타일 효과 (보드/이동)
    ///   BattleContext.Cards.cs   드로우·버림·키워드·에너지 (카드/덱)
    ///   BattleContext.Input.cs   플레이어 선택 (전투 흐름)
    /// 규칙 전체: Docs/CODING_RULES.md
    /// </summary>
    public partial class BattleContext
    {
        // ── 상태 ──
        public readonly RunState Run;
        public readonly BattleConfig Config;
        public readonly BattleEvents Events = new();
        public readonly IBattlePresenter Presenter;
        public readonly IPlayerInput Input;
        public readonly Board Board;
        public readonly PlayerUnit Player = new();
        public readonly List<Enemy> Enemies = new();
        public readonly List<Obstacle> Obstacles = new();
        public readonly CardPiles Piles = new();

        public int Energy;
        public int MaxEnergy;
        public int Turn;
        public int TeleportsThisTurn;
        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
        public bool IsOver => Phase is BattlePhase.Won or BattlePhase.Lost;
        /// <summary>플레이어 입력을 받을 수 있는 상태. 해결 중이거나 전투가 끝났으면 false.</summary>
        public bool CanAct => Phase == BattlePhase.PlayerAction;

        public readonly List<string> Logs = new();

        /// <param name="presenter">null이면 연출 없음</param>
        /// <param name="input">null이면 항상 첫 후보 자동 선택</param>
        public BattleContext(RunState run, BattleConfig config, IEnumerable<EnemyData> enemies,
                             IBattlePresenter presenter = null, IPlayerInput input = null)
        {
            Run = run;
            Config = config;
            Presenter = presenter ?? new NullBattlePresenter();
            Input = input ?? new AutoPlayerInput();
            Board = new Board(run.BoardSize);
            foreach (var e in enemies) Enemies.Add(new Enemy(e));
        }

        // ════════════════════ 조회 ════════════════════

        public Tile GetTile(Vector2Int pos) => Board.Get(pos);
        public List<Enemy> GetEnemies() => Enemies;
        public Enemy GetRandomEnemy()
        {
            var alive = Enemies.Where(e => e.Hp > 0).ToList();
            if (alive.Count == 0) alive = Enemies;
            return alive.Count == 0 ? null : alive[UnityEngine.Random.Range(0, alive.Count)];
        }

        // ════════════════════ 사이클 ════════════════════
        // 입력 진입점(StartBattle / PlayCard / Teleport / EndTurn)은
        // Phase를 Resolving으로 잠그고, 모든 결과가 끝나면 FinishAction에서 다시 PlayerAction으로 연다.

        public async Awaitable StartBattle()
        {
            if (Phase != BattlePhase.NotStarted) return;
            Phase = BattlePhase.Resolving;
            try
            {
                MaxEnergy = Run.MaxEnergy;
                Player.MaxHp = Run.MaxHp;
                Player.Hp = Run.Hp;
                Player.Pos = Board.Center;   // TODO: 4x4는 첫 패 전에 중앙 4칸 중 선택

                foreach (var card in Run.Deck) Piles.Draw.Add(card.Clone());
                Piles.Shuffle(Piles.Draw);

                foreach (var relic in Run.Relics) relic.Bind(this);

                Log($"전투 시작: 보드 {Board.Size}x{Board.Size}, 적 {string.Join(", ", Enemies)}");
                await Presenter.BattleStarted(this);
                await Events.BattleStart.Raise();
                await StartTurn();
            }
            finally { await FinishAction(); }
        }

        async Awaitable StartTurn()
        {
            if (IsOver) return;
            Turn++;
            TeleportsThisTurn = 0;
            Energy = MaxEnergy;
            Log($"── 턴 {Turn}/{Config.turnLimit} 시작 ──");
            await Presenter.TurnStarted(Turn);
            await Presenter.EnergyChanged(Energy);

            foreach (var enemy in Enemies)
            {
                await enemy.DeclareIntent(this);
                if (IsOver) return;
                await Presenter.IntentDeclared(enemy);
            }
            await DrawCards(Config.handSize);
            await Events.TurnStart.Raise();
        }

        /// <summary>
        /// 카드 사용. 반환되면 이동·타일·부가 효과·연쇄 반응이 모두 끝난 상태.
        /// 흑백 카드(전체 이동 불가)는 가능한 만큼만 이동, 부가 효과 없음, 에너지 소모 없음.
        /// </summary>
        public async Awaitable<bool> PlayCard(CardInstance card, EffectTarget target)
        {
            if (!CanAct || !Piles.Hand.Contains(card)) return false;

            bool full = CanFullyMove(card);
            int cost = full ? card.EffectiveCost : 0;
            if (Energy < cost) { Log($"에너지 부족: {card}"); return false; }

            Phase = BattlePhase.Resolving;
            try
            {
                Energy -= cost;
                Piles.Hand.Remove(card);
                var src = new EffectSource(card);
                Log($"카드 사용: {card}{(full ? "" : " (흑백)")}");
                await Presenter.CardPlayed(card, full);
                if (cost > 0) await Presenter.EnergyChanged(Energy);

                // 1~5. 한 칸씩 이동 + 지나가는 타일 효과
                bool moved = false;
                foreach (var dir in card.Moves.ToList())
                {
                    if (IsOver || !await MovePlayer(src, dir)) break;
                    moved = true;
                }
                if (moved) await TriggerTile(Player.Pos, TileTrigger.Arrive);
                if (IsOver) return true;

                // 6. 모든 이동 성공 시 부가 효과
                if (full) await RunEffects(card.Data.effects, src, target);
                if (IsOver) return true;

                // TODO(카드/덱): 질주 재사용, 잠금 이동 큐 등 키워드 처리
                if (card.Has(Keyword.Delete)) { Piles.Trash.Add(card); await Presenter.CardTrashed(card); }
                else { Piles.Discard.Add(card); await Presenter.CardDiscarded(card); }

                await Events.CardPlayed.Raise(card);
                return true;
            }
            finally { await FinishAction(); }
        }

        /// <summary>HP를 지불하는 순간이동. 에너지는 쓰지 않는다.</summary>
        public async Awaitable<bool> Teleport(Vector2Int pos)
        {
            if (!CanAct) return false;
            if (!Board.IsPassable(pos) || pos == Player.Pos) return false;
            if (Config.teleportLimitPerTurn >= 0 && TeleportsThisTurn >= Config.teleportLimitPerTurn) return false;

            Phase = BattlePhase.Resolving;
            try
            {
                TeleportsThisTurn++;
                int cost = Config.teleportHpCost;
                Player.Hp -= cost;
                Log($"순간이동 → {pos} (HP -{cost})");
                if (Player.Hp <= 0) { Player.Hp = 0; await EndBattle(false); return true; }

                var from = Player.Pos;
                Player.Pos = pos;
                await Presenter.PlayerTeleported(from, pos, cost);
                await Events.PlayerMoved.Raise(from, pos);
                await TriggerTile(pos, TileTrigger.Arrive);
                return true;
            }
            finally { await FinishAction(); }
        }

        /// <summary>턴 종료: 효과 정산 → 사기 0 판정 → 손패 버림 → 턴 종료 이벤트 → 다음 진행.</summary>
        public async Awaitable EndTurn()
        {
            if (!CanAct) return;
            Phase = BattlePhase.Resolving;
            try
            {
                bool lastTurn = Turn >= Config.turnLimit;

                // 1. 턴 종료 효과 정산
                await TriggerTile(Player.Pos, TileTrigger.TurnEndStanding);
                foreach (var enemy in Enemies)
                {
                    if (IsOver) return;
                    await enemy.OnTurnEnd(this);
                }
                if (lastTurn) await RunLastTurnObstacleEffects();
                await TickTileEffects();
                if (IsOver) return;

                // 2. 사기 0 판정
                bool allBroken = Enemies.All(e => e.Hp <= 0);

                // 3. 손패 버림 (보존 제외)
                foreach (var card in Piles.Hand.Where(c => !c.Has(Keyword.Retain)).ToList())
                    await DiscardCard(card);

                // 4. 턴 종료 이벤트 (마지막)
                await Presenter.TurnEnded(Turn);
                await Events.TurnEnd.Raise();
                if (IsOver) return;

                // 5. 다음 진행
                if (allBroken) { Log("적의 사기 0!"); await EndBattle(true); }
                else if (lastTurn)
                {
                    int remain = Enemies.Sum(e => e.Hp);
                    Log($"제한 턴 종료: 남은 사기 {remain}만큼 피해");
                    await DamagePlayer(EffectSource.System, remain);
                    if (!IsOver) await EndBattle(true);
                }
                else await StartTurn();
            }
            finally { await FinishAction(); }
        }

        async Awaitable EndBattle(bool won)
        {
            if (IsOver) return;
            Phase = won ? BattlePhase.Won : BattlePhase.Lost;
            Run.Hp = Player.Hp;
            Log(won ? $"전투 승리 (HP {Player.Hp})" : "패배");
            await Presenter.BattleEnded(won);
            await Events.BattleEnd.Raise(won);
            // TODO(보상): 화폐 + 카드 3중1 + 엘리트면 유물 → 보상 화면으로
        }

        // ════════════════════ 공용 실행 ════════════════════

        /// <summary>
        /// 효과 목록을 순서대로 끝까지 실행. 전투가 끝나면 남은 효과는 건너뛴다.
        /// 카드·타일·적·유물·장애물이 효과 목록을 실행할 때는 모두 이걸 쓴다.
        /// </summary>
        public async Awaitable RunEffects(IEnumerable<Effect> effects, EffectSource src, EffectTarget target)
        {
            foreach (var e in effects.ToList())
            {
                if (IsOver) return;
                if (e != null) await e.Execute(this, src, target);
            }
        }

        // ════════════════════ 사이클 내부 헬퍼 ════════════════════

        /// <summary>행동 하나가 끝남: 전투가 안 끝났으면 다시 조작 가능 상태로 연다.</summary>
        async Awaitable FinishAction()
        {
            if (IsOver) return;
            Phase = BattlePhase.PlayerAction;
            RefreshPlayable();
            await Presenter.InputReady();
        }

        /// <summary>패의 모든 카드 흑백 여부 갱신.</summary>
        void RefreshPlayable()
        {
            foreach (var c in Piles.Hand) c.IsGrayed = !CanFullyMove(c);
        }

        async Awaitable TriggerTile(Vector2Int pos, TileTrigger trigger)
        {
            var tile = Board.Get(pos);
            if (tile == null) return;
            foreach (var te in tile.Effects.Where(t => t.Trigger == trigger).ToList())
            {
                if (IsOver) return;
                await Presenter.TileTriggered(pos, te);
                await RunEffects(te.Effects, new EffectSource(te), EffectTarget.OfTile(pos));
            }
        }

        async Awaitable RunLastTurnObstacleEffects()
        {
            foreach (var o in Obstacles.ToList())
                await RunEffects(o.Data.onLastTurnEnd, new EffectSource(o), EffectTarget.OfTile(o.Pos));
        }

        async Awaitable TickTileEffects()
        {
            foreach (var tile in Board.AllTiles())
            {
                foreach (var te in tile.Effects.Where(t => t.Duration > 0)) te.Duration--;
                foreach (var te in tile.Effects.Where(t => t.Duration == 0).ToList())
                {
                    tile.Effects.Remove(te);
                    await Presenter.TileEffectRemoved(tile.Pos, te);
                }
            }
        }

        public void Log(string msg)
        {
            Logs.Add(msg);
            Debug.Log($"[Battle] {msg}");
        }
    }
}
