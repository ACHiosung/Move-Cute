using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 기계 심장: 처음으로 치명적인 HP 감소(HP가 0 이하가 됨)를 받으면 HP leaveHp를 남기고 버틴다.
    /// 기본은 런에서 한 번 (resetEachBattle을 켜면 전투마다 한 번). 순간이동 비용으로 죽을 때도 버틸지는 includeTeleport.
    /// 계산은 BattleContext.LosePlayerHp가 합·곱 감소(두꺼운 패딩 등)를 모두 적용한 뒤 마지막에 묻는다.
    /// 발동 여부(Used)는 아이콘 흑백 표시에 쓸 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Lethal Guard", fileName = "Relic_LethalGuard")]
    public class LethalGuardRelicData : RelicData
    {
        [Tooltip("버틸 때 남길 HP. 설명문의 {leaveHp}")]
        [Min(1)] public int leaveHp = 1;

        [Tooltip("켜면 전투마다 다시 쓸 수 있다. 끄면 런에서 한 번")]
        public bool resetEachBattle;

        [Tooltip("순간이동 HP 비용으로 HP가 0 이하가 될 때도 버틴다")]
        public bool includeTeleport = true;

        public override Relic CreateRelic() => new LethalGuardRelic(this);
    }

    public class LethalGuardRelic : Relic, IPlayerLethalGuard
    {
        readonly LethalGuardRelicData data;

        /// <summary>이미 발동했는지. 런 동안 유지 (resetEachBattle이면 전투 시작마다 초기화).</summary>
        public bool Used { get; private set; }

        /// <summary>발동하고 나면 전투 화면에서 흑백.</summary>
        public override bool Spent => Used;

        public LethalGuardRelic(LethalGuardRelicData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx)
        {
            if (data.resetEachBattle) Used = false;
        }

        public bool TrySurvive(PlayerHpLossKind kind, out int leaveHp)
        {
            leaveHp = data.leaveHp;
            if (Used || kind == PlayerHpLossKind.Teleport && !data.includeTeleport) return false;
            Used = true;
            return true;
        }
    }
}
