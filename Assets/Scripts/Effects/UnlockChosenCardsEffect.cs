using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// from에서 잠긴 카드를 count장까지 골라 해금한다 (만능열쇠: 손패의 잠금 카드 1장).
    /// 잠긴 카드만 후보다. keywords로 조건을 더 걸 수 있다. 잠긴 카드가 없으면 흑백.
    /// 해금은 ctx.UnlockCard — 자동 잠금이 있는 카드는 사용 후 다시 잠긴다.
    /// </summary>
    [Serializable]
    public class UnlockChosenCardsEffect : ChooseCardsEffect
    {
        protected override bool Accepts(CardInstance card) => card.IsLocked;

        protected override Awaitable Apply(BattleContext ctx, CardInstance card) => ctx.UnlockCard(card);

        /// <summary>예: "손패: 해금할 카드를 고르세요 (1/1)"</summary>
        protected override string Prompt(int n, int total) =>
            FormatPrompt("prompt.unlock_cards", "{from}: 해금할 카드를 고르세요 ({n}/{total})", n, total);
    }
}
