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
        readonly List<BuffIcon> battleBuffViews = new();

        /// <summary>
        /// 버프 카드가 건 버프(ctx.Buffs: 카운터·노력의 결실·세계의 경계)의 아이콘. 필요한 만큼 만들어 두고 다시 쓴다.
        /// shownBuffs[i]가 cardBuffViews[i]에 보이는 버프다.
        /// </summary>
        readonly List<BuffIcon> cardBuffViews = new();
        readonly List<Relic> shownBuffs = new();

        /// <summary>버프 아이콘 하나: 바탕, 그림, 오른쪽 아래 수치, 그림이 없을 때 대신 보여 줄 이름 첫 글자.</summary>
        sealed class BuffIcon
        {
            public Image Tile, Art;
            public Text Stacks, Letter;
        }

        /// <summary>
        /// 보드 위 버프 줄. 상태 버프(MOVE CUTE·의욕 상실·꽃단장) 다음에 버프 카드가 건 버프를 건 순서대로 놓는다.
        /// HUD를 다시 그릴 때마다(RefreshScreenHud) 불린다.
        /// </summary>
        void RefreshBattleBuffs()
        {
            if (!battleBuffRoot) CreateBattleBuffDisplay();
            var boardFrame = (RectTransform)board.transform.parent;
            battleBuffRoot.anchoredPosition = boardFrame.anchoredPosition + Vector2.up * (BuffIconSize + 16f);
            int visible = 0;
            for (int i = 0; i < BattleBuffIds.Length; i++)
            {
                int stacks = ctx.GetStatusStacks(ctx.Player, BattleBuffIds[i]);
                var view = battleBuffViews[i];
                if (!PlaceBuffIcon(view, !ctx.IsOver && stacks > 0, ref visible)) continue;
                SetBuffArt(view, i < battleBuffIcons.Length ? battleBuffIcons[i] : null, Loc.Get(BattleBuffNameKeys[i]));
                view.Stacks.text = stacks.ToString();
                if (ReferenceEquals(hoveredInfoOwner, view.Tile) && openKeyword == null) ShowBattleBuffInfo(i);
            }

            shownBuffs.Clear();
            if (!ctx.IsOver)
                foreach (var buff in ctx.Buffs)
                    if (buff.Enabled) shownBuffs.Add(buff);
            for (int i = 0; i < shownBuffs.Count || i < cardBuffViews.Count; i++)
            {
                if (i == cardBuffViews.Count)
                {
                    int index = i;
                    cardBuffViews.Add(CreateBuffIcon("CardBuff", () => ShowCardBuffInfo(index)));
                }
                var view = cardBuffViews[i];
                if (!PlaceBuffIcon(view, i < shownBuffs.Count, ref visible)) continue;
                var buff = shownBuffs[i];
                SetBuffArt(view, buff.Data.icon, buff.Data.DisplayName);
                // 쌓이지 않는 버프(세계의 경계·노력의 결실)는 수치가 늘 1이라 숫자를 숨긴다
                view.Stacks.text = buff.Stackable || buff.Stacks != 1 ? buff.Stacks.ToString() : "";
                if (ReferenceEquals(hoveredInfoOwner, view.Tile) && openKeyword == null) ShowCardBuffInfo(i);
            }
            battleBuffRoot.sizeDelta = new Vector2(visible * (BuffIconSize + BuffIconGap), BuffIconSize);
            battleBuffRoot.gameObject.SetActive(visible > 0);
        }

        /// <summary>
        /// 아이콘을 켜거나 끈다. 켜면 다음 자리(visible번째)에 놓고 true를 돌려준다.
        /// 마우스가 올라가 있던 아이콘이 꺼지면 정보 패널도 되돌린다 (꺼진 아이콘에는 마우스 나감이 오지 않는다).
        /// </summary>
        bool PlaceBuffIcon(BuffIcon view, bool active, ref int visible)
        {
            view.Tile.gameObject.SetActive(active);
            if (!active)
            {
                EndInfoHover(view.Tile);
                return false;
            }
            view.Tile.rectTransform.anchoredPosition = Vector2.right * visible++ * (BuffIconSize + BuffIconGap);
            return true;
        }

        /// <summary>그림이 있으면 그림을, 없으면 이름 첫 글자를 보여 준다 (유물 목록과 같은 방식).</summary>
        static void SetBuffArt(BuffIcon view, Sprite sprite, string name)
        {
            view.Art.sprite = sprite;
            view.Art.enabled = sprite != null;
            view.Letter.text = sprite != null ? "" : System.Globalization.StringInfo.GetNextTextElement(name);
        }

        void CreateBattleBuffDisplay()
        {
            battleBuffRoot = new GameObject("BattleBuffs", typeof(RectTransform)).GetComponent<RectTransform>();
            battleBuffRoot.SetParent(transform, false);
            battleBuffRoot.anchorMin = battleBuffRoot.anchorMax = battleBuffRoot.pivot = Vector2.up;
            battleBuffRoot.SetSiblingIndex(modal.GetSiblingIndex());
            for (int i = 0; i < BattleBuffIds.Length; i++)
            {
                int index = i;
                battleBuffViews.Add(CreateBuffIcon(BattleBuffIds[i], () => ShowBattleBuffInfo(index)));
            }
        }

        BuffIcon CreateBuffIcon(string objectName, System.Action showInfo)
        {
            var tile = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            tile.rectTransform.SetParent(battleBuffRoot, false);
            tile.rectTransform.anchorMin = tile.rectTransform.anchorMax = tile.rectTransform.pivot = Vector2.up;
            tile.rectTransform.sizeDelta = Vector2.one * BuffIconSize;
            tile.color = Panel;
            var art = new GameObject("Art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            art.rectTransform.SetParent(tile.transform, false);
            art.rectTransform.anchorMin = Vector2.zero;
            art.rectTransform.anchorMax = Vector2.one;
            art.rectTransform.offsetMin = Vector2.one * 2f;
            art.rectTransform.offsetMax = -Vector2.one * 2f;
            art.preserveAspect = true;
            art.raycastTarget = false;

            var letter = new GameObject("Letter", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            letter.rectTransform.SetParent(tile.transform, false);
            letter.rectTransform.anchorMin = Vector2.zero;
            letter.rectTransform.anchorMax = Vector2.one;
            letter.rectTransform.offsetMin = letter.rectTransform.offsetMax = Vector2.zero;
            letter.font = screenFont;
            letter.fontSize = 24;
            letter.fontStyle = FontStyle.Bold;
            letter.alignment = TextAnchor.MiddleCenter;
            letter.color = Ink;
            letter.raycastTarget = false;

            var count = new GameObject("Stacks", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            count.rectTransform.SetParent(tile.transform, false);
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
            var pointer = tile.gameObject.AddComponent<BattleScreenPointer>();
            pointer.Enter = () => BeginInfoHover(tile, showInfo);
            pointer.Exit = () => EndInfoHover(tile);
            return new BuffIcon { Tile = tile, Art = art, Stacks = count, Letter = letter };
        }

        void ShowBattleBuffInfo(int index)
        {
            int stacks = ctx.GetStatusStacks(ctx.Player, BattleBuffIds[index]);
            string description = T(BattleBuffIds[index] + "BuffDesc")
                .Replace("{stacks}", stacks.ToString())
                .Replace("{percent}", ((long)stacks * BattleContext.HesitationBoostPercentPerStack).ToString());
            ShowInformation(Loc.Get(BattleBuffNameKeys[index]), description);
        }

        /// <summary>버프 카드가 건 버프의 이름과 설명. 설명의 {damage}·{percent} 등은 버프 에셋 값으로 바뀐다.</summary>
        void ShowCardBuffInfo(int index)
        {
            if (index >= shownBuffs.Count) return;
            var data = shownBuffs[index].Data;
            ShowInformation(data.DisplayName, data.Description);
        }
    }
}
