using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 유물: 전투 이벤트를 구독해 효과를 실행한다. 런 동안 유지되고, 전투마다 Bind가 다시 호출된다.
    /// 조건이 복잡한 유물은 이 클래스를 상속해 Bind를 재정의한다 (RelicData.CreateRelic 참고).
    /// </summary>
    public class Relic
    {
        public readonly RelicData Data;
        public Relic(RelicData data) { Data = data; }

        /// <summary>꺼져 있으면 Fire가 아무것도 하지 않는다. 효과를 읽기만 하는 유물은 읽는 쪽이 확인한다 (예: BombDamageRelic).</summary>
        public bool Enabled = true;

        public virtual void Bind(BattleContext ctx)
        {
            var ev = ctx.Events;
            switch (Data.trigger)
            {
                case RelicTrigger.BattleStart: ev.BattleStart.Add(() => Fire(ctx)); break;
                case RelicTrigger.TurnStart: ev.TurnStart.Add(() => Fire(ctx)); break;
                case RelicTrigger.TurnEnd: ev.TurnEnd.Add(() => Fire(ctx)); break;
                case RelicTrigger.CardPlayed: ev.CardPlayed.Add(_ => Fire(ctx)); break;
                case RelicTrigger.PlayerMoved: ev.PlayerMoved.Add((_, _) => Fire(ctx)); break;
                case RelicTrigger.LastTurnEnd: ev.LastTurnEnd.Add(() => Fire(ctx)); break;
            }
        }

        protected Awaitable Fire(BattleContext ctx) =>
            Enabled ? Data.effects.ExecuteAll(ctx, new EffectSource(this), EffectTarget.None) : AwaitableUtil.Completed();

        public override string ToString() => Data != null ? Data.DisplayName : "Relic";
    }
}
