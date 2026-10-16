using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    [CreateAssetMenu(menuName = "MoveCute/Enemy", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string nameKey;
        public int startMorale = 20;
        public Sprite sprite;

        [Tooltip("턴마다 순서대로 사용. 끝나면 처음부터 반복. 좌표는 보드 크기에 맞춰 설계")]
        public List<IntentData> pattern = new();
    }

    /// <summary>적의 한 턴 행동(= 예고). 턴 시작에 공개된다.</summary>
    [Serializable]
    public class IntentData
    {
        [Tooltip("타일에 거는 효과")]
        public List<TilePlacement> tileEffects = new();

        [Tooltip("타일과 무관하게 턴 종료 시 실행 (묘비 배치, 즉시 피해 등)")]
        [SerializeReference, SubclassPicker] public List<Effect> turnEndActions = new();
    }

    [Serializable]
    public class TilePlacement
    {
        public Vector2Int pos;
        public TileTrigger trigger = TileTrigger.TurnEndStanding;
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();
    }

    /// <summary>전투 중인 적 한 명. 적은 보드 타일 위에 존재하지 않는다.</summary>
    public class Enemy : Unit
    {
        public readonly EnemyData Data;
        /// <summary>이번 턴 예고. UI가 이걸 읽어 표시한다.</summary>
        public IntentData CurrentIntent { get; private set; }

        public Enemy(EnemyData data)
        {
            Data = data;
            Hp = data.startMorale;
        }

        public override string DisplayName => Data != null ? Data.name : "Enemy";

        /// <summary>턴 시작: 지난 예고를 지우고 이번 예고를 타일에 배치.</summary>
        public virtual async Awaitable DeclareIntent(BattleContext ctx)
        {
            await ctx.RemoveTileEffects(this);
            CurrentIntent = null;
            if (Data.pattern.Count == 0) return;

            CurrentIntent = Data.pattern[(ctx.Turn - 1) % Data.pattern.Count];
            foreach (var p in CurrentIntent.tileEffects)
                await ctx.AddTileEffect(p.pos, new TileEffect(p.trigger, p.effects, this));
        }

        /// <summary>턴 종료: 타일 외 행동 실행.</summary>
        public virtual async Awaitable OnTurnEnd(BattleContext ctx)
        {
            if (CurrentIntent == null) return;
            await ctx.RunEffects(CurrentIntent.turnEndActions, new EffectSource(this), EffectTarget.None);
        }
    }
}
