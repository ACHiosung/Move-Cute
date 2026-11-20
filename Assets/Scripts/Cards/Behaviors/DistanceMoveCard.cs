using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 원하는 방향으로 거리 distance만큼 곧게 이동하는 카드 (축지법: 거리 1, 재사용 1, 잠금).
    /// - 카드 에셋의 target은 OtherTile. 낼 때 상하좌우로 distance칸 떨어져 있고 가는 길이 모두 비어 있는 칸만 후보로 뜬다.
    ///   경계 이동이 켜져 있으면 보드 밖으로 이어지는 길은 반대편 끝으로 이어진다 (ctx.StepTarget). 후보가 없으면 흑백.
    /// - 질주(Keyword_Dash)로 재사용되면 이미 고른 칸에 서 있으므로 갈 칸을 다시 고른다.
    ///   재사용 때의 선택은 취소할 수 있고, 취소하면 이동하지 않는다. 재사용 1을 기준으로 만들었다.
    /// - 에셋의 moves(화살표)는 잠금 해제 조건이다. MovePlayer는 해금된 카드가 출처인 이동을 막는데,
    ///   이 카드의 이동은 화살표가 아니라 효과이므로 플레이어 자신을 출처로 이동한다.
    /// - effects는 이동할 때마다 이동이 끝난 뒤 도착한 칸을 대상으로 발동한다.
    /// </summary>
    [Serializable]
    public class DistanceMoveCard : CardInstance
    {
        static readonly Direction[] Directions = { Direction.Left, Direction.Up, Direction.Right, Direction.Down };

        [Tooltip("한 번에 이동하는 칸 수 (한 방향으로 곧게). 설명문의 {distance}")]
        public int distance = 1;

        public override IEnumerable<Vector2Int> TargetTiles(BattleContext ctx) =>
            Directions.Where(d => CanGo(ctx, d)).Select(d => Destination(ctx, d)).Where(p => p != ctx.Player.Pos).Distinct();

        // 잠금 카드는 흑백. 해금 상태여도 갈 칸이 없으면 흑백
        public override bool CanPlayFully(BattleContext ctx) => !IsLocked && TargetTiles(ctx).Any();

        public override async Awaitable Play(BattleContext ctx, EffectTarget target)
        {
            var dest = target.Tile;
            if (dest == null || dest == ctx.Player.Pos)
            {
                // 재사용: 낼 때 고른 칸에 이미 서 있다 → 다시 고른다 (취소하면 이동 안 함)
                var prompt = Loc.Get("prompt.move_again", "{card}: 한 번 더 이동할 칸을 고르세요 (취소하면 이동하지 않음)")
                    .Replace("{card}", ToString());
                dest = await ctx.ChooseTile(TargetTiles(ctx).ToList(), prompt);
                if (dest == null) return;
            }

            // 경계 이동으로 두 방향이 같은 칸에 닿으면 (4×4에서 거리 2 좌우 등) 앞 방향(←↑→↓ 순)으로 간다
            var dirs = Directions.Where(d => CanGo(ctx, d) && Destination(ctx, d) == dest).ToList();
            if (dirs.Count == 0) return;

            await ctx.MovePlayerAlong(new EffectSource(ctx.Player), Enumerable.Repeat(dirs[0], distance));
            await Data.effects.ExecuteAll(ctx, new EffectSource(this), EffectTarget.OfTile(ctx.Player.Pos));
        }

        /// <summary>d 방향으로 distance칸 간 칸 (경계 이동이 켜져 있으면 반대편으로 이어진다).</summary>
        Vector2Int Destination(BattleContext ctx, Direction d)
        {
            var pos = ctx.Player.Pos;
            for (int i = 0; i < distance; i++) pos = ctx.StepTarget(pos, d);
            return pos;
        }

        /// <summary>d 방향으로 distance칸 가는 길이 모두 비어 있는지.</summary>
        bool CanGo(BattleContext ctx, Direction d)
        {
            if (distance <= 0) return false;
            var pos = ctx.Player.Pos;
            for (int i = 0; i < distance; i++)
            {
                pos = ctx.StepTarget(pos, d);
                if (!ctx.Board.IsPassable(pos)) return false;
            }
            return true;
        }
    }
}
