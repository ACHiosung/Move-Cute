using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 세계의 경계 버프: 이번 전투 동안
    ///   1) 보드 밖으로 나가는 한 칸 이동이 반대편 끝 칸으로 이어진다 (IEdgeWrapRule → ctx.StepTarget). 그 방향 한 칸 이동으로 친다.
    ///   2) 카드가 주는 피해가 percent% 줄어든다 (IDealtDamageModifier, 공격 측 곱). 폭탄(장애물)·버프·유물이 주는 피해는 그대로.
    /// 규칙을 바꾸는 버프라 이벤트를 구독하지 않고, 계산하는 쪽이 ctx.Modifiers로 묻는다.
    /// 카드는 GrantBuffEffect의 stackable을 꺼서, 여러 장 써도 한 번만 적용된다 (세계의 경계 카드).
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Buff/World's Edge", fileName = "Buff_WorldsEdge")]
    public class WorldsEdgeBuffData : RelicData
    {
        [Tooltip("카드가 주는 피해 감소량 (%). 설명문의 {percent}, 카드 설명문에서는 {buff.percent}")]
        [Range(0, 100)] public int percent = 20;

        public override Relic CreateRelic() => new WorldsEdgeBuff(this);
    }

    public class WorldsEdgeBuff : Relic, IEdgeWrapRule, IDealtDamageModifier
    {
        readonly WorldsEdgeBuffData data;

        public WorldsEdgeBuff(WorldsEdgeBuffData data) : base(data) { this.data = data; }

        // 반응할 시점이 없다 (규칙만 바꾼다)
        public override void Bind(BattleContext ctx) { }

        public decimal DealtDamageMultiplier(EffectSource src, Unit target) =>
            src.Owner is CardInstance && target is not PlayerUnit ? 1m - data.percent / 100m : 1m;
    }
}
