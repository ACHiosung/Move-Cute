using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [참고용 예시] 충격파: 플레이어 주변 range칸 안의 장애물마다 damage 피해를 주고,
    /// 그중 부순 장애물 하나당 onBreak 효과를 한 번씩 실행한다.
    ///
    /// 새 효과를 만들 때 이 파일에서 볼 것:
    ///   1. 숫자는 public 필드 → 인스펙터에서 조정하고, 설명문의 {range} {damage}에 자동으로 들어간다
    ///   2. 다른 효과를 담는 필드([SerializeReference, SubclassPicker] List&lt;Effect&gt;) → "무엇을 할지"는 기획자가 조합
    ///   3. 대상 목록은 복사(ToList)해 두고 돈다 → await하는 동안 원본 목록이 바뀔 수 있다
    ///   4. ctx 함수는 하나씩 await → 반환되면 연출·연쇄 반응(파괴 시 효과 등)까지 끝나 있다
    ///   5. await 뒤에는 상황이 바뀌었을 수 있으니 다시 확인하고 다음 행동을 정한다
    ///
    /// 설명문 예 (Strings.csv):
    ///   주변 {range}칸의 장애물에 피해 {damage}. 장애물을 부술 때마다 적에게 피해 {onBreak.damage}.
    ///
    /// 주의: 필드에 전투 중 상태(누적 횟수 등)를 저장하지 않는다.
    /// 효과 객체는 카드 에셋에 하나뿐이라 같은 카드의 모든 복사본·모든 전투가 공유한다. 상태는 지역 변수에 둔다.
    /// </summary>
    [Serializable]
    public class ShockwaveEffect : Effect
    {
        [Tooltip("플레이어로부터 몇 칸 안까지 (상하좌우 걸음 수)")]
        [Min(1)] public int range = 1;

        [Tooltip("범위 안 장애물마다 주는 피해")]
        public int damage = 3;

        [Tooltip("장애물을 하나 부술 때마다 실행할 효과. 대상은 이 효과가 받은 대상을 그대로 넘긴다")]
        [SerializeReference, SubclassPicker] public List<Effect> onBreak = new();

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            // 대상을 먼저 확정해 복사해 둔다. 장애물이 부서지면 ctx.Obstacles에서 빠지므로
            // 원본 목록을 돌면서 await하면 목록이 도중에 바뀐다.
            var center = ctx.Player.Pos;
            var hits = ctx.Obstacles
                .Where(o => Distance(o.Pos, center) <= range)
                .OrderBy(o => Distance(o.Pos, center))
                .ToList();

            int broken = 0;
            foreach (var o in hits)
            {
                // 앞 장애물의 파괴 시 효과(폭발 등)로 이미 부서졌으면 건너뛴다
                if (!ctx.Obstacles.Contains(o)) continue;

                // 반환되면 피해 연출, 피해 이벤트(유물 반응), 파괴와 파괴 시 효과까지 모두 끝나 있다
                await ctx.DealDamage(src, o, damage);
                if (o.Hp <= 0) broken++;
            }

            // 부순 개수만큼 하위 효과 실행. 전투가 끝나면 RunEffects가 알아서 멈춘다
            for (int i = 0; i < broken; i++)
                await ctx.RunEffects(onBreak, src, target);
        }

        static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
