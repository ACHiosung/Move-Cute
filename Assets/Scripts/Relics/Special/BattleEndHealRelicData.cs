using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 자가수리키트: 전투에서 이기면 최대 HP의 percent%만큼 회복한다 (소수점 내림, 최대 HP 50의 3% = 1).
    /// 회복은 BattleEnd 이벤트에서 BattleContext.HealAfterBattle로 한다 (전투 후 보상 회복과 별개).
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Battle End Heal", fileName = "Relic_BattleEndHeal")]
    public class BattleEndHealRelicData : RelicData
    {
        [Tooltip("최대 HP의 몇 %를 회복할지. 설명문의 {percent}")]
        [Min(0)] public int percent = 3;

        public override Relic CreateRelic() => new BattleEndHealRelic(this);
    }

    public class BattleEndHealRelic : Relic
    {
        readonly BattleEndHealRelicData data;

        public BattleEndHealRelic(BattleEndHealRelicData data) : base(data) { this.data = data; }

        public override void Bind(BattleContext ctx) =>
            ctx.Events.BattleEnd.Add(won =>
            {
                if (!Enabled || !won) return AwaitableUtil.Completed();
                int amount = ctx.Run.MaxHp * data.percent / 100;   // 정수 나눗셈 = 내림
                return ctx.HealAfterBattle(new EffectSource(this), amount);
            });
    }
}
