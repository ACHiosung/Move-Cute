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

        public async Awaitable AddKeyword(CardInstance card, CardKeyword kw)
        {
            if (IsOver || card == null || kw == null || card.Has(kw)) return;


            var copy = kw.Clone(); // 에셋과 카드 사이에 키워드 객체를 공유하지 않는다.
            if (copy is Keyword_Lock locked) locked.ResetProgress(card.Moves.Count);
            card.Keywords.Add(copy);
            card.IsGrayed = card.IsLocked || !card.CanPlayFully(this);
            await Presenter.CardChanged(card);
        }

        /// <summary>기존 잠금 키워드 상태를 전환하고 UI를 갱신한다. 키워드와 원래 이동·비용은 보존한다.</summary>
        public async Awaitable SetLockState(CardInstance card, bool isUnlocked)
        {
            if (IsOver || card == null) return;
            var state = card.GetKeyword<Keyword_Lock>();
            if (state == null || state.isUnlocked == isUnlocked) return;

            state.isUnlocked = isUnlocked;
            if (!isUnlocked) state.ResetProgress(card.Moves.Count);
            card.IsGrayed = card.IsLocked || !card.CanPlayFully(this);
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
