using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>보드 위 장애물 (폭탄, 묘비, 좀비 등). 칸을 막고 HP가 있다.</summary>
    public class Obstacle : Unit
    {
        public readonly ObstacleData Data;
        public Vector2Int Pos;

        public Obstacle(ObstacleData data, Vector2Int pos)
        {
            Data = data;
            Pos = pos;
            Hp = data.hp;
        }

        /// <summary>부서질 때 효과 (폭발 등). 대상은 장애물이 있던 칸.</summary>
        public Awaitable OnDestroyed(BattleContext ctx) =>
            Data.onDestroyed.ExecuteAll(ctx, new EffectSource(this), EffectTarget.OfTile(Pos));

        /// <summary>
        /// 제한 턴의 마지막 턴이 끝났을 때. 아직 남아 있으면 onLastTurnEnd 효과를 실행하고,
        /// destroyOnLastTurnEnd면 파괴한다 (파괴 시 효과까지). 앞선 장애물의 폭발로 이미 부서졌으면 아무것도 안 한다.
        /// </summary>
        public async Awaitable OnLastTurnEnd(BattleContext ctx)
        {
            if (!ctx.Obstacles.Contains(this)) return;
            await Data.onLastTurnEnd.ExecuteAll(ctx, new EffectSource(this), EffectTarget.OfTile(Pos));
            if (Data.destroyOnLastTurnEnd) await ctx.DestroyObstacle(new EffectSource(this), this);
        }

        public override string DisplayName => Data != null ? Data.DisplayName : "Obstacle";

        /// <summary>
        /// 여러 장애물을 차례로 처리(폭발)할 순서: lastTurnPriority 높은 것부터 (예술 → 연계 → 폭탄),
        /// 같으면 보드 읽는 순서 (위쪽 줄부터, 줄 안에서는 왼쪽부터).
        /// </summary>
        public static List<Obstacle> InExplosionOrder(IEnumerable<Obstacle> obstacles, Board board) =>
            obstacles.OrderByDescending(o => o.Data.lastTurnPriority)
                .ThenBy(o => board.ReadingOrderKey(o.Pos)).ToList();
    }
}
