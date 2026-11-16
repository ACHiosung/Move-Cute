using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// from에서 카드를 count장까지 골라 keyword를 부여한다 (날개: 손패 1장에 질주 / 신용 대출: 해금 + 자동 잠금).
    /// 이미 같은 키워드가 있는 카드는 후보에서 뺀다. 후보가 없으면 흑백.
    /// 잠금 키워드는 상태로 본다: 지금 잠겨 있는 카드와 이미 같은 상태(해금·자동 잠금)인 카드만 뺀다.
    /// 그래서 해금 상태의 잠금 카드는 후보가 된다 (신용 대출의 "잠금이 없는 카드").
    /// 이 효과를 가진 카드(날개 자신과 다른 날개)도 뺀다. 흑백 판정 때는 자기 자신이 아직 손패에 있어서,
    /// 빼지 않으면 다른 후보가 없는데도 흑백이 되지 않는다.
    /// 부여는 ctx.AddKeyword — 카드마다 키워드 복사본을 붙인다.
    /// </summary>
    [Serializable]
    public class AddKeywordToChosenCardsEffect : ChooseCardsEffect
    {
        [Tooltip("고른 카드에 부여할 키워드")]
        [SerializeReference, SubclassPicker] public CardKeyword keyword;

        public override bool CanExecute(BattleContext ctx) => keyword != null && base.CanExecute(ctx);

        protected override bool Accepts(CardInstance card) =>
            keyword != null && !AlreadyHas(card) && !card.Data.effects.Contains(this);

        bool AlreadyHas(CardInstance card) => keyword is Keyword_Lock wanted
            ? card.IsLocked || card.GetKeyword<Keyword_Lock>() is { } own &&
                               own.isUnlocked == wanted.isUnlocked && own.autoLock == wanted.autoLock
            : card.Has(keyword);

        protected override Awaitable Apply(BattleContext ctx, CardInstance card) => ctx.AddKeyword(card, keyword);

        /// <summary>예: "손패: 키워드를 부여할 카드를 고르세요 (1/1)"</summary>
        protected override string Prompt(int n, int total) =>
            FormatPrompt("prompt.add_keyword", "{from}: 키워드를 부여할 카드를 고르세요 ({n}/{total})", n, total);
    }
}
