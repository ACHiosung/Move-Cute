using System;
using System.Linq;
using UnityEngine;

// 폭탄·장애물 설치 효과. 인스펙터 효과 목록에서 "BombEffects" 아래에 모인다.
namespace MoveCute
{
    /// <summary>
    /// 폭탄 폭발 피해. 폭탄 장애물의 onDestroyed에 넣는다 (폭탄·연계 폭탄·예술 폭탄 공용).
    ///   기본 피해 = damage + 장착해서 켜 둔 폭탄 피해 유물(BombDamageRelic)의 bonus 합
    ///             + 보드에 남은 장애물의 bombDamageBonus 합 (화약통 1개당 1)
    ///   다른 장애물 수 k = 지금 보드에 남은 장애물 수. onDestroyed에서는 자신과 앞서 터진 폭탄이 이미 빠져 있다
    ///   피해량 = 기본 피해 × multiplierPerOtherObstacle^k (공격 측 곱). 소수점은 피격 측 곱(주저 등)까지 곱한 뒤
    ///   BattleContext.CalculateDamage에서 한 번만 내림 (예술 폭탄 1.5, 주저 없음: 2 → 3 → 4 → 6)
    ///   횟수 = 1 + (hitPerOtherObstacle면 k + 클레이모어 bonus) (연계 폭탄)
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
            Damage amount = Amount(ctx, others, (src.Owner as Obstacle)?.Data);
            int hits = 1 + (hitPerOtherObstacle ? others + ChainHitBonus(ctx) : 0);
            for (int i = 0; i < hits; i++)
            {
                var unit = target.Unit ?? ctx.GetRandomEnemy();
                if (unit != null) await ctx.DealDamage(src, unit, amount);
            }
        }

        Damage Amount(BattleContext ctx, int others, ObstacleData bomb)
        {
            int baseDamage = damage + ctx.Run.Relics.OfType<BombDamageRelic>().Where(r => r.Enabled && r.AppliesTo(bomb)).Sum(r => r.Bonus)
                + ctx.Obstacles.Sum(o => o.Data.bombDamageBonus);
            var amount = new Damage(baseDamage);
            for (int i = 0; i < others; i++) amount = amount.Multiply((decimal)multiplierPerOtherObstacle);
            return amount;
        }

        /// <summary>연계 폭탄 피해 횟수에 더할 값: 켜 둔 클레이모어(ChainBombHitRelic)의 bonus 합</summary>
        static int ChainHitBonus(BattleContext ctx) =>
            ctx.Run.Relics.OfType<ChainBombHitRelic>().Where(r => r.Enabled).Sum(r => r.Bonus);
    }

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

    /// <summary>
    /// 플레이어에게서 거리 distance 이내의 폭탄(ObstacleData.isBomb)을 모두 터뜨린다 (폭탄 해체 카드).
    /// 거리는 이동 기준 (대각선 = 2). 순서는 마지막 턴 폭발과 같다 (Obstacle.InExplosionOrder).
    /// 앞의 폭발로 이미 부서진 폭탄은 건너뛴다.
    /// </summary>
    [Serializable]
    public class DetonateBombsInRangeEffect : Effect
    {
        [Tooltip("플레이어에게서 이 거리 이내의 폭탄을 터뜨린다. 설명문의 {distance}")]
        [Min(1)] public int distance = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var player = ctx.Player.Pos;
            var bombs = Obstacle.InExplosionOrder(
                ctx.Obstacles.Where(o => o.Data.isBomb && Board.Distance(o.Pos, player) <= distance), ctx.Board);
            foreach (var bomb in bombs)
                if (ctx.Obstacles.Contains(bomb)) await ctx.DestroyObstacle(src, bomb);
        }
    }
}
