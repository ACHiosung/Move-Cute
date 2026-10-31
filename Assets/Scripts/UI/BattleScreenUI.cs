using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>
    /// 전투 화면. 전투 상태 변경은 BattleContext의 공개 함수에 위임한다.
    /// 공용 임시 UI와 독립적으로 사용하며, 화면 구성은 Layout partial에 있다.
    /// </summary>
    public partial class BattleScreenUI : MonoBehaviour, IBattlePresenter, IPlayerInput
    {
        [Header("프리팹")]
        public Button tilePrefab;
        public Button cardPrefab;
        public Button enemyPrefab;

        [Header("배치")]
        public GridLayoutGroup board;
        public RectTransform hand;
        public RectTransform enemies;
        public RectTransform playerMarker;

        [Header("텍스트·버튼")]
        public Text hud;
        public Text log;
        public Text banner;
        public GameObject promptRoot;
        public Text prompt;
        public Button cancelButton;
        public Button endTurnButton;
        public GameObject resultPanel;
        public Text resultText;
        public Button nextBattleButton;
        public Button newRunButton;

        [Header("연출")]
        [Tooltip("연출 속도 배율. 클수록 빠름")]
        public float speed = 1f;

        static readonly Color TileBase = new(0.25f, 0.27f, 0.32f);
        static readonly Color CardBase = new(0.95f, 0.92f, 0.85f);
        static readonly Color EnemyBase = new(0.45f, 0.2f, 0.25f);
        static readonly Color Pickable = new(0.3f, 0.8f, 0.4f);
        static readonly Color Grayed = new(0.55f, 0.55f, 0.55f);
        static readonly Color ObstacleColor = new(0.5f, 0.38f, 0.25f);

        BattleManager M => BattleManager.Instance;
        BattleContext ctx;

        readonly Dictionary<Vector2Int, Button> tiles = new();
        readonly Dictionary<CardInstance, Button> cards = new();
        readonly Dictionary<Enemy, Button> enemyViews = new();

        // 선택 대기 중인 요청. 후보를 클릭하면 Resolve로 완료된다.
        AwaitableCompletionSource<object> pick;
        HashSet<object> pickable;

        void Awake()
        {
            InitializeScreen();
            InitializeRewards();
            endTurnButton.onClick.AddListener(() =>
            {
                if (!M.CanInput) return;
                ctx.EndTurn().Forget();
            });
            cancelButton.onClick.AddListener(() => Resolve(null));
            nextBattleButton.onClick.AddListener(RequestNextBattle);
            newRunButton.onClick.AddListener(() => { M.NewRun(); M.StartBattle().Forget(); });
            promptRoot.SetActive(false);
            resultPanel.SetActive(false);
            banner.gameObject.SetActive(false);
            Loc.LanguageChanged += RefreshAll;
        }

        void OnDestroy() => Loc.LanguageChanged -= RefreshAll;

        float D(float seconds) => seconds / Mathf.Max(0.01f, speed);

        // ════════════ 클릭 ════════════

        void OnTileClick(Vector2Int pos)
        {
            if (ctx.Board.Get(pos).Occupant is Obstacle obstacle && TryPick(obstacle)) return;
            if (TryPick(pos)) return;
            if (teleportMode && M.CanInput && ctx.Board.IsPassable(pos) && pos != ctx.Player.Pos)
            {
                teleportMode = false;
                ctx.Teleport(pos).Forget();
                return;
            }
            Back();
        }

        void OnCardClick(CardInstance card)
        {
            if (TryPick(card)) return;
            if (pick != null) Back();
            if (modal.gameObject.activeSelf || !M.CanInput) return;
            teleportMode = false;
            selectedCard = card;
            ShowCard(card);
            Preview(card);
            M.PlayCard(card).Forget();
        }

        // ════════════ IPlayerInput ════════════

        public async Awaitable<Unit> PickUnit(IReadOnlyList<Unit> candidates, string message) =>
            (Unit)await Pick(candidates, message);

        public async Awaitable<Vector2Int?> PickTile(IReadOnlyList<Vector2Int> candidates, string message) =>
            await Pick(candidates.Cast<object>(), message) is Vector2Int v ? v : (Vector2Int?)null;

        public async Awaitable<CardInstance> PickCard(IReadOnlyList<CardInstance> candidates, string message) =>
            (CardInstance)await Pick(candidates, message);

        /// <summary>후보를 테두리로 표시하고 클릭 또는 취소를 기다린다.</summary>
        async Awaitable<object> Pick(IEnumerable<object> candidates, string message)
        {
            var list = candidates.ToList();
            if (list.Count == 0) return null;

            Resolve(null);
            var source = new AwaitableCompletionSource<object>();
            pick = source;
            pickable = new HashSet<object>(list);
            prompt.text = message;
            promptRoot.SetActive(true);
            RefreshTargets();
            return await source.Awaitable;
        }

        bool TryPick(object target)
        {
            if (pick == null || !pickable.Contains(target)) return false;
            Resolve(target);
            return true;
        }

        void Resolve(object result)
        {
            var source = pick;
            if (source == null) return;
            pick = null;   // 결과 처리 중에 새 요청이 올 수 있으므로 먼저 비운다
            pickable = null;
            promptRoot.SetActive(false);
            RefreshTargets();
            source.SetResult(result);
            if (result == null) ClearSelection();
        }

        // ════════════ IBattlePresenter: 사이클 ════════════

        public Awaitable BattleStarted(BattleContext c)
        {
            ctx = c;
            ResetRewardScreen();
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            selectedCard = null;
            teleportMode = false;
            modal.gameObject.SetActive(false);
            ClearPreview();
            Resolve(null);
            resultPanel.SetActive(false);
            Build();
            return AwaitableUtil.Completed();
        }

        public Awaitable TurnStarted(int turn)
        {
            RefreshHud();
            return Banner($"{T("turn")} {turn}");
        }

        public Awaitable IntentDeclared(Enemy enemy)
        {
            unitViews[enemyViews[enemy]].InvalidateIntent();
            RefreshEnemy(enemyViews[enemy], enemy);
            return Punch(ViewOf(enemy), 0.15f);
        }

        public async Awaitable BattleEnded(bool won)
        {
            RefreshAll();
            ClearSelection();
            if (won)
            {
                ShowRewardChest();
                return;
            }
            resultText.text = won ? $"{T("won")}  HP {ctx.Player.Hp}" : T("lost");
            nextBattleButton.gameObject.SetActive(won);
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.one * 0.8f;
            await resultPanel.transform.DOScale(1f, D(0.2f)).SetEase(Ease.OutBack).SetLink(resultPanel).ToAwaitable();
        }

        public Awaitable InputReady()
        {
            RefreshHud();
            foreach (var card in cards.Keys) RefreshCard(card);
            foreach (var entry in enemyViews) RefreshEnemy(entry.Value, entry.Key);
            ClearSelection();
            if (tiles.TryGetValue(ctx.Player.Pos, out var t)) playerMarker.position = t.transform.position;
            return AwaitableUtil.Completed();
        }

        // ════════════ IBattlePresenter: 유닛 ════════════

        public Awaitable Damaged(Unit target, int amount)
        {
            RefreshUnit(target);
            var view = target == ctx.Player ? playerMarker : ViewOf(target);
            Popup(view, $"-{amount}", Color.red);
            return Punch(view, 0.25f);
        }

        public Awaitable MoraleChanged(Enemy enemy, int amount)
        {
            RefreshEnemy(enemyViews[enemy], enemy);
            Popup(ViewOf(enemy), $"{T("morale")} {amount:+0;-0;0}", new Color(1f, 0.6f, 0.2f));
            return Punch(ViewOf(enemy), 0.2f);
        }

        public Awaitable StatusAdded(Unit target, string statusId, int stacks)
        {
            RefreshUnit(target);
            Popup(target == ctx.Player ? playerMarker : ViewOf(target), $"{statusId} +{stacks}", Color.magenta);
            return AwaitableUtil.Completed();
        }

        // ════════════ IBattlePresenter: 보드 ════════════

        public Awaitable PlayerStepped(Vector2Int from, Vector2Int to) =>
            playerMarker.DOMove(tiles[to].transform.position, D(0.18f)).SetEase(Ease.OutQuad).SetLink(playerMarker.gameObject).ToAwaitable();

        public Awaitable PlayerTeleported(Vector2Int from, Vector2Int to, int hpCost)
        {
            RefreshHud();
            Popup(playerMarker, $"HP -{hpCost}", Color.red);
            return DOTween.Sequence()
                .Append(playerMarker.DOScale(0f, D(0.12f)))
                .AppendCallback(() => playerMarker.position = tiles[to].transform.position)
                .Append(playerMarker.DOScale(1f, D(0.12f)))
                .SetLink(playerMarker.gameObject)
                .ToAwaitable();
        }

        public Awaitable ObstaclePlaced(Obstacle obstacle)
        {
            RefreshTile(obstacle.Pos);
            return Punch(tiles[obstacle.Pos].transform, 0.25f);
        }

        public Awaitable ObstacleDestroyed(Obstacle obstacle)
        {
            RefreshTile(obstacle.Pos);
            Popup(tiles[obstacle.Pos].transform, T("destroyed"), Color.white);
            return Punch(tiles[obstacle.Pos].transform, 0.25f);
        }

        public Awaitable TileEffectAdded(Vector2Int pos, TileEffect effect)
        {
            RefreshTile(pos);
            return Punch(tiles[pos].transform, 0.12f);
        }

        public Awaitable TileEffectRemoved(Vector2Int pos, TileEffect effect)
        {
            RefreshTile(pos);
            return AwaitableUtil.Completed();
        }

        public Awaitable TileTriggered(Vector2Int pos, TileEffect effect) => Punch(tiles[pos].transform, 0.2f);

        // ════════════ IBattlePresenter: 카드 ════════════

        public Awaitable CardDrawn(CardInstance card)
        {
            var view = AcquireHandCard();
            cards[card] = view;
            BindCard(view, card);
            RefreshCard(card);
            RefreshHud();
            RefreshHandLayout();
            view.transform.localScale = Vector3.zero;
            return view.transform.DOScale(1f, D(0.08f)).SetLink(view.gameObject).ToAwaitable();
        }

        /// <summary>손패에서 빼서 위로 띄우며 사라진다. 카드 효과는 이 연출이 끝난 뒤 시작된다.</summary>
        public Awaitable CardPlayed(CardInstance card, bool fullyMoved)
        {
            RefreshHud();
            if (!cards.Remove(card, out var view)) return AwaitableUtil.Completed();
            handView.Remove((RectTransform)view.transform);
            EndInfoHover(view);
            ClearSelection();

            view.transform.SetParent(transform, true);
            view.transform.SetAsLastSibling();
            var group = view.GetComponent<CanvasGroup>();
            if (!group) group = view.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            return DOTween.Sequence()
                .Append(view.transform.DOMoveY(view.transform.position.y + 120f, D(0.2f)))
                .Join(group.DOFade(0f, D(0.2f)))
                .OnComplete(() => ReleaseHandCard(view))
                .SetLink(view.gameObject)
                .ToAwaitable();
        }

        public Awaitable CardDiscarded(CardInstance card) => RemoveCard(card);
        public Awaitable CardTrashed(CardInstance card) => RemoveCard(card);

        public Awaitable CardChanged(CardInstance card)
        {
            RefreshCard(card);
            if (inspectedCard == card)
            {
                bool keywordsOpen = keywordPanel.gameObject.activeSelf;
                ShowCard(card);
                if (keywordsOpen) ShowKeywords();
            }
            return AwaitableUtil.Completed();
        }

        public Awaitable EnergyChanged(int energy)
        {
            RefreshHud();
            bool empty = energy <= 0;
            if (empty != lastEmptyEnergy)
            {
                lastEmptyEnergy = empty;
                foreach (var card in cards.Keys) RefreshCard(card);
            }
            return AwaitableUtil.Completed();
        }

        Awaitable RemoveCard(CardInstance card)
        {
            if (!cards.Remove(card, out var view)) return AwaitableUtil.Completed();
            handView.Remove((RectTransform)view.transform);
            EndInfoHover(view);
            RefreshHud();
            RefreshHandLayout();
            return view.transform.DOScale(0f, D(0.06f))
                .OnComplete(() => ReleaseHandCard(view))
                .SetLink(view.gameObject)
                .ToAwaitable();
        }

        // ════════════ 그리기 ════════════

        /// <summary>전투 시작 시 보드·적 뷰를 새로 만든다.</summary>
        void Build()
        {
            Clear(board.transform);
            foreach (var view in cards.Values) ReleaseHandCard(view);
            handView.Clear();
            Clear(hand);
            Clear(enemies);
            tiles.Clear();
            cards.Clear();
            enemyViews.Clear();
            boardViews.Clear();
            unitViews.Clear();

            int n = ctx.Board.Size;
            board.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            board.constraintCount = n;
            board.cellSize = Vector2.one * ((660f - (n - 1) * board.spacing.x) / n);
            for (int y = n - 1; y >= 0; y--)   // 위쪽 행부터
                for (int x = 0; x < n; x++)
                {
                    var pos = new Vector2Int(x, y);
                    var view = Instantiate(tilePrefab, board.transform);
                    view.gameObject.SetActive(true);
                    view.onClick.AddListener(() => OnTileClick(pos));
                    tiles[pos] = view;
                    boardViews.Add(view, view.GetComponent<BattleTileView>());
                    BindTile(view, pos);
                }

            foreach (var e in ctx.Enemies)
            {
                var view = Instantiate(enemyPrefab, enemies);
                view.gameObject.SetActive(true);
                view.onClick.AddListener(() => { if (!TryPick(e)) Back(); });
                enemyViews[e] = view;
                unitViews.Add(view, view.GetComponent<BattleEnemyView>());
                BindEnemy(view, e);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)board.transform);
            playerMarker.localScale = Vector3.one;
            playerMarker.position = tiles[ctx.Player.Pos].transform.position;
            lastEmptyEnergy = ctx.Energy <= 0;
            RefreshAll();
        }

        static void Clear(Transform parent)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);   // Destroy는 프레임 끝에 일어나므로 레이아웃에서 먼저 뺀다
                Destroy(child.gameObject);
            }
        }

        void RefreshAll()
        {
            if (ctx == null) return;
            RefreshHud();
            foreach (var pos in tiles.Keys) RefreshTile(pos);
            foreach (var card in cards.Keys) RefreshCard(card);
            foreach (var (enemy, view) in enemyViews)
            {
                RefreshEnemy(view, enemy);
            }
            endTurnButton.interactable = M.CanInput && !teleportMode && !modal.gameObject.activeSelf;
        }

        void RefreshHud()
        {
            RefreshScreenHud();
        }

        void RefreshTile(Vector2Int pos)
        {
            var view = tiles[pos];
            var tile = ctx.Board.Get(pos);
            var color = TileBase;
            if (tile.Effects.Count > 0) color = TriggerColor(tile.Effects[0].Trigger);
            if (tile.Occupant != null) color = ObstacleColor;
            SetFrame(view, (pickable != null && (pickable.Contains(pos) ||
                tile.Occupant != null && pickable.Contains(tile.Occupant))) ||
                teleportMode && ctx.Board.IsPassable(pos) && pos != ctx.Player.Pos);

            var lines = new List<string>();
            if (tile.Occupant != null) lines.Add($"{tile.Occupant} {tile.Occupant.Hp}");
            foreach (var te in tile.Effects)
                lines.Add($"{TriggerName(te.Trigger)}\n{string.Join(" / ", te.Effects.Select(DescribeEffect))}");
            boardViews[view].Render(color, string.Join("\n", lines));
        }

        void RefreshCard(CardInstance card)
        {
            var view = cards[card];
            RefreshCardView(view, card);
        }

        // ════════════ 연출 헬퍼 ════════════

        Awaitable Punch(Transform target, float seconds)
        {
            if (target == null) return AwaitableUtil.Completed();
            target.DOComplete();   // 이전 펀치가 남아 있으면 크기가 틀어지므로 먼저 끝낸다
            return target.DOPunchScale(Vector3.one * 0.15f, D(seconds), 6).SetLink(target.gameObject).ToAwaitable();
        }

        Awaitable Banner(string message)
        {
            banner.text = message;
            banner.gameObject.SetActive(true);
            var c = banner.color;
            banner.color = new Color(c.r, c.g, c.b, 0f);
            return DOTween.Sequence()
                .Append(banner.DOFade(1f, D(0.15f)))
                .AppendInterval(D(0.35f))
                .Append(banner.DOFade(0f, D(0.15f)))
                .OnComplete(() => banner.gameObject.SetActive(false))
                .SetLink(banner.gameObject)
                .ToAwaitable();
        }

        /// <summary>떠오르는 숫자. 기다리지 않는다 — 숫자가 떠 있는 동안 다음 로직이 진행된다.</summary>
        void Popup(Transform at, string message, Color color)
        {
            if (at == null) return;
            var go = new GameObject("Popup", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            go.transform.position = at.position;
            ((RectTransform)go.transform).sizeDelta = new Vector2(300, 60);
            var text = go.GetComponent<Text>();
            text.font = hud.font;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = message;
            text.raycastTarget = false;

            DOTween.Sequence()
                .Append(go.transform.DOMoveY(at.position.y + 90f, D(0.7f)))
                .Join(text.DOFade(0f, D(0.7f)).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(go))
                .SetLink(go);
        }

        Transform ViewOf(Unit unit) => unit switch
        {
            Enemy e when enemyViews.TryGetValue(e, out var v) => v.transform,
            Obstacle o when tiles.TryGetValue(o.Pos, out var t) => t.transform,
            _ => null,
        };

        static void Paint(Button view, Color color) => view.image.color = color;
        static Text Label(Button view) => view.GetComponentInChildren<Text>();

        static string IntentSummary(Enemy enemy)
        {
            var intent = enemy.CurrentIntent;
            if (intent == null) return "";
            return $"예고: 타일 {intent.tileEffects.Count} / 행동 {intent.turnEndActions.Count}";
        }

        static string EffectName(Effect effect) => effect?.GetType().Name.Replace("Effect", "") ?? "-";

        static string TriggerName(TileTrigger t) => t switch
        {
            TileTrigger.Pass => T("pass"),
            TileTrigger.Arrive => T("arrive"),
            _ => T("turnEnd"),
        };

        static Color TriggerColor(TileTrigger t) => t switch
        {
            TileTrigger.Pass => new Color(0.8f, 0.7f, 0.2f),
            TileTrigger.Arrive => new Color(0.2f, 0.6f, 0.8f),
            _ => new Color(0.75f, 0.25f, 0.25f),
        };
    }
}

