using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 이동하면서 떠난 칸마다 장애물을 남기는 카드 (폭탄 설치 등).
    /// 예: 넘패드 9에서 ←← 이동 → 플레이어는 7, 9와 8에 장애물.
    ///
    /// 흑백(전체 이동 불가)이면 기본 카드처럼 가능한 만큼만 이동하고 장애물은 남기지 않는다.
    /// 남긴 장애물은 칸을 막으므로, 지나온 칸으로 되돌아가는 이동이 있으면 흑백이다.
    /// 에셋의 effects는 모든 이동이 끝난 뒤 기본 카드와 같이 발동한다.
    /// </summary>
    [Serializable]
    public class ObstacleTrailCard : CardInstance
    {
        [Tooltip("떠난 칸에 남길 장애물 (Assets/Data/Obstacles)")]
        public ObstacleData obstacle;

        public override bool CanPlayFully(BattleContext ctx)
        {
            var pos = ctx.Player.Pos;
            var visited = new HashSet<Vector2Int> { pos };
            foreach (var dir in Moves)
            {
                pos += dir.ToOffset();
                if (!ctx.Board.IsPassable(pos) || !visited.Add(pos)) return false;
            }
            return true;
        }

        public override async Awaitable Play(BattleContext ctx, EffectTarget target)
        {
            bool full = CanPlayFully(ctx);
            var src = new EffectSource(this);

            await ctx.SpendEnergy(PlayCost(ctx));
            await ctx.BeginCardPlay(this, full);
            if (full) await ctx.MovePlayerAlong(src, Moves, (from, to) => LeaveObstacle(ctx, src, from));
            else await ctx.MovePlayerAlong(src, Moves);
            if (full) await Data.effects.ExecuteAll(ctx, src, target);

            await Finish(ctx);
        }

        async Awaitable LeaveObstacle(BattleContext ctx, EffectSource src, Vector2Int pos)
        {
            await ctx.PlaceObstacle(src, pos, obstacle);
        }
    }
}
