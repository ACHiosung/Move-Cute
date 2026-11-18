using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 수상한 송곳니: 전투에서 이긴 뒤 받는 HP 회복 보상이 percent%만큼 늘어난다 (원래 보상 × 1.1, 소수점 내림).
    /// 이벤트를 구독하지 않는다. BattleContext.PrepareRewards가 켜진 이 유물의 percent를 더해 회복 보상에 곱한다.
    /// 회복 보상이 작으면 내림 때문에 늘어나지 않을 수 있다 (2 × 1.1 = 2.2 → 2).
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Reward Healing Bonus", fileName = "Relic_RewardHealingBonus")]
    public class RewardHealingBonusRelicData : RelicData
    {
        [Tooltip("회복 보상을 몇 % 늘릴지. 설명문의 {percent}")]
        [Min(0)] public int percent = 10;

        public override Relic CreateRelic() => new RewardHealingBonusRelic(this);
    }

    public class RewardHealingBonusRelic : Relic
    {
        readonly RewardHealingBonusRelicData data;

        public RewardHealingBonusRelic(RewardHealingBonusRelicData data) : base(data) { this.data = data; }

        public int Percent => Mathf.Max(0, data.percent);

        public override void Bind(BattleContext ctx) { }
    }
}
