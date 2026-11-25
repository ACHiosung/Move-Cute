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
        [Tooltip("순간이동 HP 소모 = 최대 HP의 이 비율(%), 소수점 내림")]
        public int teleportHpPercent = 10;
        [Tooltip("턴당 순간이동 횟수. -1 = 무제한 (미정)")]
        public int teleportLimitPerTurn = -1;
        public BattleRewardPolicy rewards = new();
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
        public readonly CardPiles Piles;
        /// <summary>지금 사용 중인 카드. 질주 재사용까지 모두 끝날 때까지 어느 더미(손패·드로우·묘지·휴지통)에도 없고 여기에만 있다.</summary>
        public CardInstance PlayingCard { get; private set; }

        /// <summary>런 시드 RNG. 무작위는 UnityEngine.Random 대신 이걸 쓴다 (같은 시드 = 같은 런). 보조 함수: RandomExtensions.</summary>
        public System.Random Rng => Run.Rng;

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
            Board = new Board(run.BoardSize, run.Rng);
            Piles = new CardPiles(run.Rng);
            foreach (var e in enemies) Enemies.Add(e.CreateEnemy());
        }

        // ════════════════════ 조회 ════════════════════

        public Tile GetTile(Vector2Int pos) => Board.Get(pos);
        public List<Enemy> GetEnemies() => Enemies;
        public Enemy GetRandomEnemy()
        {
            var alive = Enemies.Where(e => e.Hp > 0).ToList();
            if (alive.Count == 0) alive = Enemies;
            return Rng.Pick(alive);
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
                foreach (var enemy in Enemies) enemy.Bind(this);

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
        /// 여기서는 낼 수 있는지 확인 → 입력 잠금 → 에너지 지불 → 손패에서 빼기 → Play(질주면 재사용 횟수만큼)
        /// → 묘지/휴지통 보내기(마지막에 한 번)를 한다. 카드가 하는 일은 CardInstance.Play가 정한다.
        /// </summary>
        public async Awaitable<bool> PlayCard(CardInstance card, EffectTarget target)
        {
            if (!CanAct || !Piles.Hand.Contains(card)) return false;
            if (card.IsLocked) return false;
            if (Energy < card.PlayCost(this)) { Log($"에너지 부족: {card}"); return false; }

            Phase = BattlePhase.Resolving;
            await SpendEnergy(card.PlayCost(this));
            Piles.Hand.Remove(card);
            PlayingCard = card;
            try
            {
                var playCount = 1;
                if (card.Has<Keyword_Dash>()) playCount += Mathf.Max(0, card.GetKeyword<Keyword_Dash>().maxReuseCount.Get(this));
                int reuses = 0;
                for (int i = 0; i < playCount; i++)
                {
                    await Presenter.CardPlayed(card, CanFullyMove(card));
                    await card.Play(this, target);
                    if (i > 0) { reuses = i; await Events.CardReused.Raise(card, i); }
                    if (!CanFullyMove(card)) break;
                    if (IsOver) return true;
                }
                // 이동·효과·재사용이 모두 끝난 뒤, 더미로 보내기 전 (이온 음료: 재사용 2번 이상이면 효과 한 번 더)
                await Events.CardResolved.Raise(card, target, reuses);
                if (IsOver) return true;
                // 재사용이 모두 끝난 뒤 한 번만 보낸다. 재사용 도중에는 PlayingCard에만 있으므로
                // 휴지통에 여러 번 들어가거나, 드로우로 묘지를 섞을 때 자기 자신을 다시 뽑지 않는다.
                if (card.Has<Keyword_Delete>()) await TrashCard(card);
                else await SendToDiscard(card);
                if (card.AutoLock) await LockCard(card);
                await Events.CardPlayed.Raise(card);
                // Judge after the full card and its reactions, so later effects may restore morale.
                if (!IsOver && Enemies.All(e => e.Hp <= 0)) await EndBattle(true);
                return true;
            }
            finally { PlayingCard = null; await FinishAction(); }
        }

        /// <summary>
        /// 카드의 효과만 한 번 더 낸다 (이온 음료). 에너지 지불·더미 이동·재사용 이벤트 없이 card.Play를 다시 부른다.
        /// 이동은 하지 않는다: 이 동안 이 카드는 해금 카드처럼 다뤄져 MovePlayer가 카드의 이동을 막고 CanFullyMove가 참이 된다.
        /// 그래서 effects뿐 아니라 behavior(특수 카드)에 든 효과도 발동한다. 이동 자체가 효과인 카드(나 잡아봐라 등)는 아무 일도 없다.
        /// </summary>
        public async Awaitable ReplayCardEffects(EffectSource src, CardInstance card, EffectTarget target)
        {
            if (IsOver || card == null) return;
            Log($"{src}: {card} 효과 한 번 더");
            effectsOnlyCard = card;
            try
            {
                await Presenter.CardPlayed(card, true);
                await card.Play(this, target);
            }
            finally { effectsOnlyCard = null; }
        }

        /// <summary>ReplayCardEffects로 효과만 내는 중인 카드.</summary>
        CardInstance effectsOnlyCard;

        /// <summary>이 카드가 출처인 이동을 건너뛰는지: 해금 카드, 또는 효과만 다시 내는 중인 카드.</summary>
        bool SkipsCardMoves(CardInstance card) => card.IsUnlocked || card == effectsOnlyCard;

        /// <summary>에너지 1과 HP를 지불하는 순간이동.</summary>
        public async Awaitable<bool> Teleport(Vector2Int pos)
        {
            if (!CanAct || Energy < 1) return false;
            if (!Board.IsPassable(pos) || pos == Player.Pos) return false;
            if (Config.teleportLimitPerTurn >= 0 && TeleportsThisTurn >= Config.teleportLimitPerTurn) return false;

            Phase = BattlePhase.Resolving;
            try
            {
                TeleportsThisTurn++;
                Energy--;
                await Presenter.EnergyChanged(Energy);
                int cost = LosePlayerHp(EffectSource.System, TeleportHpCost, PlayerHpLossKind.Teleport);
                Log($"순간이동 → {pos} (HP -{cost})");
                if (Player.Hp <= 0) { await EndBattle(false); return true; }

                await TeleportPlayer(pos, cost);
                return true;
            }
            finally { await FinishAction(); }
        }

        /// <summary>
        /// 비용 없는 순간이동 (빙고판: 전투 시작 때 시작 칸 고르기). 에너지·HP·이번 턴 순간이동 횟수를 쓰지 않고,
        /// 입력 진입점이 아니라 효과가 부르는 함수라 입력을 잠그지 않는다.
        /// 도착은 보통 순간이동과 같다: PlayerMoved 이벤트 + 도착 칸의 Arrive 효과. 잠금 진행에는 들어가지 않는다.
        /// 막힌 칸이거나 지금 칸이면 false (아무 일도 없음).
        /// </summary>
        public async Awaitable<bool> TeleportFree(EffectSource src, Vector2Int pos)
        {
            if (IsOver || !Board.IsPassable(pos) || pos == Player.Pos) return false;
            Log($"{src}: 순간이동 → {pos} (비용 없음)");
            await TeleportPlayer(pos, 0);
            return true;
        }

        /// <summary>순간이동의 도착 부분 (비용을 치른 뒤). 연출 → PlayerMoved → 도착 칸 Arrive.</summary>
        async Awaitable TeleportPlayer(Vector2Int pos, int hpCost)
        {
            var from = Player.Pos;
            Player.Pos = pos;
            await Presenter.PlayerTeleported(from, pos, hpCost);
            await Events.PlayerMoved.Raise(from, pos);
            await TriggerTile(pos, TileTrigger.Arrive);
        }

        /// <summary>턴 종료: 효과·반응 정산 → 전투 종료 판단 → 계속되는 전투만 손패 버림·다음 턴.</summary>
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
                if (lastTurn)
                {
                    // 유물이 폭탄을 놓으면 같이 터지도록 장애물 처리 전에 알린다
                    await Events.LastTurnEnd.Raise();
                    await RunLastTurnObstacleEffects();
                }
                await TickTileEffects();
                if (IsOver) return;

                // 2. 사기 0 판정
                bool allBroken = Enemies.All(e => e.Hp <= 0);

                // 3. 손패 버림 (보존 제외)
                foreach (var card in Piles.Hand.Where(c => !c.Has<Keyword_Retain>()).ToList())
                    await DiscardCard(card);

                // 4. 턴 종료 이벤트 (마지막)
                // Finish all end-turn reactions before deciding whether a new turn is needed.
                await Presenter.TurnEnded(Turn);
                await Events.TurnEnd.Raise();
                if (IsOver) return;

                if (Enemies.All(e => e.Hp <= 0))
                {
                    Log("적의 사기 0!");
                    await EndBattle(true);
                    return;
                }
                if (lastTurn)
                {
                    int remain = Enemies.Sum(e => e.Hp);
                    Log($"제한 턴 종료: 남은 사기 {remain}만큼 피해");
                    await DamagePlayer(EffectSource.System, remain);
                    if (!IsOver) await EndBattle(true);
                    return;
                }

                // Only continuing battles discard their hand and begin another turn.
                foreach (var card in Piles.Hand.Where(c => !c.Has<Keyword_Retain>()).ToList())
                    await DiscardCard(card);
                await StartTurn();
            }
            finally { await FinishAction(); }
        }

        async Awaitable EndBattle(bool won)
        {
            if (IsOver) return;
            Phase = won ? BattlePhase.Won : BattlePhase.Lost;
            Run.Hp = Player.Hp;
            if (won) Run.BattlesWon++;
            Log(won ? $"전투 승리 (HP {Player.Hp})" : "패배");
            if (won) await PrepareConfiguredRewards();
            await Presenter.BattleEnded(won);
            await Events.BattleEnd.Raise(won);
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
            foreach (var c in Piles.Hand) c.IsGrayed = c.IsLocked || !c.CanPlayFully(this);
        }

        async Awaitable TriggerTile(Vector2Int pos, TileTrigger trigger)
        {
            var tile = Board.Get(pos);
            if (tile == null) return;
            HashSet<Enemy> acted = null;
            foreach (var te in tile.Effects.Where(t => t.Trigger == trigger).ToList())
            {
                if (IsOver) return;
                // 턴 종료 때 적의 예고가 내가 서 있는 칸에서 발동하기 직전 (적마다 한 번): 카운터 등이 먼저 반응한 뒤 적 행동이 진행된다
                if (trigger == TileTrigger.TurnEndStanding && pos == Player.Pos && te.Source is Enemy enemy && (acted ??= new()).Add(enemy))
                {
                    await Events.EnemyActsOnPlayerTile.Raise(enemy);
                    if (IsOver) return;
                }
                await Presenter.TileTriggered(pos, te);
                await te.Fire(this, pos);
            }
        }

        /// <summary>처리 순서: lastTurnPriority 높은 것부터 (연계 폭탄 먼저), 같으면 보드 읽는 순서 (위쪽 줄 → 왼쪽부터).</summary>
        async Awaitable RunLastTurnObstacleEffects()
        {
            var order = Obstacles
                .OrderByDescending(o => o.Data.lastTurnPriority)
                .ThenBy(o => Board.ReadingOrderKey(o.Pos))
                .ToList();
            foreach (var o in order)
                await o.OnLastTurnEnd(this);
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

        // ════════════════════ 순간이동 비용 · 깎은 사기 ════════════════════

        /// <summary>
        /// 순간이동 HP 소모: 최대 HP × teleportHpPercent%, 소수점 내림 (최대 HP 11 → 1, 51 → 5).
        /// 제트팩(FreeTeleportRelic)이 켜져 있으면 0. UI 표시도 이 값을 쓴다.
        /// 유물의 감소·버티기(두꺼운 패딩·기계 심장)는 이 값이 아니라 실제로 HP를 잃을 때(LosePlayerHp) 적용된다.
        /// </summary>
        public int TeleportHpCost =>
            Run.Relics.Any(r => r.Enabled && r is FreeTeleportRelic) ? 0 : Player.MaxHp * Config.teleportHpPercent / 100;

        /// <summary>
        /// 이 전투에서 적 사기를 실제로 깎은 양의 합 (피해·사기 감소 효과 모두). 적이 회복해도 줄지 않는다.
        /// 남은 사기보다 큰 피해는 남은 만큼만 센다. ChangeMorale·DealDamage(Units.cs)에서 더한다.
        /// </summary>
        public int MoraleDealt { get; private set; }
    }
}
