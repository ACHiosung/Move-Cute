using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public sealed class BattleRewardListView : MonoBehaviour
    {
        [SerializeField] Button[] buttons;
        [SerializeField] Text[] labels;
        [SerializeField] RectTransform[] rows;

        public void Configure(Button gold, Button healing, Button card)
        {
            buttons = new[] { gold, healing, card };
            labels = new Text[3]; rows = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                labels[i] = buttons[i].GetComponentInChildren<Text>();
                rows[i] = (RectTransform)buttons[i].transform;
            }
        }

        public void Render(BattleRewards state, string gold, string healing, string card)
        {
            SetRow(0, gold, state.GoldClaimed);
            SetRow(1, healing, state.HealingClaimed);
            SetRow(2, card, state.CardResolved);
            int row = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                if (!buttons[i].gameObject.activeSelf) continue;
                var position = new Vector2(30, -110 - row++ * 105);
                if (rows[i].anchoredPosition == position) continue;
                rows[i].DOKill();
                rows[i].DOAnchorPos(position, .16f).SetUpdate(true).SetLink(buttons[i].gameObject);
            }
        }

        void SetRow(int i, string text, bool complete)
        {
            if (labels[i].text != text) labels[i].text = text;
            if (buttons[i].gameObject.activeSelf == complete) buttons[i].gameObject.SetActive(!complete);
            buttons[i].interactable = !complete;
        }
    }
}
