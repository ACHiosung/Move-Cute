using UnityEngine;

namespace MoveCute
{
    [CreateAssetMenu(menuName = "MoveCute/Relic/Ignore First Lock Moves", fileName = "Relic_IgnoreFirstLockMoves")]
    public class IgnoreFirstLockMovesRelicData : RelicData
    {
        [Tooltip("잠금 카드의 해금 조건에서 생략할 앞쪽 이동 개수. 전부 생략되면 즉시 해금합니다")]
        [Min(0)] public int moveCount = 1;

        public override Relic CreateRelic() => new IgnoreFirstLockMovesRelic(this);
    }

    public class IgnoreFirstLockMovesRelic : Relic
    {
        readonly IgnoreFirstLockMovesRelicData data;
        public int MoveCount => Mathf.Max(0, data.moveCount);

        public IgnoreFirstLockMovesRelic(IgnoreFirstLockMovesRelicData data) : base(data)
        {
            this.data = data;
        }

        public override void Bind(BattleContext ctx) =>
            ctx.Events.BattleStart.Add(ctx.UnlockCompletedLockCards);
    }
}
