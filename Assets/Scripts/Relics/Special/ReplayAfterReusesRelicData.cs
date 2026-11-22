using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 이온 음료: 카드의 재사용이 모두 끝났을 때 재사용이 minReuses번 이상이었으면 그 카드의 효과를 한 번 더 낸다.
    /// (재사용 2번 이상 = 3번 사용 → 4번째는 이동 없이 효과만). 카드가 묘지/휴지통으로 가기 전에 발동한다.
    /// 효과는 BattleContext.ReplayCardEffects로 낸다: 에너지·더미 이동·재사용 이벤트 없이 card.Play를 다시 부르므로
    /// effects와 behavior(특수 카드)의 효과가 모두 발동하고, 카드 자신의 이동은 막힌다. 대상은 낼 때 고른 대상 그대로.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Replay After Reuses", fileName = "Relic_ReplayAfterReuses")]
    public class ReplayAfterReusesRelicData : RelicData
    {
        [Tooltip("재사용이 이 횟수 이상이면 발동. 설명문의 {minReuses}")]
        [Min(1)] public int minReuses = 2;

        public override Relic CreateRelic() => new ReplayAfterReusesRelic(this);
    }

    public class ReplayAfterReusesRelic : Relic
    {
        readonly ReplayAfterReusesRelicData data;

        public ReplayAfterReusesRelic(ReplayAfterReusesRelicData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx) =>
            ctx.Events.CardResolved.Add((card, target, reuses) =>
                Enabled && reuses >= data.minReuses
                    ? ctx.ReplayCardEffects(new EffectSource(this), card, target)
                    : AwaitableUtil.Completed());
    }
}
