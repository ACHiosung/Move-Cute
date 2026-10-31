using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>Reusable card visuals and pointer routing; no game-rule decisions.</summary>
    public sealed class BattleCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image artwork;
        [SerializeField] RectTransform directionRoot;
        [SerializeField] GameObject targetFrame;
        readonly List<Image> icons = new();
        readonly List<Direction> displayedMoves = new();
        CardInstance card;
        Action<CardInstance> clicked;
        Action<Button, CardInstance> entered;
        Action<Button> exited;
        Action cancelled;
        bool initialized, hovering;
        bool hasRendered;
        Material displayedMaterial;
        public Button Button => button;

        public void CaptureReferences()
        {
            button = GetComponent<Button>();
            artwork = transform.Find("Art").GetComponent<Image>();
            directionRoot = (RectTransform)transform.Find("DirectionIcons");
            targetFrame = transform.Find("TargetFrame").gameObject;
        }

        void Initialize()
        {
            if (initialized) return;
            if (!button || !artwork || !directionRoot || !targetFrame) CaptureReferences();
            foreach (Transform child in directionRoot) icons.Add(child.GetComponent<Image>());
            var pointer = GetComponent<BattleScreenPointer>();
            if (!pointer) pointer = gameObject.AddComponent<BattleScreenPointer>();
            button.onClick.AddListener(Click);
            pointer.Enter = Enter;
            pointer.Exit = Exit;
            pointer.RightClick = Cancel;
            initialized = true;
        }

        public void Bind(CardInstance value, Action<CardInstance> onClick,
            Action<Button, CardInstance> onEnter, Action<Button> onExit, Action onCancel)
        {
            Initialize();
            Unbind();
            card = value;
            clicked = onClick;
            entered = onEnter;
            exited = onExit;
            cancelled = onCancel;
        }

        public void Unbind()
        {
            if (hovering) Exit();
            card = null;
            clicked = null;
            entered = null;
            exited = null;
            cancelled = null;
        }

        void Click() { if (card != null) clicked?.Invoke(card); }
        void Enter() { hovering = true; if (card != null) entered?.Invoke(button, card); }
        void Exit() { hovering = false; exited?.Invoke(button); }
        void Cancel() => cancelled?.Invoke();

        public void Render(Sprite sprite, Material material, Color background,
            IReadOnlyList<Direction> moves, Sprite[] directions, bool selected)
        {
            Initialize();
            if (button.image.color != background) button.image.color = background;
            if (artwork.sprite != sprite) artwork.sprite = sprite;
            if (artwork.color != Color.white) artwork.color = Color.white;
            if (!hasRendered || displayedMaterial != material)
            {
                artwork.material = material;
                displayedMaterial = material;
            }
            hasRendered = true;
            bool changed = displayedMoves.Count != moves.Count;
            if (!changed)
                for (int i = 0; i < moves.Count; i++)
                    if (displayedMoves[i] != moves[i]) { changed = true; break; }
            if (changed)
            {
                displayedMoves.Clear();
                for (int i = 0; i < moves.Count; i++) displayedMoves.Add(moves[i]);
                while (icons.Count < moves.Count)
                {
                    var image = new GameObject("Direction", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(directionRoot, false);
                    image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0, 1);
                    image.raycastTarget = false;
                    image.preserveAspect = true;
                    icons.Add(image);
                }
                float size = Mathf.Min(34, 118f / Mathf.Max(1, moves.Count));
                for (int i = 0; i < icons.Count; i++)
                {
                    var image = icons[i];
                    image.gameObject.SetActive(i < moves.Count);
                    if (i >= moves.Count) continue;
                    image.sprite = directions[(int)moves[i]];
                    image.rectTransform.sizeDelta = Vector2.one * size;
                    image.rectTransform.anchoredPosition = new Vector2(4 + i * size, -4);
                }
            }
            SetTarget(selected);
        }

        public void SetTarget(bool value)
        {
            Initialize();
            if (targetFrame.activeSelf != value) targetFrame.SetActive(value);
        }
    }
}
