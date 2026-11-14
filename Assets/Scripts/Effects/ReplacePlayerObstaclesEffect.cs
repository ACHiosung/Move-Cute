using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 킬러 퀸: 플레이어가 설치한 모든 장애물(Obstacle.PlacedByPlayer)을 obstacle(연계 폭탄)로 교체한다.
    /// 원래 장애물은 파괴하지 않고 조용히 치운다 — 폭발·파괴 효과·ObstacleDestroyed 이벤트가 없다.
    /// 원래 있던 연계 폭탄도 치우고 새로 설치한다 (체력 초기화). 적이 설치한 장애물(묘비 등)은 그대로 둔다.
    /// </summary>
    [Serializable]
    public class ReplacePlayerObstaclesEffect : Effect
    {
        [Tooltip("교체할 장애물 (연계 폭탄)")]
        public ObstacleData obstacle;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            if (obstacle == null) return;
            foreach (var o in ctx.Obstacles.Where(o => o.PlacedByPlayer).ToList())
            {
                if (ctx.IsOver) return;
                ctx.Obstacles.Remove(o);
                ctx.Board.Get(o.Pos).Occupant = null;
                await ctx.PlaceObstacle(src, o.Pos, obstacle);
            }
        }
    }
}
