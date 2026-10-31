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
        public BattleCellStripView energyStrip, turnStrip;
        public BattleRewardListView rewardListView;
        readonly Dictionary<Button, BattleCardView> cardViews = new();
        readonly Dictionary<Button, BattleEnemyView> unitViews = new();
        readonly Dictionary<Button, BattleTileView> boardViews = new();
        readonly Stack<Button> availableHandCards = new();
        RectTransform cardPoolRoot;
        Transform detailPanelRoot, playerPanelRoot;
        GameObject mouseIcon;
        Button deckButton, drawButton, discardButton, closeBrowserButton;
        GameObject backgroundObject;
        bool lastEmptyEnergy;
        Transform[] backdropRoots;
        GameObject[] blockedBattleRoots;

        void InitializeViews()
        {
            // One-time lookups for the root-level layout; card/target parts are serialized in each view.
            detailPanelRoot = detailName.transform.parent;
            playerPanelRoot = goldBalance.transform.parent;
            mouseIcon = detailPanelRoot.Find("MouseIcon").gameObject;
            deckButton = playerPanelRoot.Find("Deck").GetComponent<Button>();
            drawButton = drawText.transform.parent.GetComponent<Button>();
            discardButton = discardText.transform.parent.GetComponent<Button>();
            closeBrowserButton = modal.Find("Close").GetComponent<Button>();
            backgroundObject = transform.Find("Background").gameObject;
            cardPoolRoot = (RectTransform)transform.Find("Templates");
            backdropRoots = new[] { backgroundObject.transform, board.transform.parent,
                hand, playerMarker, energyButton.transform, endTurnButton.transform,
                drawButton.transform, discardButton.transform };
            blockedBattleRoots = new[] { hand.gameObject, playerMarker.gameObject, energyButton.gameObject,
                endTurnButton.gameObject, drawButton.gameObject, discardButton.gameObject };
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
            CachedCard(button).Bind(card, OnCardClick, OnCardEntered, OnCardExited, Back);

        void OnCardEntered(Button button, CardInstance card)
        {
            BeginInfoHover(button, () => ShowCard(card));
            if (selectedCard == null && M.CanInput && !modal.gameObject.activeSelf) Preview(card);
        }

        void OnCardExited(Button button)
        {
            EndInfoHover(button);
            if (selectedCard == null) ClearPreview();
        }

        void BindBrowserCard(Button button, CardInstance card)
        {
            CachedCard(button).Bind(card, InspectBrowserCard, OnCardEntered, OnCardExited, Back);
            RefreshCardView(button, card);
        }

        void InspectBrowserCard(CardInstance card) { ShowCard(card); ShowKeywords(); }

        void BindRewardCard(Button button, CardInstance card)
        {
            CachedCard(button).Bind(card, SelectRewardCard, OnCardEntered, OnCardExited, Back);
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
