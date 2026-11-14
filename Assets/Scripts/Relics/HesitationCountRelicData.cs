using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 채찍: 적에게 부여한 주저를 누적해서 threshold에 도달할 때마다 effects를 실행한다 (카드 1장 뽑기).
    /// 누적은 런 동안 유지된다. 0에서 시작해 threshold 이상이 되면 threshold를 빼고 발동 (예: 8에서 3 부여 → 1, 1번 발동).
    /// 한 번에 많이 부여하면 그만큼 여러 번 발동한다. 출처와 무관하게 센다 (버프로 부여한 주저 포함).
    /// 부여 직후 BattleContext.AfterStatusAdded가 OnHesitationApplied를 부른다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Hesitation Count", fileName = "Relic_HesitationCount")]
    public class HesitationCountRelicData : RelicData
    {
        [Tooltip("이만큼 주저를 부여할 때마다 발동. 설명문의 {threshold}")]
        [Min(1)] public int threshold = 10;

        public override Relic CreateRelic() => new HesitationCountRelic(this);
    }

    public class HesitationCountRelic : Relic
    {
        readonly HesitationCountRelicData data;

        /// <summary>지금까지 쌓인 주저 (0 ~ threshold-1). 유물 아이콘 숫자 표시에 쓸 수 있다.</summary>
        public int Counter { get; private set; }

        public HesitationCountRelic(HesitationCountRelicData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx) { }

        public async Awaitable OnHesitationApplied(BattleContext ctx, int amount)
        {
            if (!Enabled || amount <= 0) return;
            Counter += amount;
            while (Counter >= data.threshold && !ctx.IsOver)
            {
                Counter -= data.threshold;
                ctx.Log($"{this}: 주저 {data.threshold} 누적 → 발동 (남은 {Counter})");
                await Fire(ctx);
            }
        }
    }
}
