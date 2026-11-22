using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 제트팩: 순간이동이 HP를 소모하지 않는다 (에너지는 그대로 1 소모).
    /// 이벤트를 구독하지 않는다. BattleContext.TeleportHpCost가 켜진 이 유물을 찾아 비용을 0으로 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Free Teleport", fileName = "Relic_FreeTeleport")]
    public class FreeTeleportRelicData : RelicData
    {
        public override Relic CreateRelic() => new FreeTeleportRelic(this);
    }

    public class FreeTeleportRelic : Relic
    {
        public FreeTeleportRelic(FreeTeleportRelicData data) : base(data) { }

        public override void Bind(BattleContext ctx) { }
    }
}
