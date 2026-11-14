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
            trashRect.anchoredPosition = discardRect.anchoredPosition - Vector2.right * (discardRect.rect.width + 12f);
            trashRect.SetSiblingIndex(discardRect.GetSiblingIndex());
            trashText = trashButton.GetComponentInChildren<Text>(true);
            closeBrowserButton = modal.Find("Close").GetComponent<Button>();
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
                var data = ctx.Run.Relics[i].Data;
                var tile = new GameObject("Relic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                var rect = tile.rectTransform;
                rect.SetParent(relicList, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(i / rows, -(i % rows)) * (size + gap);
                rect.sizeDelta = Vector2.one * size;
                tile.sprite = data.icon;
                tile.preserveAspect = true;
                tile.color = data.icon != null ? Color.white : Panel;

                var pointer = tile.gameObject.AddComponent<BattleScreenPointer>();
                pointer.Enter = () => BeginInfoHover(tile, () => ShowInformation(data.DisplayName, data.Description));
                pointer.Exit = () => EndInfoHover(tile);

                if (data.icon == null)
                    RelicLabel(rect, System.Globalization.StringInfo.GetNextTextElement(data.DisplayName), Vector2.zero, Vector2.one * size);
            }
            int extra = ctx.Run.Relics.Count - count;
            if (extra > 0)
                RelicLabel(relicList, $"+{extra}", new Vector2(0, -rows * (size + gap)), new Vector2(width, size));
        }

        void RelicLabel(RectTransform parent, string value, Vector2 position, Vector2 size)
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
        }

        BattleCardView CachedCard(Button button)
        {
            if (!cardViews.TryGetValue(button, out var view))
            {
                view = button.GetComponent<BattleCardView>();
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
            if (selectedCard == null && M.CanInput && !modal.gameObject.activeSelf) Preview(card);
        }

        void OnCardExited(Button button)
        {
            // Keep the displayed card available while the pointer travels to its keyword buttons.
            EndInfoHover(button, restore: !keywordButtonsRoot.gameObject.activeSelf);
            if (selectedCard == null) ClearPreview();
        }

        void BindBrowserCard(Button button, CardInstance card)
        {
            CachedCard(button).Bind(card, InspectBrowserCard, OnCardEntered, OnCardExited);
            RefreshCardView(button, card);
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
