using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// from에서 카드를 count장까지 한 장씩 골라 to로 옮긴다. 예: 재활용 = 휴지통 → 손패 1장.
    /// 고르는 창은 ctx.ChooseCardFromList (전투 화면에서는 목록 창, 고르기 전에는 닫을 수 없음).
    /// from의 카드가 count보다 적으면 있는 만큼만 고른다. from이 비어 있으면 발동할 수 없어 카드가 흑백이다.
    /// 뽑을 카드 더미에서 고를 때는 뽑을 순서가 드러나지 않게 정렬해서 보여 준다.
    /// </summary>
    [Serializable]
    public class MoveChosenCardsEffect : Effect
    {
        [Tooltip("고를 카드가 있는 곳")]
        public CardPileKind from = CardPileKind.Trash;

        [Tooltip("고른 카드를 보낼 곳")]
        public CardPileKind to = CardPileKind.Hand;

        [Tooltip("고를 카드 수 (있는 만큼만). 설명문의 {count}")]
        public BattleInt count = 1;

        [Tooltip("to가 뽑을 카드 더미일 때: 켜면 무작위 위치에 섞어 넣고, 끄면 맨 위(다음에 뽑힘)에 놓는다")]
        public bool shuffleIntoDraw;

        public override bool CanExecute(BattleContext ctx) => from != to && ctx.Piles.Get(from).Count > 0;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            if (from == to) return;
            int total = Math.Min(count.Get(ctx), ctx.Piles.Get(from).Count);
            for (int i = 0; i < total; i++)
            {
                var candidates = ctx.Piles.Get(from).ToList();
                if (candidates.Count == 0) return;
                if (from == CardPileKind.Draw)
                    candidates = candidates.OrderBy(c => c.Data.id, StringComparer.Ordinal).ToList();

                var card = await ctx.ChooseCardFromList(candidates, Prompt(i + 1, total));
                if (card == null) return;
                await ctx.MoveCard(card, to, shuffleIntoDraw);
            }
        }

        /// <summary>예: "휴지통 → 손패: 카드를 고르세요 (1/2)"</summary>
        string Prompt(int n, int total) =>
            Loc.Get("prompt.move_cards", "{from} → {to}: 카드를 고르세요 ({n}/{total})")
                .Replace("{from}", PileName(from)).Replace("{to}", PileName(to))
                .Replace("{n}", n.ToString()).Replace("{total}", total.ToString());

        static string PileName(CardPileKind pile) =>
            Loc.Get("pile." + pile.ToString().ToLowerInvariant(), pile.ToString());
    }
}
