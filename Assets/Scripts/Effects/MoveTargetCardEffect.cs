using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 효과 대상 카드(target.Card)가 from 더미에 있으면 to로 옮긴다.
    /// 유물의 카드 시점(해금·삭제·재사용·사용)이 그 카드를 대상으로 넘긴다.
    /// 예: 칭찬 스티커 = 해금된 카드가 버린 카드 더미에 있으면 뽑을 카드 더미 맨 위로.
    /// </summary>
    [Serializable]
    public class MoveTargetCardEffect : Effect
    {
        [Tooltip("대상 카드가 이 더미에 있을 때만 옮긴다")]
        public CardPileKind from = CardPileKind.Discard;

        [Tooltip("옮길 곳")]
        public CardPileKind to = CardPileKind.Draw;

        [Tooltip("to가 뽑을 카드 더미일 때: 켜면 무작위 위치에 섞어 넣고, 끄면 맨 위(다음에 뽑힘)에 놓는다")]
        public bool shuffleIntoDraw;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var card = target.Card;
            if (card == null || from == to || !ctx.Piles.Get(from).Contains(card)) return;
            await ctx.MoveCard(card, to, shuffleIntoDraw);
        }
    }
}
