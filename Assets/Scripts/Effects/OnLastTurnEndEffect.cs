using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// effects를 이번 전투의 마지막 턴 종료 때 실행하도록 예약한다 (자폭 드론 카드).
    /// 실행 시점은 마지막 턴 장애물 처리(폭탄 폭발) 직전이라, 여기서 놓은 폭탄도 같이 터진다 (BattleEvents.LastTurnEnd).
    /// 여러 번 쓰면 그만큼 예약된다. 예약은 이 전투에만 유효하다.
    /// </summary>
    [Serializable]
    public class OnLastTurnEndEffect : Effect
    {
        [Tooltip("마지막 턴 종료 시 실행할 효과. 대상 없음 (예: PlaceObstacleEffect → 무작위 빈 칸)")]
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();

        public override Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            ctx.Events.LastTurnEnd.Add(() => effects.ExecuteAll(ctx, src, EffectTarget.None));
            return AwaitableUtil.Completed();
        }
    }
}
