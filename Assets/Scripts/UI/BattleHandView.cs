using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>Stable hand order and layout, updated only on membership/selection/size changes.</summary>
    public sealed class BattleHandView : MonoBehaviour
    {
        readonly List<RectTransform> items = new();
        RectTransform root, selected;
        bool dirty;
        public int LayoutPasses { get; private set; }

        void Awake()
        {
            root = (RectTransform)transform;
            var oldLayout = GetComponent<HorizontalLayoutGroup>();
            if (oldLayout) oldLayout.enabled = false;
        }

        public void Add(RectTransform item) { if (!items.Contains(item)) { items.Add(item); dirty = true; } }
        public void Remove(RectTransform item) { if (items.Remove(item)) dirty = true; }
        public void Clear() { items.Clear(); selected = null; dirty = true; }
        public void Select(RectTransform item) { if (selected != item) { selected = item; dirty = true; } }
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
                item.anchoredPosition = new Vector2(start + i * step, item == selected ? 26 : 0);
            }
            dirty = false;
            LayoutPasses++;
        }
    }
}
