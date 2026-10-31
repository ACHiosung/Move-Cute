using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    public sealed class BattlePreviewView : MonoBehaviour
    {
        readonly List<BattlePathGraphic> segments = new();
        int used;
        public int Capacity => segments.Count;
        public void Clear()
        {
            for (int i = 0; i < used; i++) segments[i].gameObject.SetActive(false);
            used = 0;
        }
        public void AddSegment(Vector3 fromWorld, Vector3 toWorld)
        {
            if (segments.Count == used)
            {
                var go = new GameObject("PathArrow", typeof(RectTransform), typeof(BattlePathGraphic));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = ((RectTransform)transform).rect.size;
                var graphic = go.GetComponent<BattlePathGraphic>();
                graphic.color = new Color(1f, .83f, .08f);
                graphic.raycastTarget = false;
                segments.Add(graphic);
            }
            var arrow = segments[used++];
            arrow.from = transform.InverseTransformPoint(fromWorld);
            arrow.to = transform.InverseTransformPoint(toWorld);
            arrow.gameObject.SetActive(true);
            arrow.SetVerticesDirty();
        }
    }
}
