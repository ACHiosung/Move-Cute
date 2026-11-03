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
    public class Keyword_Lock : CardKeyword
    {
        [Tooltip("false: 잠금(사용 불가), true: 해금(이동 없이 무료 사용)")]
        public bool isUnlocked;

        // 기존 UI의 이름·설명 키(Lock/Unlock)를 현재 상태에 맞게 선택한다.
        public override string ToString() => isUnlocked ? "Unlock" : "Lock";

        [Tooltip("자동 잠금 여부를 나타내는 설정값. 재잠금 동작은 별도로 연결한다")]
        public bool autoLock;

        [NonSerialized] public int count;
        [NonSerialized] public int startIndex; // 포함
        [NonSerialized] public int endIndex;   // 제외

        /// <summary>새 카드/전투 복사본/잠금 부여 시의 초기값. 이동 비교는 아직 하지 않는다.</summary>
        public void ResetProgress(int moveCount)
        {
            count = 0;
            startIndex = 0;
            endIndex = moveCount;
        }
    }

}
