using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [참고용 예시] 만보기: 한 턴에 steps칸 이상 움직이면 effects를 실행한다 (턴당 한 번).
    ///
    /// 단순한 유물("전투 시작 시 에너지 +1")은 코드 없이 RelicData 에셋에서 trigger + effects만 고른다.
    /// 조건이나 누적 상태가 필요할 때만 이렇게 두 클래스를 한 파일에 만든다:
    ///   PedometerRelicData — 에셋에 저장되는 설정값. 기획자가 조정하는 숫자를 필드로 둔다
    ///   PedometerRelic     — 런 동안 살아 있는 객체. 이벤트를 구독하고 상태(이번 턴 이동 수)를 가진다
    /// 파일 이름은 ScriptableObject 쪽 클래스 이름과 같아야 한다 (PedometerRelicData.cs). 다르면 에셋이 깨진다.
    ///
    /// 이 파일에서 볼 것:
    ///   1. CreateRelic 재정의 → 에셋이 자기 Relic 하위 클래스를 만든다
    ///   2. Bind에서 여러 이벤트 구독. 전투마다 ctx가 새로 만들어지므로 구독 해제는 필요 없다
    ///   3. 상태는 Relic 객체에 둔다 (에셋에 두면 모든 런이 공유한다).
    ///      Relic은 런 내내 유지되므로 전투마다 초기화할 값은 Bind에서 초기화한다
    ///   4. "무엇을 할지"는 에셋의 effects → Fire(ctx)로 실행 (드로우, 에너지 등 기획자가 선택)
    ///
    /// 설명문 예 (Strings.csv): 한 턴에 {steps}칸 이상 이동하면 카드를 {count}장 뽑습니다.
    ///   {steps} = 이 에셋의 필드, {count} = effects 안 DrawCardsEffect의 필드
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Pedometer", fileName = "Relic_Pedometer")]
    public class PedometerRelicData : RelicData
    {
        [Tooltip("한 턴에 이만큼 이동하면 발동. 순간이동도 1칸으로 센다")]
        [Min(1)] public int steps = 3;

        public override Relic CreateRelic() => new PedometerRelic(this);
    }

    public class PedometerRelic : Relic
    {
        readonly PedometerRelicData data;
        int movedThisTurn;
        bool firedThisTurn;

        public PedometerRelic(PedometerRelicData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx)
        {
            movedThisTurn = 0;
            firedThisTurn = false;
            ctx.Events.TurnStart.Add(OnTurnStart);
            ctx.Events.PlayerMoved.Add((from, to) => OnPlayerMoved(ctx));
        }

        /// <summary>기다릴 일이 없는 반응은 완료된 Awaitable을 돌려준다.</summary>
        Awaitable OnTurnStart()
        {
            movedThisTurn = 0;
            firedThisTurn = false;
            return AwaitableUtil.Completed();
        }

        async Awaitable OnPlayerMoved(BattleContext ctx)
        {
            movedThisTurn++;
            if (firedThisTurn || movedThisTurn < data.steps) return;

            // Fire 도중에 다시 이동 이벤트가 와도 두 번 발동하지 않게 먼저 표시한다
            firedThisTurn = true;
            ctx.Log($"{this}: 이번 턴 {movedThisTurn}칸 이동 → 발동");
            await Fire(ctx);
        }
    }
}
