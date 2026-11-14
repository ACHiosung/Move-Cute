using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 상어 모양 대포: 폭탄(ObstacleData.isBomb)이 발동(파괴)될 때마다 effects를 실행한다 (카드 1장 뽑기).
    /// 폭탄은 파괴되면 항상 터지므로 ObstacleDestroyed 이벤트로 센다. 실행 시점은 폭발 피해 직전이다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Bomb Trigger", fileName = "Relic_BombTrigger")]
    public class BombTriggerRelicData : RelicData
    {
        public override Relic CreateRelic() => new BombTriggerRelic(this);
    }

    public class BombTriggerRelic : Relic
    {
        public BombTriggerRelic(BombTriggerRelicData data) : base(data) { }

        public override void Bind(BattleContext ctx)
        {
            ctx.Events.ObstacleDestroyed.Add(o => o.Data.isBomb ? Fire(ctx) : AwaitableUtil.Completed());
        }
    }
}
