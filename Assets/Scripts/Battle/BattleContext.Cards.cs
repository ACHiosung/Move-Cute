using UnityEngine;

namespace MoveCute
{
    /// <summary>상호작용 계약 — 카드 (담당: 카드/덱). 함수 이름·인자는 합의 없이 바꾸지 않는다.</summary>
    public partial class BattleContext
    {
        /// <summary>n장 드로우. 뽑을 카드가 없으면 거기서 멈춘다.</summary>
        public async Awaitable DrawCards(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (IsOver) return;
                var card = Piles.DrawOne();
                if (card == null) break;
                await Presenter.CardDrawn(card);
            }
        }

        public async Awaitable DiscardCard(CardInstance card)
        {
            if (IsOver || !Piles.Hand.Remove(card)) return;
            Piles.Discard.Add(card);
            await Presenter.CardDiscarded(card);
        }

        /// <summary>휴지통으로 (삭제).</summary>
        public async Awaitable TrashCard(CardInstance card)
        {
            if (IsOver) return;
            Piles.Hand.Remove(card);
            Piles.Draw.Remove(card);
            Piles.Discard.Remove(card);
            Piles.Trash.Add(card);
            await Presenter.CardTrashed(card);
        }

        public async Awaitable AddKeyword(CardInstance card, Keyword kw)
        {
            if (IsOver || card.Has(kw)) return;
            card.Keywords.Add(kw);
            await Presenter.CardChanged(card);
        }

        public async Awaitable GainEnergy(int n)
        {
            if (IsOver) return;
            Energy += n;
            await Presenter.EnergyChanged(Energy);
        }

        /// <summary>에너지 지불. 0 이하면 아무것도 하지 않는다. 부족한지는 호출하는 쪽에서 확인한다.</summary>
        public async Awaitable SpendEnergy(int n)
        {
            if (IsOver || n <= 0) return;
            Energy -= n;
            await Presenter.EnergyChanged(Energy);
        }

        /// <summary>카드 해결 시작: 손패에서 뺀다. 해결이 끝나면 SendToDiscard / TrashCard로 보낸다.</summary>
        public async Awaitable BeginCardPlay(CardInstance card, bool fullyMoved)
        {
            if (IsOver) return;
            Piles.Hand.Remove(card);
            Log($"카드 사용: {card}{(fullyMoved ? "" : " (흑백)")}");
            await Presenter.CardPlayed(card, fullyMoved);
        }

        /// <summary>카드 재사용 요청, 재사용 이벤트를 발생시킨다. 실제 카드 재사용은 카드 내에서 진행한다.</summary>
        public async Awaitable<bool> RequestReuseEvent(CardInstance card, int count)
        {
            if (IsOver || PlayingCard != card) return false;
            Log($"{card} 재사용 ({count}회째)");
            await Events.CardReused.Raise(card, count);   // 유물·상태 반응까지 끝나고 돌아옴
            return true;
        }

        /// <summary>어디에 있든 묘지로 (사용이 끝난 카드 등). 손패의 카드를 버릴 때는 DiscardCard.</summary>
        public async Awaitable SendToDiscard(CardInstance card)
        {
            if (IsOver) return;
            Piles.Hand.Remove(card);
            Piles.Draw.Remove(card);
            Piles.Discard.Remove(card);
            Piles.Discard.Add(card);
            await Presenter.CardDiscarded(card);
        }
    }
}
