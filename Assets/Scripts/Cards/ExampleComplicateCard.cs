using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [참고용 예시] 이동 없이 충격파를 repeat번 발동하는 카드. 이동 판정(흑백)도 하지 않는다.
    ///
    /// 특수 카드 = CardInstance 하위 클래스 하나. 카드 에셋(CardData)의 behavior 드롭다운에서 고른다.
    /// 에셋 타입은 CardData 그대로라 [CreateAssetMenu]가 필요 없고, 파일 이름도 클래스 이름과 달라도 된다.
    ///
    /// 이 파일에서 볼 것:
    ///   1. public 필드 → 카드 에셋 인스펙터의 behavior 아래에서 조정하고, 설명문의 {repeat} {shockwave.damage}에 들어간다
    ///   2. CanPlayFully 재정의 → 흑백 판정을 바꾼다. 이 카드는 이동이 없으므로 항상 전부 발동
    ///   3. Play 재정의 → 에너지 지불 → BeginCardPlay → 카드가 하는 일 → Finish 순서는 지킨다
    ///   4. 에셋의 moves·effects는 이 카드가 쓰지 않는다. 쓰고 싶으면 Play에서 직접 부른다 (Data.effects.ExecuteAll)
    ///   5. 전투 중 상태가 필요하면 [NonSerialized] 필드로 둔다. 카드 한 장마다 복사되므로 다른 카드와 공유되지 않는다
    ///
    /// 설명문 예 (Strings.csv):
    ///   주변 {shockwave.range}칸 안의 장애물에 피해를 {shockwave.damage} 줍니다. {repeat}번 반복합니다.
    /// </summary>
    [Serializable]
    public class ExampleComplicateCard : CardInstance
    {
        [Tooltip("충격파 발동 횟수")]
        [Min(1)] public int repeat = 2;

        [Tooltip("발동할 충격파. 범위·피해·장애물을 부술 때 효과를 여기서 조정한다")]
        public ShockwaveEffect shockwave = new();//효과를 기존에 쓰던 List에 추가한 것이 아닌 별도 필드에 둔 사례, 기존 List에 넣어도 될 것

        public override bool CanPlayFully(BattleContext ctx) => true;

        public override async Awaitable Play(BattleContext ctx, EffectTarget target)
        {
            var src = new EffectSource(this);

            for (int i = 0; i < repeat; i++)
            {
                if (ctx.IsOver) break; // 게임이 끝났으면 반복 중단
                //await Data.effects.ExecuteAll(ctx, src, target); //기존 List에 넣었을 때
                await shockwave.Execute(ctx, src, target); // 별도 필드에 둔 경우
            }

            await Finish(ctx);//카드 묘지, 휴지통으로 버리기
        }
    }
}
