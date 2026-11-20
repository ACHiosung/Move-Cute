using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 빈 칸 하나를 골라 비용 없이 순간이동한다 (빙고판 유물: 전투 시작 때 시작 칸 고르기 → ctx.TeleportFree).
    /// 후보는 장애물이 없는 모든 칸이고, 지금 서 있는 칸도 후보다 (고르면 그대로). 취소해도 그대로 있는다.
    /// </summary>
    [Serializable]
    public class TeleportToChosenTileEffect : Effect
    {
        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var tiles = ctx.Board.AllTiles().Where(t => t.Passable).Select(t => t.Pos)
                .OrderBy(ctx.Board.ReadingOrderKey).ToList();
            var prompt = Loc.Get("prompt.teleport_free", "{source}: 순간이동할 칸을 고르세요 (취소하면 그대로)")
                .Replace("{source}", src.ToString());
            var pos = await ctx.ChooseTile(tiles, prompt);
            if (pos.HasValue) await ctx.TeleportFree(src, pos.Value);
        }
    }
}
