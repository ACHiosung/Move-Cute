using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 잠금 카드의 해금 조건에서 뒤쪽 이동 moveCount개를 생략한다 (잠금 뒤 이동 생략 / 락픽).
    /// minMoves를 넣으면 이동이 그 수 이상인 잠금 카드에만 적용한다 (락픽: 이동 3번 이상인 카드의 마지막 이동 1개).
    /// 이벤트로 효과를 내지 않고 BattleContext.GetLockMoveRange가 찾아 읽는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Ignore Last Lock Moves", fileName = "Relic_IgnoreLastLockMoves")]
    public class IgnoreLastLockMovesRelicData : RelicData
    {
        [Tooltip("잠금 카드의 해금 조건에서 생략할 뒤쪽 이동 개수. 전부 생략되면 즉시 해금합니다")]
        [Min(0)] public int moveCount = 1;

        [Tooltip("카드의 이동(화살표)이 이 수 이상일 때만 적용. 0이면 모든 잠금 카드. 설명문의 {minMoves}")]
        [Min(0)] public int minMoves;

        public override Relic CreateRelic() => new IgnoreLastLockMovesRelic(this);
    }

    public class IgnoreLastLockMovesRelic : Relic
    {
        readonly IgnoreLastLockMovesRelicData data;
        public int MoveCount => Mathf.Max(0, data.moveCount);

        /// <summary>이 카드에 적용되는지 (카드 원래 이동 수가 minMoves 이상).</summary>
        public bool AppliesTo(CardInstance card) => card.Moves.Count >= data.minMoves;

        public IgnoreLastLockMovesRelic(IgnoreLastLockMovesRelicData data) : base(data)
        {
            this.data = data;
        }

        public override void Bind(BattleContext ctx) =>
            ctx.Events.BattleStart.Add(ctx.UnlockCompletedLockCards);
    }
}
