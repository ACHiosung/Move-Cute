using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public sealed class BattleCellStripView : MonoBehaviour
    {
        internal const float CellGap = 5f;
        const float PendingAlpha = 0.5f;
        readonly List<Image> cells = new();
        int shown = -1, capacity = -1, pending;
        bool geometryDirty;
        Image template;
        public void Render(Image image, int remaining, int maximum, int pendingSpend = 0)
        {
            maximum = Mathf.Max(0, maximum);
            remaining = Mathf.Clamp(remaining, 0, maximum);
            pendingSpend = Mathf.Clamp(pendingSpend, 0, remaining);
            if (template == image && capacity == maximum && shown == remaining && pending == pendingSpend && !geometryDirty) return;
            template = image;
            capacity = maximum;
            shown = remaining;
            pending = pendingSpend;
            while (cells.Count < capacity)
                cells.Add(Instantiate(template, transform));
            var root = (RectTransform)transform;
            float width = Mathf.Max(0, root.rect.width - Mathf.Max(0, capacity - 1) * CellGap) / Mathf.Max(1, capacity);
            var normal = template.color;
            var translucent = normal;
            translucent.a *= PendingAlpha;
            for (int i = 0; i < cells.Count; i++)
            {
                var imageCell = cells[i];
                imageCell.gameObject.SetActive(i < shown);
                imageCell.color = i >= shown - pending && i < shown ? translucent : normal;
                var cell = imageCell.rectTransform;
                cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0, .5f);
                cell.anchoredPosition = new Vector2(i * (width + CellGap), 0);
                cell.sizeDelta = new Vector2(width, root.rect.height);
            }
            geometryDirty = false;
        }
        void OnRectTransformDimensionsChange() => geometryDirty = true;
        void LateUpdate() { if (geometryDirty && capacity >= 0) Render(template, shown, capacity, pending); }
    }
}
