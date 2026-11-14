using System;
using System.Linq;
using UnityEngine;

// 주저 키워드 카드 효과. 주저 배율 계산은 BattleContext.HesitationMultiplier, 의욕 상실 반응은 BattleContext.AfterStatusAdded.
namespace MoveCute
{
    /// <summary>
    /// 대상(없으면 무작위 적)에게 주저 부여 (웅크리기, 애교부리기, PTSD).
    ///   부여량 = hesitation + perBomb × 보드의 폭탄 수 (ObstacleData.isBomb, 모든 종류)
    /// </summary>
    [Serializable]
    public class AddHesitationEffect : Effect
    {
        [Tooltip("기본 부여량. 설명문의 {hesitation}")]
        public int hesitation = 2;

        [Tooltip("보드에 있는 폭탄 하나마다 부여량에 더하는 값 (PTSD 2). 0이면 폭탄 수와 무관")]
        public int perBomb;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit == null) return;
            int amount = hesitation + perBomb * ctx.Obstacles.Count(o => o.Data.isBomb) + ctx.CardHesitationBonus(src);
            if (amount > 0) await ctx.AddStatus(src, unit, StatusIds.Hesitation, amount);
        }
    }

    /// <summary>
    /// 대상(없으면 무작위 적)에게 피해. 대상의 주저 수치 1마다 피해량이 perHesitation 증가 (애원).
    /// 증가분은 합연산이라 주저 배율보다 먼저 더해진다.
    /// </summary>
    [Serializable]
    public class DamagePerHesitationEffect : Effect
    {
        [Tooltip("기본 피해. 설명문의 {damage}")]
        public int damage = 2;

        [Tooltip("대상의 주저 수치 1마다 더하는 피해. 설명문의 {perHesitation}")]
        public int perHesitation = 2;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            var unit = target.Unit ?? ctx.GetRandomEnemy();
            if (unit == null) return;
            int stacks = ctx.GetStatusStacks(unit, StatusIds.Hesitation);
            await ctx.DealDamage(src, unit, new Damage(damage).Add(perHesitation * stacks));
        }
    }

    /// <summary>
    /// MOVE CUTE: 플레이어의 MOVE CUTE 버프 수치를 amount만큼 올린다 (기본 1, 여러 장 쓰면 쌓임).
    /// 이번 전투 동안 한 칸 이동할 때마다 무작위 적에게 주저를 버프 수치만큼 부여한다. 순간이동도 1칸으로 센다.
    /// 카드 자신의 이동은 버프가 붙기 전이라 세지 않는다. 주저 부여의 주체는 카드가 아니라 버프라 인공 눈물이 적용되지 않는다.
    /// </summary>
    [Serializable]
    public class MoveCuteBuffEffect : Effect
    {
        [Tooltip("버프 수치 증가량 (= 이동 1칸당 주저 부여량 증가)")]
        public int amount = 1;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            bool first = ctx.GetStatusStacks(ctx.Player, StatusIds.MoveCute) == 0;
            await ctx.AddStatus(src, ctx.Player, StatusIds.MoveCute, amount);
            if (!first) return;   // 이동 반응은 전투당 한 번만 등록한다. 수치는 반응 시점에 읽는다
            ctx.Events.PlayerMoved.Add(async (from, to) =>
            {
                if (ctx.IsOver) return;
                var buff = ctx.Player.Statuses.Find(s => s.Id == StatusIds.MoveCute);
                if (buff == null || buff.Stacks <= 0) return;
                var enemy = ctx.GetRandomEnemy();
                if (enemy != null) await ctx.AddStatus(new EffectSource(buff), enemy, StatusIds.Hesitation, buff.Stacks);
            });
        }
    }

    /// <summary>
    /// 의욕 상실: 플레이어의 의욕 상실 버프 수치를 amount만큼 올린다 (여러 장 쓰면 쌓임).
    /// 실제 피해는 적에게 주저가 붙을 때 BattleContext.AfterStatusAdded가 준다 (부여량 × 수치).
    /// </summary>
    [Serializable]
    public class DemotivationBuffEffect : Effect
    {
        [Tooltip("버프 수치 증가량")]
        public int amount = 1;

        public override Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target) =>
            ctx.AddStatus(src, ctx.Player, StatusIds.Demotivation, amount);
    }

    /// <summary>
    /// 주저 효과 증가: 플레이어의 주저 효과 증가 버프 수치를 amount만큼 올린다 (여러 장 쓰면 쌓임).
    /// 버프 수치 1당 주저 효과 +20%. 계산은 BattleContext.HesitationMultiplier.
    /// </summary>
    [Serializable]
    public class HesitationBoostBuffEffect : Effect
    {
        [Tooltip("버프 수치 증가량. 수치 1당 주저 효과 +20%")]
        public int amount = 1;

        public override Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target) =>
            ctx.AddStatus(src, ctx.Player, StatusIds.HesitationBoost, amount);
    }
}
