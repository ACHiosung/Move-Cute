using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>Reusable card slots for both the scroll browser and reward candidates.</summary>
    public sealed class BattleCardCollectionView : MonoBehaviour
    {
        readonly List<BattleCardView> slots = new();
        public int Capacity => slots.Count;
        public void Render(IReadOnlyList<CardInstance> cards, Button template, Action<Button, CardInstance> bind)
        {
            while (slots.Count < cards.Count)
            {
                var slot = Instantiate(template, transform);
                slot.gameObject.SetActive(false);
                slots.Add(slot.GetComponent<BattleCardView>());
            }
            for (int i = 0; i < slots.Count; i++)
            {
                var view = slots[i];
                view.Unbind();
                view.gameObject.SetActive(i < cards.Count);
                if (i < cards.Count) bind(view.Button, cards[i]);
            }
        }
    }
}
