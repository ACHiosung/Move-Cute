using System;
using System.Linq;
using UnityEngine;

// 예시 효과들. 형식 참고용이며, 실제 효과는 담당자가 이 파일 또는 새 파일에 추가한다.
namespace MoveCute
{
    /// <summary>대상 유닛(없으면 무작위 적)에게 피해.</summary>
    [Serializable]
    public class DamageEffect : Effect
    {
        public int damage = 6;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit != null) await ctx.DealDamage(src, unit, damage);
        }
    }

    /// <summary>적의 사기 증가. 출처가 적이면 그 적, 아니면 무작위 적.</summary>
    [Serializable]
    public class GainMoraleEffect : Effect
    {
        public int amount = 5;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var enemy = (src.Owner as Enemy) ?? (target.Unit as Enemy) ?? ctx.GetRandomEnemy();
            if (enemy != null) await ctx.ChangeMorale(src, enemy, amount);
        }
    }

    /// <summary>플레이어에게 즉시 피해 (적 기믹).</summary>
    [Serializable]
    public class DamagePlayerEffect : Effect
    {
        public int amount = 3;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            await ctx.DamagePlayer(src, amount);
        }
    }

    /// <summary>대상 타일(없으면 무작위 빈 타일)에 장애물 배치.</summary>
    [Serializable]
    public class PlaceObstacleEffect : Effect
    {
        public ObstacleData obstacle;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var pos = target.Tile ?? ctx.Board.GetRandomEmptyTile(ctx.Player.Pos);
            if (pos.HasValue) await ctx.PlaceObstacle(src, pos.Value, obstacle);
        }
    }

    [Serializable]
    public class DrawCardsEffect : Effect
    {
        public int count = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            await ctx.DrawCards(count);
        }
    }

    [Serializable]
    public class GainEnergyEffect : Effect
    {
        public int amount = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            await ctx.GainEnergy(amount);
        }
    }

    [Serializable]
    public class AddStatusEffect : Effect
    {
        public string statusId = "Guilt";
        public int stacks = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit != null) await ctx.AddStatus(src, unit, statusId, stacks);
        }
    }

    /// <summary>손패에서 골라 버린다. 효과 도중 플레이어 선택을 기다리는 예시.</summary>
    [Serializable]
    public class DiscardChosenCardEffect : Effect
    {
        public int count = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            for (int i = 0; i < count; i++)
            {
                var card = await ctx.ChooseCard(ctx.Piles.Hand.ToList(), "버릴 카드 선택");
                if (card == null) return;
                await ctx.DiscardCard(card);
            }
        }
    }
}
