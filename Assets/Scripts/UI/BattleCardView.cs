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
        const float FreeFrameThickness = 3f;
        static readonly Color FreeFrameColor = new Color32(255, 212, 71, 255);
        readonly List<Image> icons = new();
        readonly List<Direction> displayedMoves = new();
        GameObject freeFrame;
        CardInstance card;
        Action<CardInstance> clicked;
        Action<Button, CardInstance> entered;
        Action<Button> exited;
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
            initialized = true;
        }

        public void Bind(CardInstance value, Action<CardInstance> onClick,
            Action<Button, CardInstance> onEnter, Action<Button> onExit)
        {
            Initialize();
            Unbind();
            card = value;
            clicked = onClick;
            entered = onEnter;
            exited = onExit;
        }

        public void Unbind()
        {
            if (hovering) Exit();
            card = null;
            clicked = null;
            entered = null;
            exited = null;
        }

        void Click() { if (card != null) clicked?.Invoke(card); }
        void Enter() { hovering = true; if (card != null) entered?.Invoke(button, card); }
        void Exit() { hovering = false; exited?.Invoke(button); }

        public void Render(Sprite sprite, Material material, Color background,
            IReadOnlyList<Direction> moves, Sprite[] directions, bool selected, bool showFreeFrame,
            int activeMoveStart = 0, int activeMoveCount = int.MaxValue, int matchedMoveCount = 0)
        {
            Initialize();
            if (button.image.color != background) button.image.color = background;
            if (artwork.sprite != sprite) artwork.sprite = sprite;
            if (artwork.color != Color.white) artwork.color = Color.white;
            // 아트가 없으면 흰 사각형이 카드 전체를 덮어 배경색(흑백 표시)이 안 보이므로 숨긴다
            bool hasArt = sprite != null;
            if (artwork.enabled != hasArt) artwork.enabled = hasArt;
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
            // 이동 배열이 같아도 유물·잠금 상태는 달라질 수 있다. 풀에서 재사용할 때 색도 복구한다.
            directionRoot.gameObject.SetActive(moves.Count > 0);
            for (int i = 0; i < moves.Count; i++)
            {
                int offset = i - activeMoveStart;
                icons[i].color = offset < 0 || offset >= activeMoveCount ? CardMoveDisplay.IgnoredColor
                    : offset < matchedMoveCount ? CardMoveDisplay.MatchedColor : Color.white;
            }
            SetFreeFrame(showFreeFrame);
            SetTarget(selected);
        }

        void SetFreeFrame(bool visible)
        {
            if (!freeFrame)
            {
                if (!visible) return;
                freeFrame = new GameObject("FreeFrame", typeof(RectTransform));
                freeFrame.SetActive(false);
                var root = (RectTransform)freeFrame.transform;
                root.SetParent(transform, false);
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = root.offsetMax = Vector2.zero;

                float thickness = FreeFrameThickness;
                AddFreeFrameEdge(root, "Top", Vector2.up, Vector2.one, new Vector2(0, -thickness), Vector2.zero);
                AddFreeFrameEdge(root, "Bottom", Vector2.zero, Vector2.right, Vector2.zero, new Vector2(0, thickness));
                AddFreeFrameEdge(root, "Left", Vector2.zero, Vector2.up, Vector2.zero, new Vector2(thickness, 0));
                AddFreeFrameEdge(root, "Right", Vector2.right, Vector2.one, new Vector2(-thickness, 0), Vector2.zero);
            }

            if (freeFrame.activeSelf == visible) return;
            freeFrame.SetActive(visible);
            // 선택 테두리를 안쪽으로 옮겨 두 상태를 함께 표시한다.
            var target = (RectTransform)targetFrame.transform;
            var inset = visible ? Vector2.one * FreeFrameThickness : Vector2.zero;
            target.offsetMin = inset;
            target.offsetMax = -inset;
        }

        static void AddFreeFrameEdge(RectTransform root, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var rect = edge.rectTransform;
            rect.SetParent(root, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            edge.color = FreeFrameColor;
            edge.raycastTarget = false;
        }

        public void SetTarget(bool value)
        {
            Initialize();
            if (targetFrame.activeSelf != value) targetFrame.SetActive(value);
        }
    }
}
