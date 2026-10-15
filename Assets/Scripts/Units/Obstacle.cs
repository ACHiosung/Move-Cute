using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    [CreateAssetMenu(menuName = "MoveCute/Obstacle", fileName = "Obstacle_")]
    public class ObstacleData : ScriptableObject
    {
        public string id;
        public string nameKey;
        public int hp = 5;
        public Sprite sprite;

        [Tooltip("HP가 0이 되어 파괴될 때 (폭탄 폭발 등)")]
        [SerializeReference, SubclassPicker] public List<Effect> onDestroyed = new();

        [Tooltip("마지막 턴 종료 시 (폭탄은 남아 있으면 터짐)")]
        [SerializeReference, SubclassPicker] public List<Effect> onLastTurnEnd = new();
    }

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

        public override string DisplayName => Data != null ? Data.name : "Obstacle";
    }
}
