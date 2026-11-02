using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 보드의 폭탄(ObstacleData.isBomb) 하나를 골라 바로 파괴한다 (HP 0). 파괴되므로 폭탄의 폭발 효과가 나간다.
    /// 폭탄이 없거나 선택을 취소하면 아무것도 하지 않는다.
    /// </summary>
    [Serializable]
    public class DetonateChosenBombEffect : Effect
    {
        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var bombs = ctx.Obstacles.Where(o => o.Data.isBomb).Cast<Unit>().ToList();
            if (await ctx.ChooseUnit(bombs, "터뜨릴 폭탄 선택") is Obstacle bomb)
                await ctx.DestroyObstacle(src, bomb);
        }
    }
}
