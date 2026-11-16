using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>대상 유닛(없으면 무작위 적)에게 쓰래기 통 카드 만큼 피해.</summary>
    [Serializable]
    public class TrashDamageEffect : Effect
    {
        public float scale = 1f;
        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit != null) await ctx.DealDamage(src, unit, new Damage(ctx.Piles.Trash.Count).Multiply((decimal)scale));
        }
    }

    /// <summary>
    /// 대상 유닛(없으면 무작위 적)에게 피해. 보드의 빈 타일 하나마다 피해량이 perEmptyTile 증가 (전력질주 1, 난동피우기 2).
    /// 빈 타일 = 지나갈 수 있는 칸 (Tile.Passable, 장애물 없음). 플레이어가 서 있는 칸도 센다.
    /// 이동이 끝난 뒤 발동하는 순간에 센다 (질주면 재사용마다 다시 셈).
    /// 증가분은 합연산이라 주저 배율보다 먼저 더해진다.
    /// </summary>
    [Serializable]
    public class DamagePerEmptyTileEffect : Effect
    {
        [Tooltip("기본 피해. 설명문의 {damage}")]
        public int damage;

        [Tooltip("빈 타일 하나마다 더하는 피해. 설명문의 {perEmptyTile}")]
        public int perEmptyTile = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit == null) return;
            int empty = ctx.Board.AllTiles().Count(t => t.Passable);
            await ctx.DealDamage(src, unit, new Damage(damage).Add(perEmptyTile * empty));
        }
    }
}
