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
            if (bar && hp != int.MinValue) SetMorale(hp, bonus, maxHp);
        }
    }
}
