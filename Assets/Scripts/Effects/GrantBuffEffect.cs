using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 플레이어에게 버프를 건다 (카운터, 노력의 결실). 버프는 유물 에셋 형식으로 Assets/Data/Buffs에 둔다.
    /// 설명문에서 버프 에셋의 값은 점으로 이어 쓴다: {buff.damage}.
    /// </summary>
    [Serializable]
    public class GrantBuffEffect : Effect
    {
        [Tooltip("걸 버프 (유물 에셋 형식, Assets/Data/Buffs)")]
        public RelicData buff;

        [Tooltip("붙일 수치. 설명문의 {stacks}")]
        [Min(1)] public int stacks = 1;

        [Tooltip("켜면 다시 걸 때 수치가 쌓인다. 끄면 이미 걸려 있을 때 아무 일도 없다")]
        public bool stackable = true;

        public override Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target) =>
            ctx.GrantBuff(src, buff, stacks, stackable);
    }
}
