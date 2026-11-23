using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [TestingTools] 테스트 도구 전용 BattleContext 함수. 에디터 창(Testing Tools, Deck Builder)만 부른다.
    /// 이 파일은 테스트 도구 폴더에 있어서 폴더째 지우면 같이 사라진다 (README.md).
    /// </summary>
    public partial class BattleContext
    {
        /// <summary>플레이 중 켠 테스트 옵션을 지금 상태에 반영한다 (에너지 무한: 최대 에너지로 채움).</summary>
        public async Awaitable ApplyTestOptions()
        {
            if (IsOver || !BattleTestOptions.InfiniteEnergy || Energy >= MaxEnergy) return;
            Energy = MaxEnergy;
            RefreshPlayable();
            await Presenter.EnergyChanged(Energy);
        }

        /// <summary>카드 한 장을 새로 만들어 손패에 넣는다 (이번 전투에만, 런 덱에는 넣지 않는다).</summary>
        public async Awaitable TestAddCardToHand(CardData data)
        {
            if (IsOver || data == null) return;
            var card = data.CreateCard();
            Piles.Hand.Add(card);
            RefreshPlayable();
            Log($"[테스트] {card}을(를) 손패에 넣음");
            await Presenter.CardDrawn(card);
        }

        /// <summary>카드 한 장을 새로 만들어 뽑을 카드 더미 맨 위(다음에 뽑힐 자리)에 넣는다 (이번 전투에만).</summary>
        public async Awaitable TestAddCardToDrawTop(CardData data)
        {
            if (IsOver || data == null) return;
            var card = data.CreateCard();
            Piles.Draw.Add(card);   // DrawOne은 맨 뒤에서 뽑는다
            Log($"[테스트] {card}을(를) 뽑을 카드 맨 위에 넣음");
            // 더미 수를 알리는 연출 함수가 따로 없어서, 에너지를 그대로 다시 알려 화면 수치(뽑을 카드 수 포함)를 갱신한다
            await Presenter.EnergyChanged(Energy);
        }
    }
}
