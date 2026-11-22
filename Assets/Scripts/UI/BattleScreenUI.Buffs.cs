using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        [Header("전투 버프 아이콘 (MOVE CUTE, 의욕 상실, 꽃단장)")]
        public Sprite[] battleBuffIcons = new Sprite[3];

        static readonly string[] BattleBuffIds =
            { StatusIds.MoveCute, StatusIds.Demotivation, StatusIds.HesitationBoost };
        static readonly string[] BattleBuffNameKeys =
            { "card.movecute.name", "card.demotivate.name", "card.primp.name" };
        const float BuffIconSize = 48f, BuffIconGap = 8f;
        RectTransform battleBuffRoot;
        readonly List<Image> battleBuffViews = new();
        readonly List<Text> battleBuffCounts = new();

        void RefreshBattleBuffs()
        {
            if (!battleBuffRoot) CreateBattleBuffDisplay();
            var boardFrame = (RectTransform)board.transform.parent;
            battleBuffRoot.anchoredPosition = boardFrame.anchoredPosition + Vector2.up * (BuffIconSize + 16f);
            int visible = 0;
            for (int i = 0; i < BattleBuffIds.Length; i++)
            {
                int stacks = ctx.GetStatusStacks(ctx.Player, BattleBuffIds[i]);
                var icon = battleBuffViews[i];
                bool active = !ctx.IsOver && stacks > 0;
                icon.gameObject.SetActive(active);
                if (!active) continue;
                icon.rectTransform.anchoredPosition = Vector2.right * visible++ * (BuffIconSize + BuffIconGap);
                battleBuffCounts[i].text = stacks.ToString();
                if (ReferenceEquals(hoveredInfoOwner, icon) && openKeyword == null) ShowBattleBuffInfo(i);
            }
            battleBuffRoot.gameObject.SetActive(visible > 0);
        }

        void CreateBattleBuffDisplay()
        {
            battleBuffRoot = new GameObject("BattleBuffs", typeof(RectTransform)).GetComponent<RectTransform>();
            battleBuffRoot.SetParent(transform, false);
            battleBuffRoot.anchorMin = battleBuffRoot.anchorMax = battleBuffRoot.pivot = Vector2.up;
            battleBuffRoot.sizeDelta = new Vector2(BattleBuffIds.Length * (BuffIconSize + BuffIconGap), BuffIconSize);
            battleBuffRoot.SetSiblingIndex(modal.GetSiblingIndex());
            for (int i = 0; i < BattleBuffIds.Length; i++)
            {
                int index = i;
                var icon = new GameObject(BattleBuffIds[i], typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.rectTransform.SetParent(battleBuffRoot, false);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = Vector2.up;
                icon.rectTransform.sizeDelta = Vector2.one * BuffIconSize;
                icon.color = Panel;
                var art = new GameObject("Art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                art.rectTransform.SetParent(icon.transform, false);
                art.rectTransform.anchorMin = Vector2.zero;
                art.rectTransform.anchorMax = Vector2.one;
                art.rectTransform.offsetMin = Vector2.one * 2f;
                art.rectTransform.offsetMax = -Vector2.one * 2f;
                art.sprite = i < battleBuffIcons.Length ? battleBuffIcons[i] : null;
                art.enabled = art.sprite != null;
                art.preserveAspect = true;
                art.raycastTarget = false;

                var count = new GameObject("Stacks", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                count.rectTransform.SetParent(icon.transform, false);
                count.rectTransform.anchorMin = Vector2.zero;
                count.rectTransform.anchorMax = Vector2.one;
                count.rectTransform.offsetMin = new Vector2(2, 0);
                count.rectTransform.offsetMax = new Vector2(-2, 0);
                count.font = screenFont;
                count.fontSize = count.resizeTextMaxSize = 18;
                count.resizeTextMinSize = 10;
                count.resizeTextForBestFit = true;
                count.fontStyle = FontStyle.Bold;
                count.alignment = TextAnchor.LowerRight;
                count.color = Color.white;
                count.raycastTarget = false;
                var outline = count.gameObject.AddComponent<Outline>();
                outline.effectColor = Ink;
                outline.effectDistance = Vector2.one;
                var pointer = icon.gameObject.AddComponent<BattleScreenPointer>();
                pointer.Enter = () => BeginInfoHover(icon, () => ShowBattleBuffInfo(index));
                pointer.Exit = () => EndInfoHover(icon);
                battleBuffViews.Add(icon);
                battleBuffCounts.Add(count);
            }
        }

        void ShowBattleBuffInfo(int index)
        {
            int stacks = ctx.GetStatusStacks(ctx.Player, BattleBuffIds[index]);
            string description = T(BattleBuffIds[index] + "BuffDesc")
                .Replace("{stacks}", stacks.ToString())
                .Replace("{percent}", ((long)stacks * BattleContext.HesitationBoostPercentPerStack).ToString());
            ShowInformation(Loc.Get(BattleBuffNameKeys[index]), description);
        }
    }
}
