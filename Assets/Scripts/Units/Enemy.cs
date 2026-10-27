using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>적의 한 턴 행동(= 예고). 턴 시작에 Enemy.PlanIntent가 만들고, 공개된 뒤 그대로 실행된다.</summary>
    [Serializable]
    public class IntentData
    {
        [Tooltip("타일에 거는 효과")]
        public List<TilePlacement> tileEffects = new();

        [Tooltip("타일과 무관하게 턴 종료 시 실행 (묘비 배치, 즉시 피해 등)")]
        [SerializeReference, SubclassPicker] public List<Effect> turnEndActions = new();

        public IntentData AddTile(Vector2Int pos, TileTrigger trigger, params Effect[] effects)
        {
            tileEffects.Add(new TilePlacement { pos = pos, trigger = trigger, effects = new List<Effect>(effects) });
            return this;
        }

        public IntentData AddTurnEnd(params Effect[] effects)
        {
            turnEndActions.AddRange(effects);
            return this;
        }
    }

    [Serializable]
    public class TilePlacement
    {
        public Vector2Int pos;
        public TileTrigger trigger = TileTrigger.TurnEndStanding;
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();
    }

    /// <summary>
    /// 전투 중인 적 한 명. 적은 보드 타일 위에 존재하지 않는다.
    /// 적마다 상속해서 PlanIntent(이번 턴에 뭘 할지)를 구현한다. 예: Enemies/GravekeeperData.cs
    /// </summary>
    public abstract class Enemy : Unit
    {
        public readonly EnemyData Data;
        /// <summary>이번 턴 예고. UI가 이걸 읽어 표시한다.</summary>
        public IntentData CurrentIntent { get; private set; }

        protected Enemy(EnemyData data)
        {
            Data = data;
            Hp = data.startMorale;
        }

        public override string DisplayName => Data != null ? Data.DisplayName : "Enemy";

        /// <summary>
        /// 이번 턴 예고를 정한다. 보드·플레이어·턴 수 등을 읽어 판단만 하고 상태는 바꾸지 않는다
        /// (타일 배치는 DeclareIntent, 실행은 타일 발동과 OnTurnEnd가 한다). 아무것도 안 하면 null.
        /// </summary>
        protected abstract IntentData PlanIntent(BattleContext ctx);

        /// <summary>전투 시작 시 한 번. 이벤트에 반응하는 적은 여기서 ctx.Events를 구독한다 (유물 Bind와 같음).</summary>
        public virtual void Bind(BattleContext ctx) { }

        /// <summary>턴 시작: 지난 예고를 지우고 PlanIntent로 이번 예고를 정해 타일에 배치.</summary>
        public virtual async Awaitable DeclareIntent(BattleContext ctx)
        {
            await ctx.RemoveTileEffects(this);
            CurrentIntent = PlanIntent(ctx);
            if (CurrentIntent == null) return;

            foreach (var p in CurrentIntent.tileEffects)
                await ctx.AddTileEffect(p.pos, new TileEffect(p.trigger, p.effects, this));
        }

        /// <summary>턴 종료: 타일 외 행동 실행.</summary>
        public virtual async Awaitable OnTurnEnd(BattleContext ctx)
        {
            if (CurrentIntent == null) return;
            await CurrentIntent.turnEndActions.ExecuteAll(ctx, new EffectSource(this), EffectTarget.None);
        }
    }
}
