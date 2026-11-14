using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// from에서 카드를 count장까지 골라 to로 옮긴다. 예: 재활용 = 휴지통 → 손패 1장.
    /// 고르는 방식·키워드 조건·흑백 판정은 ChooseCardsEffect. from과 to가 같으면 발동하지 않는다.
    /// </summary>
    [Serializable]
    public class MoveChosenCardsEffect : ChooseCardsEffect
    {
        [Tooltip("고른 카드를 보낼 곳")]
        public CardPileKind to = CardPileKind.Hand;

        [Tooltip("to가 뽑을 카드 더미일 때: 켜면 무작위 위치에 섞어 넣고, 끄면 맨 위(다음에 뽑힘)에 놓는다")]
        public bool shuffleIntoDraw;

        public MoveChosenCardsEffect() { from = CardPileKind.Trash; }

        public override bool CanExecute(BattleContext ctx) => from != to && base.CanExecute(ctx);

        public override Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target) =>
            from == to ? AwaitableUtil.Completed() : base.Execute(ctx, src, target);

        protected override Awaitable Apply(BattleContext ctx, CardInstance card) =>
            ctx.MoveCard(card, to, shuffleIntoDraw);

        /// <summary>예: "휴지통 → 손패: 카드를 고르세요 (1/2)"</summary>
        protected override string Prompt(int n, int total) =>
            FormatPrompt("prompt.move_cards", "{from} → {to}: 카드를 고르세요 ({n}/{total})", n, total)
                .Replace("{to}", PileName(to));
    }
}
