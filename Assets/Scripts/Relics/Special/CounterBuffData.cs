using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 카운터 버프: 턴 종료 때 적의 예고가 내가 서 있는 칸에서 발동하기 직전, 그 적에게 피해를 damage 준다.
    /// 수치(Relic.Stacks) = 쌓인 카운터 수. 반격 한 번에 수치를 모두 쓰고 damage × 수치만큼 피해를 준 뒤 버프가 사라진다
    /// (수치 0 → 버프 기본 규칙). 여러 적이 내 칸을 노리면 예고 순서로 처음 노린 적에게만 반격한다.
    /// 반격 뒤 적 행동은 그대로 진행된다.
    /// 이벤트 BattleEvents.EnemyActsOnPlayerTile을 구독한다. 카드에서는 GrantBuffEffect로 건다 (카운터 카드).
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Buff/Counter", fileName = "Buff_Counter")]
    public class CounterBuffData : RelicData
    {
        [Tooltip("반격 피해. 카드 설명문의 {buff.damage}")]
        public int damage = 20;

        public override Relic CreateRelic() => new CounterBuff(this);
    }

    public class CounterBuff : Relic
    {
        readonly CounterBuffData data;

        public CounterBuff(CounterBuffData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx) =>
            ctx.Events.EnemyActsOnPlayerTile.Add(enemy => Counter(ctx, enemy));

        async Awaitable Counter(BattleContext ctx, Enemy enemy)
        {
            if (!Enabled || Stacks <= 0 || enemy == null) return;
            var src = new EffectSource(this);
            int stacks = Stacks;
            ctx.Log($"{this}: {enemy}에게 반격 (수치 {stacks} 모두 사용, 피해 {data.damage} × {stacks})");
            // 수치부터 모두 쓴다. 여기서 버프가 사라지지만 이번 반격은 그대로 한다
            await ctx.AddBuffStacks(src, this, -stacks);
            await ctx.DealDamage(src, enemy, data.damage * stacks);
        }
    }
}
