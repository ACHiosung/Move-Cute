using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 플레이어에게서 거리 distance 이내의 폭탄(ObstacleData.isBomb)을 모두 터뜨린다 (폭탄 해체 카드).
    /// 거리는 이동 기준 (대각선 = 2). 순서는 마지막 턴 폭발과 같다 (Obstacle.InExplosionOrder).
    /// 앞의 폭발로 이미 부서진 폭탄은 건너뛴다.
    /// </summary>
    [Serializable]
    public class DetonateBombsInRangeEffect : Effect
    {
        [Tooltip("플레이어에게서 이 거리 이내의 폭탄을 터뜨린다. 설명문의 {distance}")]
        [Min(1)] public int distance = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var player = ctx.Player.Pos;
            var bombs = Obstacle.InExplosionOrder(
                ctx.Obstacles.Where(o => o.Data.isBomb && Board.Distance(o.Pos, player) <= distance), ctx.Board);
            foreach (var bomb in bombs)
                if (ctx.Obstacles.Contains(bomb)) await ctx.DestroyObstacle(src, bomb);
        }
    }
}
