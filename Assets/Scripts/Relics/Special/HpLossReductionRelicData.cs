using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 두꺼운 패딩: 플레이어가 HP를 잃을 때 reducePercent%만큼 덜 잃는다. 감소량은 소수점 내림 (15 → 1.5를 버려 1 → 14).
    /// 이벤트를 구독하지 않는다. BattleContext.LosePlayerHp가 켜진 IPlayerHpLossModifier 유물의 곱을 모아 적용한다.
    /// 순간이동 HP 비용에도 적용할지는 includeTeleport.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/HP Loss Reduction", fileName = "Relic_HpLossReduction")]
    public class HpLossReductionRelicData : RelicData
    {
        [Tooltip("잃는 HP를 몇 % 줄일지. 설명문의 {reducePercent}")]
        [Range(0, 100)] public int reducePercent = 10;

        [Tooltip("순간이동 HP 비용에도 적용")]
        public bool includeTeleport = true;

        public override Relic CreateRelic() => new HpLossReductionRelic(this);
    }

    public class HpLossReductionRelic : Relic, IPlayerHpLossModifier
    {
        readonly HpLossReductionRelicData data;

        public HpLossReductionRelic(HpLossReductionRelicData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx) { }

        public decimal HpLossMultiplier(PlayerHpLossKind kind) =>
            kind == PlayerHpLossKind.Teleport && !data.includeTeleport ? 1m : 1m - data.reducePercent / 100m;
    }
}
