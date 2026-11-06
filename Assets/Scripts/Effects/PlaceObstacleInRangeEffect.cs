using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 플레이어에게서 거리 distance 이내의 빈 칸 하나를 골라 장애물을 놓는다 (폭탄 설치 카드).
    /// 거리는 이동 기준 (대각선 = 2, Board.Distance). 카드의 부가 효과라 이동이 모두 끝난 뒤 고른다.
    /// 선택을 취소하거나 빈 칸이 없으면 놓지 않는다 (설치 생략).
    /// </summary>
    [Serializable]
    public class PlaceObstacleInRangeEffect : Effect
    {
        [Tooltip("놓을 장애물 (Assets/Data/Obstacles)")]
        public ObstacleData obstacle;

        [Tooltip("플레이어에게서 이 거리 이내의 빈 칸에 놓는다. 설명문의 {distance}")]
        [Min(1)] public int distance = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var tiles = ctx.Board.EmptyTilesWithin(ctx.Player.Pos, distance);
            var pos = await ctx.ChooseTile(tiles, $"{obstacle.DisplayName} 설치할 칸 선택 (취소: 설치 안 함)");
            if (pos.HasValue) await ctx.PlaceObstacle(src, pos.Value, obstacle);
        }
    }
}
