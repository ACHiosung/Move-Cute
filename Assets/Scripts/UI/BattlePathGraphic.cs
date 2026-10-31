using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    // A single mesh, with a shaft ending exactly at its triangular arrowhead.
    [RequireComponent(typeof(CanvasRenderer))]
    public class BattlePathGraphic : MaskableGraphic
    {
        public Vector2 from, to;
        public float thickness = 10, headLength = 28, headWidth = 30;
        public bool arrowhead = true;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var direction = (to - from).normalized;
            if (direction == Vector2.zero) return;
            var side = new Vector2(-direction.y, direction.x);
            var neck = arrowhead ? to - direction * Mathf.Min(headLength, Vector2.Distance(from, to) * 0.4f) : to;
            Add(vh, from + side * thickness / 2);
            Add(vh, from - side * thickness / 2);
            Add(vh, neck - side * thickness / 2);
            Add(vh, neck + side * thickness / 2);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
            if (!arrowhead) return;
            Add(vh, neck + side * headWidth / 2);
            Add(vh, neck - side * headWidth / 2);
            Add(vh, to);
            vh.AddTriangle(4, 5, 6);
        }
        void Add(VertexHelper vh, Vector2 point) => vh.AddVert(point, color, Vector2.zero);
    }
}
