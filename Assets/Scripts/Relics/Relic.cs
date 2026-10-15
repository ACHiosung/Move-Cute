using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    public enum RelicTrigger { BattleStart, TurnStart, TurnEnd, CardPlayed, PlayerMoved }

    [CreateAssetMenu(menuName = "MoveCute/Relic", fileName = "Relic_")]
    public class RelicData : ScriptableObject
    {
        public string id;
        public string nameKey;
        public string descKey;
        public Sprite icon;

        public RelicTrigger trigger;
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();
    }

    /// <summary>
    /// 유물: 전투 이벤트를 구독해 효과를 실행한다.
    /// 조건이 복잡한 유물은 이 클래스를 상속해 Bind를 재정의한다.
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

        public override string ToString() => Data != null ? Data.name : "Relic";
    }
}
