using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        [Header("UI 표시 컴포넌트")]
        public BattleHandView handView;
        public BattlePreviewView previewView;
        public BattleCardCollectionView browserView, rewardChoicesView;
        public BattleBackdropView backdropView;
        public BattleCellStripView energyStrip;
        public BattleRewardListView rewardListView;
        readonly Dictionary<Button, BattleCardView> cardViews = new();
        readonly Dictionary<Button, BattleEnemyView> unitViews = new();
        readonly Dictionary<Button, BattleTileView> boardViews = new();
        readonly Stack<Button> availableHandCards = new();
        RectTransform cardPoolRoot;
        RectTransform relicList, panelSupports;

        /// <summary>유물 아이콘 하나. 누적 숫자와 흑백(다 쓴 유물)은 RefreshRelicStates가 매 프레임 맞춘다.</summary>
        sealed class RelicView
        {
            public Relic Relic;
            public Image Tile, Icon;
            public Text Name, Counter;
            public bool ShownSpent;
            public CanvasGroup Fade;
            public bool ShownEnabled;   // 켜기·끄기는 아이콘 클릭 말고 에디터 창(유물 켜기·끄기, Testing Tools)에서도 바뀐다
        }
        readonly List<RelicView> relicViews = new();
        static readonly Color RelicTile = new(0.24f, 0.24f, 0.24f);
        static readonly Color SpentRelicTile = new(0.16f, 0.16f, 0.16f);
        static readonly Color SpentRelicName = new(0.55f, 0.55f, 0.55f);
        Transform detailPanelRoot, playerPanelRoot;
        GameObject mouseIcon;
        Button deckButton, drawButton, discardButton, trashButton, closeBrowserButton;
        Text trashText;
        GameObject backgroundObject;
        bool lastEmptyEnergy;
        Transform[] backdropRoots;
        GameObject[] blockedBattleRoots;

        void InitializeViews()
        {
            enemies.anchoredPosition += Vector2.left * 32f;
            // One-time lookups for the root-level layout; card/target parts are serialized in each view.
            detailPanelRoot = detailName.transform.parent;
            playerPanelRoot = goldBalance.transform.parent;
            panelSupports = transform.Find("PanelSupports") as RectTransform;
            mouseIcon = detailPanelRoot.Find("MouseIcon").gameObject;
            deckButton = playerPanelRoot.Find("Deck").GetComponent<Button>();
            drawButton = drawText.transform.parent.GetComponent<Button>();
            discardButton = discardText.transform.parent.GetComponent<Button>();
            var discardRect = (RectTransform)discardButton.transform;
            trashButton = Instantiate(discardButton, discardRect.parent);
            trashButton.name = "TrashPile";
            var trashRect = (RectTransform)trashButton.transform;
            float trashSize = discardRect.rect.width * 0.5f;
            const float trashGap = 12f;
            trashRect.sizeDelta = new Vector2(trashSize, trashSize);
            trashRect.anchoredPosition = discardRect.anchoredPosition
                + new Vector2(discardRect.rect.width - trashSize, trashSize + trashGap);
            trashRect.SetSiblingIndex(discardRect.GetSiblingIndex());
            trashText = trashButton.GetComponentInChildren<Text>(true);
            trashText.fontSize = trashText.resizeTextMaxSize = 18;
            closeBrowserButton = modal.Find("Close").GetComponent<Button>();
            ConfigureBrowserCloseButton(closeBrowserButton);
            backgroundObject = transform.Find("Background").gameObject;
            cardPoolRoot = (RectTransform)transform.Find("Templates");
            backdropRoots = new[] { backgroundObject.transform, board.transform.parent,
                hand, playerMarker, energyButton.transform, endTurnButton.transform,
                drawButton.transform, discardButton.transform, trashButton.transform };
            blockedBattleRoots = new[] { hand.gameObject, playerMarker.gameObject, energyButton.gameObject,
                endTurnButton.gameObject, drawButton.gameObject, discardButton.gameObject, trashButton.gameObject };
        }

        void ShowRelics()
        {
            const int rows = 10, capacity = rows * 2;
            const float gap = 4f;
            int count = Mathf.Min(ctx.Run.Relics.Count, capacity);
            var playerPanel = (RectTransform)playerPanelRoot;
            var detailPanel = (RectTransform)detailPanelRoot;
            var boardFrame = (RectTransform)board.transform.parent;
            float height = playerPanel.anchoredPosition.y - detailPanel.anchoredPosition.y + detailPanel.rect.height;
            float size = (height - (rows - 1) * gap) / rows;
            if (count > rows) size *= 0.5f;
            float width = count > rows ? 2 * size + gap : size;
            float available = boardFrame.anchoredPosition.x - playerPanel.rect.width;
            float outerGap = count == 0 ? available / 2 : (available - width) / 3;
            float panelX = count == 0 ? outerGap : 2 * outerGap + width;
            var shift = Vector2.right * (panelX - playerPanel.anchoredPosition.x);
            playerPanel.anchoredPosition += shift;
            detailPanel.anchoredPosition += shift;
            if (panelSupports != null) panelSupports.anchoredPosition += shift;
            modal.anchoredPosition += shift;
            keywordPanel.anchoredPosition += shift;

            relicViews.Clear();
            if (count == 0)
            {
                if (relicList != null) Clear(relicList);
                return;
            }

            if (relicList == null)
            {
                relicList = new GameObject("Relics", typeof(RectTransform)).GetComponent<RectTransform>();
                relicList.SetParent(transform, false);
                relicList.anchorMin = relicList.anchorMax = relicList.pivot = new Vector2(0, 1);
                relicList.sizeDelta = Vector2.zero;
            }
            Clear(relicList);

            relicList.anchoredPosition = new Vector2(outerGap, playerPanel.anchoredPosition.y);
            for (int i = 0; i < count; i++)
            {
                var relic = ctx.Run.Relics[i];
                var data = relic.Data;
                var tile = new GameObject("Relic", typeof(RectTransform), typeof(Image), typeof(CanvasGroup))
                    .GetComponent<Image>();
                var rect = tile.rectTransform;
                rect.SetParent(relicList, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(i / rows, -(i % rows)) * (size + gap);
                rect.sizeDelta = Vector2.one * size;
                tile.color = RelicTile;
                var shadow = tile.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
                shadow.effectDistance = new Vector2(0f, -1f);
                var border = tile.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(0.12f, 0.12f, 0.12f);
                border.effectDistance = new Vector2(1f, 1f);

                var fade = tile.GetComponent<CanvasGroup>();
                fade.alpha = relic.Enabled ? 1f : 0.35f;
                var pointer = tile.gameObject.AddComponent<BattleScreenPointer>();
                pointer.Enter = () => BeginInfoHover(tile, () => ShowInformation(data.DisplayName,
                    $"{T(relic.Enabled ? "relicOn" : "relicOff")}\n{data.Description}"));
                pointer.Exit = () => EndInfoHover(tile);
                pointer.LeftClick = () =>
                {
                    relic.Enabled = !relic.Enabled;
                    fade.alpha = relic.Enabled ? 1f : 0.35f;
                    foreach (var card in cards.Keys) RefreshCard(card);
                    if (ReferenceEquals(hoveredInfoOwner, tile)) RestorePanel();
                };

                var view = new RelicView { Relic = relic, Tile = tile, Fade = fade, ShownEnabled = relic.Enabled };
                relicViews.Add(view);
                if (data.icon != null)
                {
                    var icon = view.Icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    icon.rectTransform.SetParent(rect, false);
                    icon.rectTransform.anchorMin = Vector2.zero;
                    icon.rectTransform.anchorMax = Vector2.one;
                    float inset = size / 14f;
                    icon.rectTransform.offsetMin = Vector2.one * inset;
                    icon.rectTransform.offsetMax = -Vector2.one * inset;
                    icon.sprite = data.icon;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }
                else
                {
                    var label = view.Name = RelicLabel(rect, System.Globalization.StringInfo.GetNextTextElement(data.DisplayName),
                        Vector2.zero, Vector2.one * size);
                    label.color = Color.white;
                    label.fontStyle = FontStyle.Bold;
                }
                if (relic is ICountingRelic) view.Counter = RelicCounter(rect, size);
            }
            int extra = ctx.Run.Relics.Count - count;
            if (extra > 0)
                RelicLabel(relicList, $"+{extra}", new Vector2(0, -rows * (size + gap)), new Vector2(width, size));
            RefreshRelicStates();
        }

        /// <summary>유물 아이콘 오른쪽 아래의 누적 숫자 (채찍·독수리 깃털·캐스트 퍼즐).</summary>
        Text RelicCounter(RectTransform tile, float size)
        {
            var label = new GameObject("Counter", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var rect = label.rectTransform;
            rect.SetParent(tile, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
            rect.anchoredPosition = new Vector2(-size * 0.06f, size * 0.02f);
            rect.sizeDelta = new Vector2(size * 0.7f, size * 0.5f);
            label.font = screenFont;
            label.fontSize = Mathf.Max(10, Mathf.RoundToInt(size * 0.38f));
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.alignment = TextAnchor.LowerRight;
            label.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);
            return label;
        }

        /// <summary>
        /// 유물 아이콘의 누적 숫자와 흑백을 지금 상태에 맞춘다. 바뀐 것만 고친다 (LateUpdate에서 매 프레임).
        /// 흑백: 다 쓴 유물(Relic.Spent, 기계 심장)은 아이콘에 흑백 머티리얼, 이름 글자는 회색.
        /// </summary>
        void RefreshRelicStates()
        {
            bool enabledChanged = false;
            foreach (var view in relicViews)
            {
                if (view.Tile == null) continue;
                if (view.Relic.Enabled != view.ShownEnabled)
                {
                    view.ShownEnabled = view.Relic.Enabled;
                    if (view.Fade != null) view.Fade.alpha = view.ShownEnabled ? 1f : 0.35f;
                    if (ReferenceEquals(hoveredInfoOwner, view.Tile)) RestorePanel();
                    enabledChanged = true;
                }
                if (view.Counter != null && view.Relic is ICountingRelic counting)
                {
                    string text = counting.DisplayValue.ToString();
                    if (view.Counter.text != text) view.Counter.text = text;
                }
                bool spent = view.Relic.Spent;
                if (spent == view.ShownSpent) continue;
                view.ShownSpent = spent;
                view.Tile.color = spent ? SpentRelicTile : RelicTile;
                if (view.Icon != null) view.Icon.material = spent ? grayscaleMaterial : null;
                if (view.Name != null) view.Name.color = spent ? SpentRelicName : Color.white;
            }
            if (enabledChanged) foreach (var card in cards.Keys) RefreshCard(card);   // 유물이 바꾸는 카드 수치(피해 등) 다시 표시
        }

        Text RelicLabel(RectTransform parent, string value, Vector2 position, Vector2 size)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var rect = label.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            label.font = screenFont;
            label.fontSize = 18;
            label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        BattleCardView CachedCard(Button button)
        {
            if (!cardViews.TryGetValue(button, out var view))
            {
                view = button.GetComponent<BattleCardView>();
                view.KeywordFont = screenFont;
                view.KeywordName = T;
                view.KeywordEntered = ShowKeyword;
                view.KeywordExited = CloseKeyword;
                cardViews.Add(button, view);
            }
            return view;
        }

        Button AcquireHandCard()
        {
            var button = availableHandCards.Count > 0 ? availableHandCards.Pop() : Instantiate(cardPrefab, hand);
            button.transform.SetParent(hand, false);
            button.transform.localScale = Vector3.one;
            var fade = button.GetComponent<CanvasGroup>();
            if (fade) { fade.alpha = 1; fade.blocksRaycasts = true; }
            button.gameObject.SetActive(true);
            handView.Add((RectTransform)button.transform);
            return button;
        }

        void ReleaseHandCard(Button button)
        {
            CachedCard(button).Unbind();
            button.gameObject.SetActive(false);
            button.transform.SetParent(cardPoolRoot, false);
            availableHandCards.Push(button);
        }

        void BindCard(Button button, CardInstance card) =>
            CachedCard(button).Bind(card, OnCardClick, OnCardEntered, OnCardExited);

        void OnCardEntered(Button button, CardInstance card)
        {
            BeginInfoHover(button, () => ShowCard(card));
            RefreshHandLayout();
            if (selectedCard == null && M.CanInput && !modal.gameObject.activeSelf) Preview(card);
        }

        void OnCardExited(Button button)
        {
            // Keep the displayed card available while the pointer travels to its keyword buttons.
            EndInfoHover(button, restore: !keywordButtonsRoot.gameObject.activeSelf);
            RefreshHandLayout();
            if (selectedCard == null) ClearPreview();
        }

        void BindBrowserCard(Button button, CardInstance card)
        {
            CachedCard(button).Bind(card, InspectBrowserCard, OnCardEntered, OnCardExited);
            RefreshCardView(button, card, showPlayability: false);
        }

        void InspectBrowserCard(CardInstance card) => ShowCard(card);

        void BindRewardCard(Button button, CardInstance card)
        {
            CachedCard(button).Bind(card, SelectRewardCard, OnCardEntered, OnCardExited);
            RefreshCardView(button, card);
        }

        void SelectRewardCard(CardInstance card) => TakeRewardCard(card.Data).Forget();

        void RefreshHandLayout()
        {
            RectTransform chosen = null;
            if (selectedCard != null && cards.TryGetValue(selectedCard, out var view)) chosen = (RectTransform)view.transform;
            handView.Select(chosen);
            handView.Hover(hoveredInfoOwner is Button hovered && hovered.transform.parent == hand
                ? (RectTransform)hovered.transform : null);
            handView.ApplyIfDirty();
        }

        void RefreshTargets()
        {
            foreach (var entry in tiles)
            {
                var tile = ctx.Board.Get(entry.Key);
                boardViews[entry.Value].SetTarget(pickable != null &&
                    (pickable.Contains(entry.Key) || tile.Occupant != null && pickable.Contains(tile.Occupant)) ||
                    teleportMode && ctx.Board.IsPassable(entry.Key) && entry.Key != ctx.Player.Pos);
            }
            foreach (var entry in cards) CachedCard(entry.Value).SetTarget(selectedCard == entry.Key || pickable != null && pickable.Contains(entry.Key));
            foreach (var entry in enemyViews) unitViews[entry.Value].SetTarget(pickable != null && pickable.Contains(entry.Key));
            RefreshHandLayout();
        }

        void RefreshUnit(Unit unit)
        {
            if (unit == ctx.Player) RefreshHud();
            else if (unit is Enemy enemy) RefreshEnemy(enemyViews[enemy], enemy);
            else if (unit is Obstacle obstacle) RefreshTile(obstacle.Pos);
        }
    }
}
