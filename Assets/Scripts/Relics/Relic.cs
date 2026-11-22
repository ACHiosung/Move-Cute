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

        /// <summary>
        /// 꺼져 있으면 Fire가 아무것도 하지 않는다. 효과를 읽기만 하는 유물은 읽는 쪽이 확인한다 (예: BombDamageRelic).
        /// 사라진 버프도 꺼 둔다 (BattleContext.RemoveBuff). 그래서 Bind를 재정의한 반응도 Enabled부터 확인한다.
        /// </summary>
        public bool Enabled = true;

        /// <summary>한 번 쓰고 나서 더는 발동하지 않는 상태 (기계 심장). 전투 화면이 아이콘을 흑백으로 그린다.</summary>
        public virtual bool Spent => false;

        /// <summary>
        /// 버프로 걸렸을 때의 수치 (카운터: 남은 반격 횟수). 런 유물은 쓰지 않는다. BattleContext.GrantBuff 참고.
        /// 바꿀 때는 BattleContext.AddBuffStacks를 쓴다 (0이 되면 버프가 사라진다).
        /// </summary>
        public int Stacks { get; internal set; } = 1;

        /// <summary>
        /// 다시 걸면 수치가 쌓이는 버프인지 (GrantBuffEffect.stackable, 카운터). BattleContext.GrantBuff가 정한다.
        /// 전투 화면은 쌓이는 버프만 아이콘에 수치를 보여 준다 (세계의 경계처럼 늘 1인 버프는 숫자를 숨김).
        /// </summary>
        public bool Stackable { get; internal set; }

        /// <summary>버프 수치가 0이 되면 사라지는지. 기본은 사라진다. 0이어도 남아야 하는 버프만 false로 재정의한다.</summary>
        public virtual bool RemoveAtZeroStacks => true;

        public virtual void Bind(BattleContext ctx)
        {
            var ev = ctx.Events;
            var none = EffectTarget.None;
            switch (Data.trigger)
            {
                case RelicTrigger.BattleStart: ev.BattleStart.Add(() => OnTriggered(ctx, none)); break;
                case RelicTrigger.TurnStart: ev.TurnStart.Add(() => OnTriggered(ctx, none)); break;
                case RelicTrigger.TurnEnd: ev.TurnEnd.Add(() => OnTriggered(ctx, none)); break;
                case RelicTrigger.CardPlayed: ev.CardPlayed.Add(card => OnTriggered(ctx, EffectTarget.OfCard(card))); break;
                case RelicTrigger.PlayerMoved: ev.PlayerMoved.Add((_, _) => OnTriggered(ctx, none)); break;
                case RelicTrigger.LastTurnEnd: ev.LastTurnEnd.Add(() => OnTriggered(ctx, none)); break;
                case RelicTrigger.CardReused: ev.CardReused.Add((card, _) => OnTriggered(ctx, EffectTarget.OfCard(card))); break;
                case RelicTrigger.CardTrashed: ev.CardTrashed.Add(card => OnTriggered(ctx, EffectTarget.OfCard(card))); break;
                case RelicTrigger.CardUnlocked: ev.CardUnlocked.Add(card => OnTriggered(ctx, EffectTarget.OfCard(card))); break;
            }
        }

        /// <summary>trigger 시점마다 불린다. 기본은 effects 실행. 누적 유물(CountingRelic)은 재정의해서 세다가 발동한다.</summary>
        protected virtual Awaitable OnTriggered(BattleContext ctx, EffectTarget target) => Fire(ctx, target);

        protected Awaitable Fire(BattleContext ctx) => Fire(ctx, EffectTarget.None);

        protected Awaitable Fire(BattleContext ctx, EffectTarget target) =>
            Enabled ? Data.effects.ExecuteAll(ctx, new EffectSource(this), target) : AwaitableUtil.Completed();

        public override string ToString() => Data != null ? Data.DisplayName : "Relic";
    }

    /// <summary>
    /// 누적 수치가 있는 유물 (채찍, 독수리 깃털, 캐스트 퍼즐). Counter가 Threshold에 닿으면 발동하고 Threshold만큼 줄어든다.
    /// 디버그 창(RelicToggleWindow)은 "Counter/Threshold"로, 전투 화면은 아이콘 오른쪽 아래에 DisplayValue를 보여 준다.
    /// </summary>
    public interface ICountingRelic
    {
        int Counter { get; }
        int Threshold { get; }
        /// <summary>아이콘에 표시할 숫자. 기본은 쌓인 수 (채찍: 0에서 10까지). 남은 수로 보여 줄 유물은 재정의한다.</summary>
        int DisplayValue => Counter;
    }

    /// <summary>플레이어가 HP를 잃는 이유. 유물이 순간이동 비용을 따로 다룰 때 쓴다.</summary>
    public enum PlayerHpLossKind { Damage, Teleport }

    /// <summary>
    /// 플레이어가 잃는 HP를 바꾸는 유물 (두꺼운 패딩). BattleContext가 켜진 유물의 합과 곱을 따로 모아
    /// "(양 + 합) × 곱"으로 계산하므로 장착 순서와 무관하다. 곱으로 바뀐 양은 소수점을 버린다 (감소량 내림).
    /// </summary>
    public interface IPlayerHpLossModifier
    {
        /// <summary>잃는 양에 더할 값 (음수면 감소).</summary>
        int HpLossAdd(PlayerHpLossKind kind) => 0;
        /// <summary>잃는 양에 곱할 값 (0.9 = 10% 덜 잃음).</summary>
        decimal HpLossMultiplier(PlayerHpLossKind kind) => 1m;
    }

    /// <summary>
    /// 잃는 HP 때문에 HP가 0 이하가 될 때 버티게 하는 유물 (기계 심장). 합·곱 계산이 모두 끝난 뒤 마지막에 묻고, 하나만 발동한다.
    /// </summary>
    public interface IPlayerLethalGuard
    {
        /// <summary>버티면 true와 남길 HP(1 이상)를 돌려준다. 발동 횟수 제한은 유물이 관리한다.</summary>
        bool TrySurvive(PlayerHpLossKind kind, out int leaveHp);
    }

    /// <summary>
    /// 보드 밖으로 나가는 한 칸 이동을 반대편 끝 칸으로 잇는 규칙 (세계의 경계). 켜진 유물·버프 중 하나라도 있으면 적용된다.
    /// 다음 칸 위치는 BattleContext.StepTarget이 이 규칙을 보고 정한다.
    /// </summary>
    public interface IEdgeWrapRule { }

    /// <summary>
    /// 주는 피해에 곱할 배율 (세계의 경계: 카드 피해 ×0.8). BattleContext.CalculateDamage가 공격 측 곱으로 곱하고,
    /// 소수점은 피격 측 곱(주저 등)까지 곱한 뒤 마지막에 한 번만 버린다. 어떤 피해에 적용할지는 구현이 src·target으로 정한다.
    /// </summary>
    public interface IDealtDamageModifier
    {
        decimal DealtDamageMultiplier(EffectSource src, Unit target);
    }
}
