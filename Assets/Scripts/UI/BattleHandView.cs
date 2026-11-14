using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>Stable hand order with raised cards for selection and hover.</summary>
    public sealed class BattleHandView : MonoBehaviour
    {
        const float Lift = 26f;
        readonly List<RectTransform> items = new();
        RectTransform root, selected, hovered;
        bool dirty;
        public int LayoutPasses { get; private set; }

        void Awake()
        {
            root = (RectTransform)transform;
            var oldLayout = GetComponent<HorizontalLayoutGroup>();
            if (oldLayout) oldLayout.enabled = false;
        }

        public void Add(RectTransform item) { if (!items.Contains(item)) { items.Add(item); dirty = true; } }
        public void Remove(RectTransform item)
        {
            if (!items.Remove(item)) return;
            SetHitArea(item, 0);
            if (selected == item) selected = null;
            if (hovered == item) hovered = null;
            dirty = true;
        }

        public void Clear()
        {
            foreach (var item in items) SetHitArea(item, 0);
            items.Clear();
            selected = hovered = null;
            dirty = true;
        }

        public void Select(RectTransform item) { if (selected != item) { selected = item; dirty = true; } }
        public void Hover(RectTransform item) { if (hovered != item) { hovered = item; dirty = true; } }
        void OnRectTransformDimensionsChange() => dirty = true;
        void LateUpdate() => ApplyIfDirty();

        public void ApplyIfDirty()
        {
            if (!dirty) return;
            if (!root) root = (RectTransform)transform;
            const float width = 130, height = 173.3333f;
            float step = items.Count <= 1 ? 146 : Mathf.Min(146, (root.rect.width - width) / (items.Count - 1));
            float start = (root.rect.width - width - Mathf.Max(0, items.Count - 1) * step) / 2;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.anchorMin = item.anchorMax = item.pivot = new Vector2(0, 1);
                item.sizeDelta = new Vector2(width, height);
                float lift = item == selected || item == hovered ? Lift : 0;
                item.anchoredPosition = new Vector2(start + i * step, lift);
                SetHitArea(item, lift);
            }
            dirty = false;
            LayoutPasses++;
        }

        static void SetHitArea(RectTransform item, float lift)
        {
            // Negative bottom padding keeps the old lower edge clickable after the lift.
            if (item.TryGetComponent<Image>(out var image))
            {
                var padding = image.raycastPadding;
                padding.y = -lift;
                image.raycastPadding = padding;
            }
        }
    }
}
