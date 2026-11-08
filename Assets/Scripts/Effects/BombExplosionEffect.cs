using System;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 폭탄 폭발 피해. 폭탄 장애물의 onDestroyed에 넣는다 (폭탄·연계 폭탄·예술 폭탄 공용).
    ///   기본 피해 = damage + 장착해서 켜 둔 폭탄 피해 유물(BombDamageRelic)의 bonus 합
    ///   다른 장애물 수 k = 지금 보드에 남은 장애물 수. onDestroyed에서는 자신과 앞서 터진 폭탄이 이미 빠져 있다
    ///   피해량 = 기본 피해 × multiplierPerOtherObstacle^k (공격 측 곱). 소수점은 피격 측 곱(주저 등)까지 곱한 뒤
    ///   BattleContext.CalculateDamage에서 한 번만 내림 (예술 폭탄 1.5, 주저 없음: 2 → 3 → 4 → 6)
    ///   횟수 = 1 + (hitPerOtherObstacle면 k) (연계 폭탄)
    /// k는 실행 시작 때 한 번 센다. 대상이 없으면 매 번 무작위 적.
    /// </summary>
    [Serializable]
    public class BombExplosionEffect : Effect
    {
        [Tooltip("기본 피해량. 폭탄 피해 유물의 bonus가 여기에 더해진다")]
        public int damage = 2;

        [Tooltip("켜면 남은 다른 장애물 하나마다 피해를 한 번 더 준다 (연계 폭탄)")]
        public bool hitPerOtherObstacle;

        [Tooltip("남은 다른 장애물 하나마다 피해량에 곱한다. 1이면 그대로 (예술 폭탄 1.5)")]
        [Min(1)] public float multiplierPerOtherObstacle = 1f;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            int others = ctx.Obstacles.Count;
            Damage amount = Amount(ctx, others);
            int hits = 1 + (hitPerOtherObstacle ? others : 0);
            for (int i = 0; i < hits; i++)
            {
                var unit = target.Unit ?? ctx.GetRandomEnemy();
                if (unit != null) await ctx.DealDamage(src, unit, amount);
            }
        }

        Damage Amount(BattleContext ctx, int others)
        {
            int baseDamage = damage + ctx.Run.Relics.OfType<BombDamageRelic>().Where(r => r.Enabled).Sum(r => r.Bonus);
            var amount = new Damage(baseDamage);
            for (int i = 0; i < others; i++) amount = amount.Multiply((decimal)multiplierPerOtherObstacle);
            return amount;
        }
    }
}
