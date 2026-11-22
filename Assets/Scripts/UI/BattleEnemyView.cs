using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public sealed class BattleEnemyView : MonoBehaviour
    {
        [SerializeField] Image artwork;
        [SerializeField] Text morale, statuses;
        [SerializeField] RectTransform bar, fill;
        [SerializeField] GameObject targetFrame;
        Text hesitationCount;
        readonly List<string> statusIds = new();
        readonly List<int> statusStacks = new();
        int hp = int.MinValue, bonus, maxHp;
        float width = -1;

        void Awake()
        {
            var legacyIntent = transform.Find("Intent");
            if (legacyIntent) legacyIntent.gameObject.SetActive(false);
        }

        public void CaptureReferences()
        {
            artwork = transform.Find("Art").GetComponent<Image>();
            morale = transform.Find("Morale").GetComponent<Text>();
            statuses = transform.Find("Statuses").GetComponent<Text>();
            bar = (RectTransform)transform.Find("Bar");
            fill = (RectTransform)transform.Find("Bar/Fill");
            targetFrame = transform.Find("Art/TargetFrame").gameObject;
        }

        public void SetArtwork(Sprite sprite)
        {
            if (!artwork) CaptureReferences();
            artwork.sprite = sprite;
            if (sprite) artwork.color = Color.white;
        }

        public void SetMorale(int value, int change, int maximum)
        {
            if (!bar) CaptureReferences();
            float currentWidth = bar.rect.width;
            if (hp == value && bonus == change && maxHp == maximum && Mathf.Approximately(width, currentWidth)) return;
            hp = value; bonus = change; maxHp = maximum; width = currentWidth;
            morale.text = $"{value}{(change != 0 ? $" (+{change})" : "")} / {maximum}";
            fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * Mathf.Clamp01((float)value / Mathf.Max(1, maximum)));
        }

        public void SetStatuses(IReadOnlyList<Status> values)
        {
            if (!hesitationCount) CreateHesitationDisplay();
            bool changed = values.Count != statusIds.Count;
            if (!changed)
                for (int i = 0; i < values.Count; i++)
                    if (values[i].Id != statusIds[i] || values[i].Stacks != statusStacks[i]) { changed = true; break; }
            if (!changed) return;
            statusIds.Clear(); statusStacks.Clear();
            var text = new StringBuilder();
            int hesitation = 0;
            for (int i = 0; i < values.Count; i++)
            {
                statusIds.Add(values[i].Id); statusStacks.Add(values[i].Stacks);
                if (values[i].Id == StatusIds.Hesitation)
                {
                    hesitation += values[i].Stacks;
                    continue;
                }
                if (text.Length > 0) text.Append(" · ");
                text.Append(values[i]);
            }
            hesitationCount.text = hesitation.ToString();
            statuses.text = text.ToString();
        }

        void CreateHesitationDisplay()
        {
            if (!bar) CaptureReferences();
            var root = new GameObject("Hesitation", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = root.anchorMax = new Vector2(bar.anchorMax.x, (bar.anchorMin.y + bar.anchorMax.y) * 0.5f);
            root.pivot = new Vector2(0, 0.5f);
            root.anchoredPosition = new Vector2(12f, bar.anchoredPosition.y);
            root.sizeDelta = new Vector2(112f, 34f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.rectTransform.SetParent(root, false);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
            icon.rectTransform.pivot = new Vector2(0, 0.5f);
            icon.rectTransform.sizeDelta = new Vector2(30f, 30f);
            icon.color = new Color(0.43f, 0.30f, 0.62f);
            icon.raycastTarget = false;

            var symbol = new GameObject("Symbol", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            symbol.rectTransform.SetParent(icon.transform, false);
            symbol.rectTransform.anchorMin = Vector2.zero;
            symbol.rectTransform.anchorMax = Vector2.one;
            symbol.rectTransform.offsetMin = symbol.rectTransform.offsetMax = Vector2.zero;
            symbol.font = morale.font;
            symbol.fontSize = 24;
            symbol.fontStyle = FontStyle.Bold;
            symbol.alignment = TextAnchor.MiddleCenter;
            symbol.color = Color.white;
            symbol.text = "?";
            symbol.raycastTarget = false;

            hesitationCount = Instantiate(symbol, root);
            hesitationCount.name = "Count";
            hesitationCount.rectTransform.offsetMin = new Vector2(38f, 0);
            hesitationCount.rectTransform.offsetMax = Vector2.zero;
            hesitationCount.alignment = TextAnchor.MiddleLeft;
            hesitationCount.color = new Color(0.10f, 0.16f, 0.23f);
            hesitationCount.fontSize = hesitationCount.resizeTextMaxSize = 26;
            hesitationCount.resizeTextMinSize = 10;
            hesitationCount.resizeTextForBestFit = true;
            hesitationCount.text = "0";
        }

        public void SetTarget(bool value)
        {
            if (targetFrame.activeSelf != value) targetFrame.SetActive(value);
        }

        void OnRectTransformDimensionsChange()
        {
            if (bar && hp != int.MinValue) SetMorale(hp, bonus, maxHp);
        }
    }
}
