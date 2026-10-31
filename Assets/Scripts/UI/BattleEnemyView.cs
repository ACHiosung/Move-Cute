using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public sealed class BattleEnemyView : MonoBehaviour
    {
        [SerializeField] Image artwork;
        [SerializeField] Text morale, intent, statuses;
        [SerializeField] RectTransform bar, fill;
        [SerializeField] GameObject targetFrame;
        readonly List<string> statusIds = new();
        readonly List<int> statusStacks = new();
        IntentData displayedIntent;
        string displayedLanguage;
        bool intentDirty = true;
        int hp = int.MinValue, bonus, initial;
        float width = -1;

        public void CaptureReferences()
        {
            artwork = transform.Find("Art").GetComponent<Image>();
            morale = transform.Find("Morale").GetComponent<Text>();
            intent = transform.Find("Intent").GetComponent<Text>();
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

        public void SetMorale(int value, int change, int start)
        {
            if (!bar) CaptureReferences();
            float currentWidth = bar.rect.width;
            if (hp == value && bonus == change && initial == start && Mathf.Approximately(width, currentWidth)) return;
            hp = value; bonus = change; initial = start; width = currentWidth;
            morale.text = $"{value}{(change != 0 ? $" (+{change})" : "")} / {start}";
            fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * Mathf.Max(0, (float)value / Mathf.Max(1, start)));
        }

        public void InvalidateIntent() => intentDirty = true;
        public bool NeedsIntent(IntentData value, string language) => intentDirty || displayedIntent != value || displayedLanguage != language;
        public void SetIntent(IntentData value, string language, string text)
        {
            intent.text = text;
            displayedIntent = value;
            displayedLanguage = language;
            intentDirty = false;
        }

        public void SetStatuses(IReadOnlyList<Status> values)
        {
            bool changed = values.Count != statusIds.Count;
            if (!changed)
                for (int i = 0; i < values.Count; i++)
                    if (values[i].Id != statusIds[i] || values[i].Stacks != statusStacks[i]) { changed = true; break; }
            if (!changed) return;
            statusIds.Clear(); statusStacks.Clear();
            var text = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                statusIds.Add(values[i].Id); statusStacks.Add(values[i].Stacks);
                if (i > 0) text.Append(" · ");
                text.Append(values[i]);
            }
            statuses.text = text.ToString();
        }

        public void SetTarget(bool value)
        {
            if (targetFrame.activeSelf != value) targetFrame.SetActive(value);
        }

        void OnRectTransformDimensionsChange()
        {
            if (bar && hp != int.MinValue) SetMorale(hp, bonus, initial);
        }
    }
}
