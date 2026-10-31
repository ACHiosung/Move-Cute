using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>보드 위 장애물 (폭탄, 묘비, 좀비 등). 칸을 막고 HP가 있다.</summary>
    public class Obstacle : Unit
    {
        public readonly ObstacleData Data;
        public Vector2Int Pos;

        public Obstacle(ObstacleData data, Vector2Int pos)
        {
            Data = data;
            Pos = pos;
            Hp = data.hp;
        }

        /// <summary>부서질 때 효과 (폭발 등). 대상은 장애물이 있던 칸.</summary>
        public Awaitable OnDestroyed(BattleContext ctx) =>
            Data.onDestroyed.ExecuteAll(ctx, new EffectSource(this), EffectTarget.OfTile(Pos));

        /// <summary>제한 턴의 마지막 턴이 끝났을 때 아직 남아 있으면 실행하는 효과.</summary>
        public Awaitable OnLastTurnEnd(BattleContext ctx) =>
            Data.onLastTurnEnd.ExecuteAll(ctx, new EffectSource(this), EffectTarget.OfTile(Pos));

        public override string DisplayName => Data != null ? Data.DisplayName : "Obstacle";
    }
}
