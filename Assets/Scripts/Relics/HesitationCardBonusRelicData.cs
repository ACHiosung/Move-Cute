using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 인공 눈물: 카드 효과로 주저를 부여할 때 부여량이 bonus만큼 증가한다.
    /// 이벤트를 구독하지 않는다. AddHesitationEffect가 BattleContext.CardHesitationBonus로 이 유물을 찾아 더한다.
    /// MOVE CUTE처럼 버프가 부여하는 주저에는 적용되지 않는다 (출처가 카드일 때만).
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Hesitation Card Bonus", fileName = "Relic_HesitationCardBonus")]
    public class HesitationCardBonusRelicData : RelicData
    {
        [Tooltip("카드 효과로 주저를 부여할 때 더하는 양")]
        public int bonus = 1;

        public override Relic CreateRelic() => new HesitationCardBonusRelic(this);
    }

    public class HesitationCardBonusRelic : Relic
    {
        readonly HesitationCardBonusRelicData data;

        public HesitationCardBonusRelic(HesitationCardBonusRelicData data) : base(data) { this.data = data; }

        public int Bonus => data.bonus;

        public override void Bind(BattleContext ctx) { }
    }
}
