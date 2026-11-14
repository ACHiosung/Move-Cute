using System;
using System.Collections.Generic;
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
    [Serializable]
    public class RandomAddCardKeyword : Effect
    {
        [SerializeReference, SubclassPicker]
        public CardKeyword keyword;
        public int count = 3;
        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            //ctx.Piles.Hand에서 count만큼 무작위 카드 선택
            var temp = ctx.Piles.Hand.ToList();
            int keywordCount = 0;
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i].Has(keyword)) keywordCount++;
            }
            for (int i = 0; i < count; )
            {
                if (temp.Count == 0 || keywordCount >= temp.Count) break;
                var card = temp[UnityEngine.Random.Range(0, temp.Count)];
                if (card.Has(keyword)) continue;
                await ctx.AddKeyword(card, keyword.Clone());
                temp.Remove(card);
                i++;
            }
        }
    }

    /// <summary>패에 있는 모든 카드에 키워드 부여. 이미 같은 키워드가 있는 카드는 건너뛴다.</summary>
    [Serializable]
    public class AllHandAddCardKeyword : Effect
    {
        [SerializeReference, SubclassPicker]
        public CardKeyword keyword;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            if (keyword == null) return;
            // 부여 도중 패가 바뀌어도 순회가 깨지지 않게 복사본으로 돈다
            foreach (var card in ctx.Piles.Hand.ToList())
                await ctx.AddKeyword(card, keyword);   // AddKeyword가 중복 검사와 복제를 맡는다
        }
    }
}
