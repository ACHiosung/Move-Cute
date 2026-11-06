using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>잠금/해금 전투 상태. 진행도는 턴·카드 더미가 바뀌어도 이 전투 안에서 유지한다.</summary>
    public partial class BattleContext
    {
        readonly Dictionary<CardInstance, List<Direction>> lockMovements = new();

        /// <summary>키워드의 상태를 잠금으로 바꾼다. 이미 잠겨 있으면 진행도를 유지한다.</summary>
        public Awaitable LockCard(CardInstance card) => SetLockState(card, false);

        /// <summary>같은 키워드의 상태를 해금으로 바꾼다. 자동 잠금 태그는 유지한다.</summary>
        public Awaitable UnlockCard(CardInstance card) => SetLockState(card, true);

        /// <summary>기존 공개 상태 전환 계약을 유지한다. 자동 잠금 설정은 바꾸지 않는다.</summary>
        public Awaitable SetLockState(CardInstance card, bool isUnlocked) =>
            SetLockState(card, isUnlocked, null);

        async Awaitable SetLockState(CardInstance card, bool isUnlocked, bool? autoLock)
        {
            if (IsOver || card == null) return;
            var keyword = card.GetKeyword<Keyword_Lock>();
            bool added = keyword == null;
            bool changed = added;
            if (keyword == null)
            {
                keyword = new Keyword_Lock();
                card.Keywords.Add(keyword);
            }
            bool stateChanged = keyword.isUnlocked != isUnlocked;
            changed |= stateChanged;
            keyword.isUnlocked = isUnlocked;
            if (autoLock.HasValue && keyword.autoLock != autoLock.Value)
            {
                keyword.autoLock = autoLock.Value;
                changed = true;
            }
            if (added || stateChanged || isUnlocked) lockMovements.Remove(card);
            card.IsGrayed = card.IsLocked || !card.CanPlayFully(this);
            if (changed) await Presenter.CardChanged(card);
        }

        // MovePlayer 성공 시에만 호출한다. HP를 소모하는 Teleport는 이 경로를 지나지 않는다.
        async Awaitable TrackLockMovement(Direction direction)
        {
            var cards = Piles.Draw.Concat(Piles.Hand).Concat(Piles.Discard);
            if (PlayingCard != null) cards = cards.Append(PlayingCard);
            foreach (var card in cards.Distinct().Where(c => c.IsLocked).ToList())
            {
                if (IsOver) return;
                // 이동이 없는 잠금 카드는 이동으로 해금하지 않는다. 명시적인 해금 부여는 가능하다.
                if (card.Moves.Count == 0) continue;
                if (!lockMovements.TryGetValue(card, out var history))
                    lockMovements[card] = history = new List<Direction>();
                history.Add(direction);
                if (history.Count > card.Moves.Count)
                    history.RemoveRange(0, history.Count - card.Moves.Count);
                // 최근 N칸을 비교하므로 불일치 뒤 겹치는 접두사도 놓치지 않는다 (←←→ 등).
                if (history.Count == card.Moves.Count && history.SequenceEqual(card.Moves))
                    await UnlockCard(card);
            }
        }
    }
}
