using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// trigger가 threshold번 일어날 때마다 effects를 실행하는 유물 (코드 없이 에셋만으로 만드는 누적 유물).
    /// 예: 캐스트 퍼즐 = 카드 해금(CardUnlocked) 2번마다 1장 뽑기, 독수리 깃털 = 카드 재사용(CardReused) 5번마다 에너지 1.
    /// 누적은 런 동안 유지된다 (채찍과 같음). 0에서 시작해 threshold에 닿으면 threshold를 빼고 발동.
    /// 발동할 때의 효과 대상은 threshold번째로 일어난 일의 대상이다 (카드 시점이면 그 카드).
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Counting", fileName = "Relic_Counting")]
    public class CountingRelicData : RelicData
    {
        [Tooltip("trigger가 이만큼 일어날 때마다 발동. 설명문의 {threshold}")]
        [Min(1)] public int threshold = 2;

        [Tooltip("아이콘 숫자: 켜면 발동까지 남은 수 (5 → 0), 끄면 쌓인 수 (0 → 5)")]
        public bool showRemaining = true;

        public override Relic CreateRelic() => new CountingRelic(this);
    }

    public class CountingRelic : Relic, ICountingRelic
    {
        readonly CountingRelicData data;

        /// <summary>지금까지 센 횟수 (0 ~ threshold-1).</summary>
        public int Counter { get; private set; }
        public int Threshold => Mathf.Max(1, data.threshold);
        public int DisplayValue => data.showRemaining ? Threshold - Counter : Counter;

        public CountingRelic(CountingRelicData data) : base(data) { this.data = data; }

        protected override async Awaitable OnTriggered(BattleContext ctx, EffectTarget target)
        {
            if (!Enabled) return;
            Counter++;
            if (Counter < Threshold) return;
            Counter -= Threshold;
            ctx.Log($"{this}: {Data.trigger} {Threshold}번 → 발동");
            await Fire(ctx, target);
        }
    }
}
