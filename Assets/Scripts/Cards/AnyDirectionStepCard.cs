using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 원하는 방향으로 한 칸 이동하는 카드 (폭탄 해체). 이동하지 않을 수는 없다.
    /// 카드 에셋의 target을 OtherTile로 둔다 → 낼 때 상하좌우 중 갈 수 있는 칸만 후보로 뜨고, 고른 칸으로 이동한다.
    /// 갈 수 있는 칸이 없으면 흑백이고 고를 칸이 없어 낼 수 없다.
    /// 에셋의 moves는 비워 둔다. effects는 이동이 끝난 뒤 기본 카드와 같이 발동한다.
    /// </summary>
    [Serializable]
    public class AnyDirectionStepCard : CardInstance
    {
        static readonly Direction[] Directions = { Direction.Left, Direction.Up, Direction.Right, Direction.Down };

        public override IEnumerable<Vector2Int> TargetTiles(BattleContext ctx) =>
            Directions.Select(d => ctx.Player.Pos + d.ToOffset()).Where(ctx.Board.IsPassable);

        public override bool CanPlayFully(BattleContext ctx) => TargetTiles(ctx).Any();

        public override async Awaitable Play(BattleContext ctx, EffectTarget target)
        {
            var src = new EffectSource(this);
            var from = ctx.Player.Pos;
            var steps = Directions.Where(d => target.Tile == from + d.ToOffset()).ToList();

            if (steps.Count == 1 && ctx.Board.IsPassable(from + steps[0].ToOffset()))
            {
                await ctx.MovePlayerAlong(src, steps);
                await Data.effects.ExecuteAll(ctx, src, target);
            }

            await Finish(ctx);
        }
    }
}
