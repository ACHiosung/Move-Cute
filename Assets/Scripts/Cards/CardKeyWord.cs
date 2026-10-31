using System;
using UnityEngine;

namespace MoveCute
{
    [Serializable]
    public class CardKeyword
    {
        /// <summary>enum 시절 이름과 같다 (Keyword_Dash → "Dash"). UI 문구 키와 정렬에 쓴다.</summary>
        public override string ToString() => GetType().Name.Replace("Keyword_", "");

        /// <summary>카드마다 따로 들 복사본. 에셋의 키워드 객체를 카드끼리 공유하지 않게 한다. 참조 필드가 생기면 재정의한다.</summary>
        public virtual CardKeyword Clone() => (CardKeyword)MemberwiseClone();
    }
    [Serializable]
    public class Keyword_Dash : CardKeyword
    {
        [Tooltip("최초 사용을 제외한 추가 재사용 상한. 5이면 총 최대 6회 발동")]
        [Min(0)]
        public int maxReuseCount = 5;
    }
    [Serializable]
    public class Keyword_Delete : CardKeyword { }
    [Serializable]
    public class Keyword_Free : CardKeyword { }
    [Serializable]
    public class Keyword_Retain : CardKeyword { }
    [Serializable]
    public class Keyword_Lock : CardKeyword { }
}
