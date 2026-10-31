using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>One-shot render tint. Graphic colors can update underneath it without per-frame recoloring.</summary>
    public sealed class BattleBackdropView : MonoBehaviour
    {
        readonly Dictionary<CanvasRenderer, Color> rendererColors = new();
        readonly Dictionary<Selectable, Selectable.Transition> transitions = new();
        readonly Dictionary<CanvasGroup, (float alpha, bool interactable, bool raycasts)> inputGroups = new();
        public bool IsDimmed { get; private set; }
        public int TintPasses { get; private set; }

        public void Enter(Transform[] graphicsRoots, GameObject[] blockedRoots)
        {
            Exit();
            foreach (var root in graphicsRoots)
            {
                if (!root) continue;
                // Capture render state before disabling Selectable's color transitions.
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                {
                    var renderer = graphic.canvasRenderer;
                    rendererColors.TryAdd(renderer, renderer.GetColor());
                }
                foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
                {
                    transitions.TryAdd(selectable, selectable.transition);
                    selectable.transition = Selectable.Transition.None;
                    if (selectable.targetGraphic) selectable.targetGraphic.CrossFadeColor(Color.white, 0, true, true);
                }
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                {
                    var renderer = graphic.canvasRenderer;
                    var original = rendererColors[renderer];
                    renderer.SetColor(new Color(original.r * .45f, original.g * .45f, original.b * .45f, original.a));
                }
            }
            foreach (var root in blockedRoots)
            {
                var group = root.GetComponent<CanvasGroup>();
                if (!group) group = root.AddComponent<CanvasGroup>();
                inputGroups.TryAdd(group, (group.alpha, group.interactable, group.blocksRaycasts));
                group.interactable = group.blocksRaycasts = false;
            }
            IsDimmed = true;
            TintPasses++;
        }

        public void Exit()
        {
            foreach (var entry in inputGroups)
                if (entry.Key)
                {
                    entry.Key.alpha = entry.Value.alpha;
                    entry.Key.interactable = entry.Value.interactable;
                    entry.Key.blocksRaycasts = entry.Value.raycasts;
                }
            foreach (var entry in transitions)
                if (entry.Key)
                {
                    entry.Key.transition = entry.Value;
                    var graphic = entry.Key.targetGraphic;
                    // Restoring a transition starts its own color tween; stop it before restoring tint.
                    if (graphic && rendererColors.TryGetValue(graphic.canvasRenderer, out var tint))
                        graphic.CrossFadeColor(tint, 0, true, true);
                }
            foreach (var entry in rendererColors) if (entry.Key) entry.Key.SetColor(entry.Value);
            rendererColors.Clear();
            inputGroups.Clear();
            transitions.Clear();
            IsDimmed = false;
        }
    }
}
