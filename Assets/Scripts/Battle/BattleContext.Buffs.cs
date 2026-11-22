using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 버프: 카드가 플레이어에게 거는 "이번 전투에만 있는 유물" + 수치(Relic.Stacks).
    /// 버프 정의는 유물 에셋(RelicData)을 그대로 쓰므로 유물의 시점·효과·누적·특수 유물 기능을 모두 쓸 수 있다.
    /// 반응(~할 때마다)은 버프가 Bind에서 이벤트를 구독한다. 수치가 0이 되면 바로 사라지고 (Relic.RemoveAtZeroStacks),
    /// 아니면 전투가 끝날 때 BattleContext와 함께 사라진다.
    /// 버프 에셋은 Assets/Data/Buffs, 카드에서는 GrantBuffEffect로 건다.
    /// </summary>
    public partial class BattleContext
    {
        /// <summary>이번 전투에 걸린 버프. 건 순서대로 (같은 시점에 반응하면 이 순서, 런 유물보다 뒤).</summary>
        public readonly List<Relic> Buffs = new();

        /// <summary>
        /// 플레이어에게 버프를 건다. 처음이면 만들어서 이벤트를 구독(Bind)하고,
        /// 이미 있으면 stackable일 때만 수치를 더한다 (아니면 아무 일도 없음).
        /// </summary>
        public Awaitable GrantBuff(EffectSource src, RelicData data, int stacks = 1, bool stackable = true)
        {
            if (IsOver || data == null || stacks <= 0) return AwaitableUtil.Completed();
            var buff = GetBuff(data);
            if (buff != null)
            {
                if (stackable) buff.Stacks += stacks;
                Log(stackable ? $"{src}: 버프 {buff} +{stacks} → {buff.Stacks}" : $"{src}: 버프 {buff}는 이미 있음");
                return AwaitableUtil.Completed();
            }
            buff = data.CreateRelic();
            buff.Stacks = stacks;
            Buffs.Add(buff);
            buff.Bind(this);
            Log($"{src}: 버프 {buff} ({stacks})");
            return AwaitableUtil.Completed();
        }

        /// <summary>
        /// 버프 수치를 amount만큼 바꾼다 (카운터: 반격할 때마다 -1). 0 아래로는 내려가지 않고,
        /// 0이 되면 버프가 사라진다 (Relic.RemoveAtZeroStacks를 false로 재정의한 버프는 남는다).
        /// </summary>
        public Awaitable AddBuffStacks(EffectSource src, Relic buff, int amount)
        {
            if (buff == null || amount == 0 || !Buffs.Contains(buff)) return AwaitableUtil.Completed();
            buff.Stacks = Mathf.Max(0, buff.Stacks + amount);
            if (buff.Stacks == 0 && buff.RemoveAtZeroStacks) return RemoveBuff(src, buff);
            return AwaitableUtil.Completed();
        }

        /// <summary>
        /// 버프를 없앤다. Bind에서 구독한 반응은 이벤트에 남지만 버프를 꺼 두므로(Relic.Enabled) 더는 일어나지 않는다
        /// (모든 유물 반응은 Enabled부터 확인한다). 같은 버프를 다시 걸면 새로 만든다.
        /// </summary>
        public Awaitable RemoveBuff(EffectSource src, Relic buff)
        {
            if (buff == null || !Buffs.Remove(buff)) return AwaitableUtil.Completed();
            buff.Enabled = false;
            Log($"{src}: 버프 {buff} 사라짐");
            return AwaitableUtil.Completed();
        }

        /// <summary>이 에셋으로 건 버프. 없으면 null.</summary>
        public Relic GetBuff(RelicData data) => Buffs.Find(b => b.Data == data);

        /// <summary>
        /// 규칙을 바꾸는 유물·버프: 켜진 런 유물 다음 버프 순서로, 인터페이스 T를 가진 것.
        /// 이동 판정·피해 계산처럼 이벤트로 할 수 없는 규칙은 계산하는 쪽이 이것으로 묻는다 (IEdgeWrapRule, IDealtDamageModifier).
        /// </summary>
        public IEnumerable<T> Modifiers<T>() where T : class =>
            Run.Relics.Concat(Buffs).Where(r => r.Enabled).OfType<T>();
    }
}
