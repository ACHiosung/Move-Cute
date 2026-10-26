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

        public override string DisplayName => Data != null ? Data.DisplayName : "Obstacle";
    }
}
