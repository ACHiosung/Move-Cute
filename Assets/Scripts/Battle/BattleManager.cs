using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 씬 진입점. 런/전투 문맥을 만들고 사이클을 시작한다.
    /// UI가 생기기 전까지 OnGUI 디버그 패널로 사이클을 돌려볼 수 있다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        public BattleConfig config = new();
        [Tooltip("런 시작 덱. 같은 카드를 여러 장 넣으려면 여러 번 넣는다")]
        public List<CardData> startingDeck = new();
        [Tooltip("전투할 적 에셋 (Assets/Data/Enemies)")]
        public EnemyData enemy;
        public List<RelicData> relics = new();
        [Tooltip("런 시드. 0 = 매 런 무작위. 같은 값이면 셔플·적 행동·무작위 대상이 재현된다 (시드는 런 시작 때 콘솔에 찍힌다)")]
        public int seed;
        [Tooltip("IBattlePresenter를 구현한 컴포넌트. 비어 있으면 연출 없음")]
        public MonoBehaviour presenter;
        [Tooltip("IPlayerInput을 구현한 컴포넌트. 비어 있으면 디버그 GUI로 선택")]
        public MonoBehaviour input;
        public bool showDebugGui = true;

        public RunState Run { get; private set; }
        public BattleContext Ctx { get; private set; }

        /// <summary>디버그 GUI용 선택 입력. 정식 UI가 생기면 IPlayerInput 구현으로 교체.</summary>
        public readonly DebugPlayerInput DebugInput = new();

        /// <summary>카드 대상 선택 중. 아직 사이클 진입 전이라 Phase로는 잠기지 않으므로 따로 막는다.</summary>
        bool choosingTarget;
        public bool CanInput => Ctx != null && Ctx.CanAct && !choosingTarget;

        void Awake() => Instance = this;

        void Start()
        {
            NewRun();
            StartBattle().Forget();
        }

        public void NewRun()
        {
            Run = new RunState(seed == 0 ? null : seed);
            Debug.Log($"[Run] 시드 {Run.Seed}");
            if (startingDeck.Count == 0) Debug.LogError("BattleManager.startingDeck가 비어 있습니다. 카드 에셋을 넣어 주세요.", this);
            foreach (var c in startingDeck.Where(c => c != null)) Run.Deck.Add(new CardInstance(c));
            foreach (var r in relics.Where(r => r != null)) Run.Relics.Add(r.CreateRelic());
        }

        public Awaitable StartBattle()
        {
            DebugInput.CancelPending();
            choosingTarget = false;

            var p = presenter as IBattlePresenter;
            if (presenter != null && p == null)
                Debug.LogError($"{presenter.name}: IBattlePresenter를 구현하지 않았습니다. 연출 없이 진행합니다.", presenter);
            var i = input as IPlayerInput;
            if (input != null && i == null)
                Debug.LogError($"{input.name}: IPlayerInput을 구현하지 않았습니다. 디버그 GUI로 선택합니다.", input);

            if (enemy == null)
            {
                Debug.LogError("BattleManager.enemy가 비어 있습니다. 적 에셋을 넣어 주세요.", this);
                return AwaitableUtil.Completed();
            }
            Ctx = new BattleContext(Run, config, new[] { enemy }, p, i ?? DebugInput);
            return Ctx.StartBattle();
        }

        /// <summary>UI에서 카드를 낼 때 호출. 대상을 고른 뒤 카드가 완전히 해결되면 끝난다. 취소하면 아무 일도 없다.</summary>
        public async Awaitable PlayCard(CardInstance card)
        {
            if (!CanInput) return;
            var ctx = Ctx;

            EffectTarget? target;
            choosingTarget = true;
            try { target = await ChooseTarget(ctx, card); }
            finally { choosingTarget = false; }

            if (target.HasValue && ctx == Ctx) await ctx.PlayCard(card, target.Value);
        }

        /// <summary>카드 대상 선택. 취소하면 null.</summary>
        static async Awaitable<EffectTarget?> ChooseTarget(BattleContext ctx, CardInstance card)
        {
            switch (card.Data.target)
            {
                case TargetType.Unit:
                    var alive = ctx.Enemies.Where(e => e.Hp > 0).Cast<Unit>().ToList();
                    if (alive.Count == 0) alive = ctx.Enemies.Cast<Unit>().ToList();
                    var unit = await ctx.ChooseUnit(alive, $"{card}: 대상 선택");
                    return unit == null ? null : (EffectTarget?)EffectTarget.OfUnit(unit);

                case TargetType.SelfTile:
                    return EffectTarget.OfTile(ctx.Player.Pos);

                case TargetType.OtherTile:
                    var tiles = ctx.Board.AllTiles().Select(t => t.Pos).Where(p => p != ctx.Player.Pos).ToList();
                    var pos = await ctx.ChooseTile(tiles, $"{card}: 타일 선택");
                    return pos.HasValue ? EffectTarget.OfTile(pos.Value) : null;

                case TargetType.Card:
                    var cards = ctx.Piles.Hand.Where(c => c != card).ToList();
                    var picked = await ctx.ChooseCard(cards, $"{card}: 카드 선택");
                    return picked == null ? null : (EffectTarget?)EffectTarget.OfCard(picked);

                default:
                    return EffectTarget.None;
            }
        }

        // ════════════ 디버그 GUI ════════════

        Vector2 logScroll;
        /// <summary>OnGUI 안에서 바로 실행하면 레이아웃 도중 상태가 바뀌므로 Update에서 실행한다.</summary>
        Action deferred;

        void Update()
        {
            var a = deferred;
            deferred = null;
            a?.Invoke();
        }

        void OnGUI()
        {
            if (!showDebugGui || Ctx == null) return;
            var ctx = Ctx;

            GUILayout.BeginArea(new Rect(10, 10, 560, Screen.height - 20), GUI.skin.box);

            GUILayout.Label($"턴 {ctx.Turn}/{ctx.Config.turnLimit}   에너지 {ctx.Energy}/{ctx.MaxEnergy}   " +
                            $"HP {ctx.Player.Hp}/{ctx.Player.MaxHp}   [{ctx.Phase}]");
            foreach (var en in ctx.Enemies)
                GUILayout.Label($"{en}: 사기 {en.Hp}  {string.Join(" ", en.Statuses)}");

            // 선택 요청 (대상 지정·효과 중 선택)
            var req = DebugInput.Pending;
            if (req != null)
            {
                GUILayout.Label($"▶ {req.Prompt}");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < req.Labels.Length; i++)
                {
                    int index = i;
                    if (GUILayout.Button(req.Labels[i], GUILayout.Height(30))) deferred = () => DebugInput.Choose(index);
                }
                if (GUILayout.Button("취소", GUILayout.Height(30))) deferred = () => DebugInput.Choose(-1);
                GUILayout.EndHorizontal();
            }

            bool prevEnabled = GUI.enabled;
            GUI.enabled = CanInput;

            // 보드: 위쪽 행부터 그린다. 칸 클릭 = 순간이동
            GUILayout.Label($"보드 (칸 클릭 = 순간이동, HP -{ctx.Config.teleportHpCost})   P=플레이어 #=장애물  타일효과 노랑:지나감 파랑:도착 빨강:턴종료");
            for (int y = ctx.Board.Size - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < ctx.Board.Size; x++)
                {
                    var pos = new Vector2Int(x, y);
                    var tile = ctx.Board.Get(pos);
                    var prev = GUI.backgroundColor;
                    if (tile.Effects.Count > 0) GUI.backgroundColor = TriggerColor(tile.Effects[0].Trigger);
                    if (GUILayout.Button(CellLabel(ctx, tile), GUILayout.Width(80), GUILayout.Height(50)))
                        deferred = () => ctx.Teleport(pos).Forget();
                    GUI.backgroundColor = prev;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label($"손패 (드로우 {ctx.Piles.Draw.Count} / 묘지 {ctx.Piles.Discard.Count} / 휴지통 {ctx.Piles.Trash.Count})");
            GUILayout.BeginHorizontal();
            foreach (var card in ctx.Piles.Hand.ToList())
            {
                var prev = GUI.color;
                if (card.IsGrayed) GUI.color = Color.gray;
                if (GUILayout.Button($"{card}\n코스트 {card.EffectiveCost}", GUILayout.Height(45)))
                    deferred = () => PlayCard(card).Forget();
                GUI.color = prev;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("턴 종료", GUILayout.Height(30))) deferred = () => ctx.EndTurn().Forget();
            GUI.enabled = prevEnabled;
            if (ctx.Phase == BattlePhase.Won && GUILayout.Button("다음 전투", GUILayout.Height(30)))
                deferred = () => StartBattle().Forget();
            if (GUILayout.Button("새 런", GUILayout.Height(30))) deferred = () => { NewRun(); StartBattle().Forget(); };
            GUILayout.EndHorizontal();

            logScroll = GUILayout.BeginScrollView(logScroll);
            for (int i = ctx.Logs.Count - 1; i >= 0; i--) GUILayout.Label(ctx.Logs[i]);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        static string CellLabel(BattleContext ctx, Tile tile)
        {
            var parts = new List<string>();
            if (ctx.Player.Pos == tile.Pos) parts.Add("P");
            if (tile.Occupant != null) parts.Add($"#{tile.Occupant.Hp}");
            if (tile.Effects.Count > 0) parts.Add($"효과{tile.Effects.Count}");
            return string.Join("\n", parts);
        }

        static Color TriggerColor(TileTrigger t) => t switch
        {
            TileTrigger.Pass => Color.yellow,
            TileTrigger.Arrive => Color.cyan,
            _ => Color.red,
        };
    }
}
