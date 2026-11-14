using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>대상 유닛(없으면 무작위 적)에게 쓰래기 통 카드 만큼 피해.</summary>
    [Serializable]
    public class TrashDamageEffect : Effect
    {
        public float scale = 1f;
        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit != null) await ctx.DealDamage(src, unit, (int)(ctx.Piles.Trash.Count * scale));
        }
    }
}
