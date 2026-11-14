using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 모든 폭탄의 기본 피해량을 bonus만큼 올리는 유물의 원본 데이터.
    /// 이벤트를 구독하지 않는다. 폭발할 때 BombExplosionEffect가 장착한 이 유물을 찾아 bonus를 더한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Bomb Damage", fileName = "Relic_BombDamage")]
    public class BombDamageRelicData : RelicData
    {
        [Tooltip("폭탄 기본 피해량에 더하는 값 (배율보다 먼저 더해진다)")]
        public int bonus = 2;

        public override Relic CreateRelic() => new BombDamageRelic(this);

        [Tooltip("비워 두면 모든 폭탄. 넣으면 이 장애물 종류의 폭탄에만 적용 (폭탄용 거푸집: 기본 폭탄)")]
        public ObstacleData onlyObstacle;
    }

    public class BombDamageRelic : Relic
    {
        readonly BombDamageRelicData data;

        public BombDamageRelic(BombDamageRelicData data) : base(data) { this.data = data; }

        public int Bonus => data.bonus;

        public override void Bind(BattleContext ctx) { }

        /// <summary>이 폭탄(장애물 데이터)에 보너스가 붙는지. 종류를 모르면(null) 제한 없는 유물만 적용.</summary>
        public bool AppliesTo(ObstacleData bomb) => data.onlyObstacle == null || data.onlyObstacle == bomb;
    }
}
