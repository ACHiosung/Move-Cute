using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 카드가 재사용될 때 effects를 실행하는 유물의 원본 데이터.
    /// 피해량은 유물 에셋의 effects에서 설정한다.
    /// </summary>
    [CreateAssetMenu(
        menuName = "MoveCute/Relic/Reuse Damage",
        fileName = "Relic_ReuseDamage")]
    public class ReuseDamageRelicData : RelicData
    {
        public override Relic CreateRelic()
            => new ReuseDamageRelic(this);
    }

    /// <summary>
    /// 전투에서 재사용 이벤트를 구독하고 유물 효과를 실행한다.
    /// 최초 카드 사용에는 반응하지 않는다.
    /// </summary>
    public class ReuseDamageRelic : Relic
    {
        public ReuseDamageRelic(ReuseDamageRelicData data)
            : base(data)
        {
        }

        public override void Bind(BattleContext ctx)
        {
            // 재사용 이벤트가 발생할 때마다 효과를 실행한다.
            // Fire가 반환한 Awaitable을 이벤트 시스템이 기다린다.
            ctx.Events.CardReused.Add(
                (card, reuseCount) => Fire(ctx));
        }
    }
}