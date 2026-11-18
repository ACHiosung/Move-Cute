using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 칸 두 개를 골라 위에 있는 것(장애물·플레이어)을 맞바꾼다 (재배치). 빈 칸도 고를 수 있다.
    /// 장애물은 적이 놓은 것과 플레이어가 놓은 것 모두 해당한다. 두 번째 칸 후보에서는 첫 번째 칸을 뺀다.
    /// 어느 쪽에서든 취소하면 교환하지 않는다 (앞선 효과는 이미 발동한 상태).
    /// 교환 규칙(타일 효과는 칸에 남음, 플레이어는 이동으로 치지 않음)은 BattleContext.SwapTiles.
    ///
    /// TODO(취소 정책 미정): 기획에서는 "한 번 선택하면 취소할 수 없게" 할 예정이라 나중에 정한다.
    ///   지금은 효과 도중 칸 선택을 취소하면 교환만 생략된다 (카드·에너지·피해는 이미 사용된 상태).
    ///   카드를 내기 전에 칸을 고르게 해서 취소 = 카드를 내지 않음으로 만드는 방식도 검토했다가 보류했다
    ///   (CardInstance에 "대상 다음 추가 선택" 단계를 두고 BattleManager.PlayCard가 대상 선택 직후 호출).
    /// </summary>
    [Serializable]
    public class SwapTileContentsEffect : Effect
    {
        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var tiles = ctx.Board.AllTiles().Select(t => t.Pos).ToList();
            var first = await ctx.ChooseTile(tiles,
                Loc.Get("prompt.swap_first", "교환할 첫 번째 칸을 고르세요 (취소하면 교환하지 않음)"));
            if (first == null) return;

            var second = await ctx.ChooseTile(tiles.Where(p => p != first.Value).ToList(),
                Loc.Get("prompt.swap_second", "교환할 두 번째 칸을 고르세요 (취소하면 교환하지 않음)"));
            if (second == null) return;

            await ctx.SwapTiles(src, first.Value, second.Value);
        }
    }
}
