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

        /// <summary>현재 언어의 이름. 다국어 표에 없으면 에셋 이름.</summary>
        public string DisplayName => Loc.Get(nameKey, name);
    }
}
