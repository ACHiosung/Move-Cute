using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 질주 카드의 최대 재사용 횟수를 설정하는 클래스.
    /// </summary>
    [Serializable]
    public class DashCard : CardInstance
    {
        [Tooltip("최초 사용을 제외한 추가 재사용 상한. 5이면 총 최대 6회 발동")]
        [Min(0)]
        public int maxReuseCount = 5;

        // 설정값이므로 실행 중 감소시키지 않는다.
        // 실제 재사용 횟수는 PM의 실행 로직에서 별도로 센다.
        //
        // Play와 CanPlayFully는 재정의하지 않는다.
        // 현재는 기본 CardInstance의 실행·이동 판정을 사용하며,
        // PM이 질주 처리를 연결하기 전까지 자동 재사용되지 않는다.
    }
}