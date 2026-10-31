using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public sealed class BattleCellStripView : MonoBehaviour
    {
        readonly List<RectTransform> cells = new();
        int shown = -1, capacity = -1;
        bool geometryDirty;
        Image template;
        public void Render(Image image, int remaining, int maximum)
        {
            template = image;
            if (capacity == maximum && shown == remaining && !geometryDirty) return;
            capacity = Mathf.Max(0, maximum);
            shown = Mathf.Max(0, remaining);
            while (cells.Count < capacity)
            {
                var cell = Instantiate(template, transform);
                cells.Add(cell.rectTransform);
            }
            var root = (RectTransform)transform;
            float width = (root.rect.width - Mathf.Max(0, capacity - 1) * 5f) / Mathf.Max(1, capacity);
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                cell.gameObject.SetActive(i < shown && i < capacity);
                cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0, .5f);
                cell.anchoredPosition = new Vector2(i * (width + 5), 0);
                cell.sizeDelta = new Vector2(width, root.rect.height);
            }
            geometryDirty = false;
        }
        void OnRectTransformDimensionsChange() => geometryDirty = true;
        void LateUpdate() { if (geometryDirty && capacity >= 0) Render(template, shown, capacity); }
    }
}
