using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MoveCute
{
    public partial class BattleScreenUI
    {
        void OpenCards(string title, IEnumerable<CardInstance> pile, bool hideOrder = false)
        {
            if (pick != null) Back();
            if (ctx == null || (!M.CanInput && !rewardScreenActive) || pick != null) return;
            ClearSelection();
            viewedCards = hideOrder ? pile.OrderBy(c => c.Data.id, System.StringComparer.Ordinal)
                .ThenBy(c => string.Join(",", c.Moves.Select(d => (int)d)), System.StringComparer.Ordinal)
                .ThenBy(c => string.Join(",", c.Keywords.OrderBy(k => (int)k).Select(k => (int)k)), System.StringComparer.Ordinal)
                .ThenBy(c => c.Cost).ThenBy(c => c.IsGrayed).ToList() : pile.ToList();
            modalTitle.text = title;
            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();
            RenderModal();
        }

        void CloseCardBrowser()
        {
            modal.gameObject.SetActive(false);
            if (hoveredInfoOwner is Button owner && owner.transform.IsChildOf(modal)) EndInfoHover(owner);
            RestorePanel();
        }

        void RenderModal()
        {
            browserView.Render(viewedCards, cardPrefab, BindBrowserCard);
            var grid = modalContent.GetComponent<GridLayoutGroup>();
            int rows = (viewedCards.Count + grid.constraintCount - 1) / grid.constraintCount;
            float height = rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
            modalContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(cardBrowserScroll.viewport.rect.height, height));
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(modalContent);
            cardBrowserScroll.StopMovement();
            cardBrowserScroll.verticalNormalizedPosition = 1;
        }


    }
}
#if UNITY_EDITOR

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        public void ConfigureCardBrowserScroll()
        {
            if (cardBrowserScroll != null)
            {
                UpgradeScrollInput();
                return;
            }
            foreach (string name in new[] { "Previous", "Next" })
            {
                var old = modal.Find(name);
                if (old != null) DestroyImmediate(old.gameObject);
            }
            var viewport = Box("Viewport", modal, 35, 110, 980, 840, Color.clear).rectTransform;
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            modalContent.SetParent(viewport, false);
            modalContent.anchorMin = new Vector2(0, 1);
            modalContent.anchorMax = new Vector2(1, 1);
            modalContent.pivot = new Vector2(0, 1);
            modalContent.anchoredPosition = Vector2.zero;
            modalContent.sizeDelta = new Vector2(0, 840);
            cardBrowserScroll = viewport.gameObject.AddComponent<BattleCardScrollRect>();
            cardBrowserScroll.viewport = viewport;
            cardBrowserScroll.content = modalContent;
            cardBrowserScroll.horizontal = false;
            cardBrowserScroll.vertical = true;
            cardBrowserScroll.movementType = ScrollRect.MovementType.Clamped;
            cardBrowserScroll.scrollSensitivity = 55;
            cardBrowserScroll.inertia = true;
            var track = Box("ScrollBar", modal, 1022, 110, 14, 840, Panel);
            track.raycastTarget = true;
            var handle = RelativeBox("Handle", track.transform, 0, 0, 1, 1, Pale);
            handle.raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            cardBrowserScroll.verticalScrollbar = bar;
            cardBrowserScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        void UpgradeScrollInput()
        {
            if (cardBrowserScroll is BattleCardScrollRect) return;
            var old = cardBrowserScroll;
            var host = old.gameObject;
            var viewport = old.viewport;
            var content = old.content;
            var scrollbar = old.verticalScrollbar;
            var sensitivity = old.scrollSensitivity;
            var inertia = old.inertia;
            var deceleration = old.decelerationRate;
            var movement = old.movementType;
            var visibility = old.verticalScrollbarVisibility;
            DestroyImmediate(old);
            cardBrowserScroll = host.AddComponent<BattleCardScrollRect>();
            cardBrowserScroll.viewport = viewport;
            cardBrowserScroll.content = content;
            cardBrowserScroll.verticalScrollbar = scrollbar;
            cardBrowserScroll.verticalScrollbarVisibility = visibility;
            cardBrowserScroll.scrollSensitivity = sensitivity;
            cardBrowserScroll.inertia = inertia;
            cardBrowserScroll.decelerationRate = deceleration;
            cardBrowserScroll.movementType = movement;
            cardBrowserScroll.horizontal = false;
            cardBrowserScroll.vertical = true;
        }

        public void AddPanelSupports()
        {
            if (transform.Find("PanelSupports") != null) return;
            var supports = NewRect("PanelSupports", transform, 0, 0, 1920, 1080);
            supports.SetSiblingIndex(1); // Above the backdrop, behind both panel images.
            var color = new Color(.30f, .48f, .66f);
            Box("LeftSupport", supports, 110, 0, 14, 320, color);
            Box("RightSupport", supports, 425, 0, 14, 320, color);
        }

        public void UpdateRewardControls()
        {
            var skip = rewardCardsPanel.transform.Find("Skip");
            if (skip != null) DestroyImmediate(skip.gameObject);
            ((RectTransform)rewardBackButton.transform).anchoredPosition = new Vector2(265, -450);
        }
    }
}

#endif
