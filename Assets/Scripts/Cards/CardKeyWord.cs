using System;
using UnityEngine;

namespace MoveCute
{
    [Serializable]
    public class CardKeyword
    {

    }

    public class Keyword_Dash : CardKeyword
    {
        [Tooltip("최초 사용을 제외한 추가 재사용 상한. 5이면 총 최대 6회 발동")]
        [Min(0)]
        public int maxReuseCount = 5;
    }
    public class Keyword_Delete : CardKeyword { }
    public class Keyword_Free : CardKeyword { }
    public class Keyword_Retain : CardKeyword { }
}
