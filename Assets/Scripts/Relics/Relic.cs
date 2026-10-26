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
            }
        }

        protected Awaitable Fire(BattleContext ctx) =>
            ctx.RunEffects(Data.effects, new EffectSource(this), EffectTarget.None);

        public override string ToString() => Data != null ? Data.DisplayName : "Relic";
    }
}
