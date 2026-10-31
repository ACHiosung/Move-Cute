using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// effects를 (baseCount + 지금 보드에 있는 장애물 수)번 실행한다. 연계 폭탄의 폭발에 쓴다.
    /// 장애물의 onDestroyed에서 쓰면 자신은 이미 보드에서 빠진 뒤라 "다른 장애물 수"가 되고,
    /// 앞서 터진 폭탄도 이미 빠져 있으므로 세지 않는다.
    /// 횟수는 실행 시작 때 한 번 센다 (반복 도중 장애물이 바뀌어도 그대로).
    /// </summary>
    [Serializable]
    public class RepeatPerObstacleEffect : Effect
    {
        [Tooltip("장애물이 하나도 없을 때의 실행 횟수")]
        [Min(0)] public int baseCount = 1;

        [Tooltip("반복할 효과. 대상은 이 효과가 받은 대상을 그대로 넘긴다")]
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            int count = baseCount + ctx.Obstacles.Count;
            for (int i = 0; i < count; i++)
                await effects.ExecuteAll(ctx, src, target);
        }
    }
}
