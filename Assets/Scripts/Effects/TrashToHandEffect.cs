using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 휴지통에서 카드를 count장 골라 손패로 가져온다 (재활용 카드).
    /// 고르는 창은 ctx.ChooseCardFromList (전투 화면에서는 휴지통 목록 창, 고르기 전에는 닫을 수 없음).
    /// 휴지통이 비어 있으면 발동할 수 없어서 카드가 흑백이 된다 (CanExecute).
    /// </summary>
    [Serializable]
    public class TrashToHandEffect : Effect
    {
        [Tooltip("가져올 카드 수. 설명문의 {count}")]
        [Min(1)] public int count = 1;

        public override bool CanExecute(BattleContext ctx) => ctx.Piles.Trash.Count > 0;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            for (int i = 0; i < count; i++)
            {
                var card = await ctx.ChooseCardFromList(ctx.Piles.Trash.ToList(),
                    Loc.Get("prompt.trash_to_hand", "휴지통에서 패로 가져올 카드를 고르세요"));
                if (card == null) return;
                await ctx.MoveCardToHand(card);
            }
        }
    }
}
