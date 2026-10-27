using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    public enum RelicTrigger { BattleStart, TurnStart, TurnEnd, CardPlayed, PlayerMoved }

    /// <summary>
    /// 유물 원본. 단순한 유물은 이 에셋에서 trigger + effects만 고르면 된다 (코드 없음).
    /// 조건·누적 상태가 필요한 유물은 이 클래스와 Relic을 함께 상속한다 (예: Relics/PedometerRelicData.cs).
    /// ScriptableObject라서 클래스 이름과 파일 이름이 같아야 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Relic/Basic", fileName = "Relic_")]
    public class RelicData : ScriptableObject
    {
        public string id;
        public string nameKey;
        public string descKey;
        public Sprite icon;

        [Tooltip("이 시점마다 effects 실행. 특수 유물(하위 클래스)은 쓰지 않는다")]
        public RelicTrigger trigger;
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();

        /// <summary>런에서 쓸 Relic 객체를 만든다. 특수 유물은 재정의해서 자기 Relic 하위 클래스를 돌려준다.</summary>
        public virtual Relic CreateRelic() => new Relic(this);

        /// <summary>현재 언어의 이름. 다국어 표에 없으면 에셋 이름.</summary>
        public string DisplayName => Loc.Get(nameKey, name);

        /// <summary>현재 언어의 설명. {이름}은 이 에셋(하위 클래스 포함)과 effects의 같은 이름 필드 값으로 바뀐다.</summary>
        public string Description => Loc.Format(descKey, new object[] { this }.Concat(effects), "");
    }
}
