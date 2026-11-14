using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 클레이모어: 연계 폭탄의 피해 횟수가 bonus만큼 증가한다.
    /// 이벤트를 구독하지 않는다. BombExplosionEffect가 hitPerOtherObstacle(연계 폭탄)일 때 이 유물을 찾아 횟수에 더한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Chain Bomb Hit", fileName = "Relic_ChainBombHit")]
    public class ChainBombHitRelicData : RelicData
    {
        [Tooltip("연계 폭탄 피해 횟수에 더하는 값. 설명문의 {bonus}")]
        public int bonus = 2;

        public override Relic CreateRelic() => new ChainBombHitRelic(this);
    }

    public class ChainBombHitRelic : Relic
    {
        readonly ChainBombHitRelicData data;

        public ChainBombHitRelic(ChainBombHitRelicData data) : base(data) { this.data = data; }

        public int Bonus => data.bonus;

        public override void Bind(BattleContext ctx) { }
    }
}
