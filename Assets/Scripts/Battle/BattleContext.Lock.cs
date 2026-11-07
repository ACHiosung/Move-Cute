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
            bool wasUnlocked = keyword.isUnlocked;
            keyword.isUnlocked = isUnlocked;
            if (autoLock.HasValue && keyword.autoLock != autoLock.Value)
            {
                keyword.autoLock = autoLock.Value;
                changed = true;
            }
            // 잠금 부여·자동 재잠금 시에도 유물로 남은 이동이 없으면 즉시 해금한다.
            if (CanUnlockWithoutMovement(card)) keyword.isUnlocked = true;
            bool stateChanged = wasUnlocked != keyword.isUnlocked;
            changed |= stateChanged;
            if (added || stateChanged || keyword.isUnlocked) lockMovements.Remove(card);
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
                if (CanUnlockWithoutMovement(card))
                {
                    await UnlockCard(card);
                    continue;
                }
                // 이동이 없는 잠금 카드는 이동으로 해금하지 않는다. 명시적인 해금 부여는 가능하다.
                if (card.Moves.Count == 0) continue;
                GetLockMoveRange(card, out int start, out int count);
                if (!lockMovements.TryGetValue(card, out var history))
                    lockMovements[card] = history = new List<Direction>();
                history.Add(direction);
                if (history.Count > count)
                    history.RemoveRange(0, history.Count - count);
                // 최근 N칸을 비교하므로 불일치 뒤 겹치는 접두사도 놓치지 않는다 (←←→ 등).
                if (history.Count == count && history.SequenceEqual(card.Moves.Skip(start).Take(count)))
                    await UnlockCard(card);
            }
        }

        /// <summary>판정과 UI가 공유하는 잠금 이동 구간. 원본을 유지하며 모두 생략되면 count는 0이다.</summary>
        public void GetLockMoveRange(CardInstance card, out int start, out int count)
        {
            start = 0;
            count = card.Moves.Count;
            if (!card.IsLocked || count == 0) return;
            // 꺼진 유물(Relic.Enabled, TempRelicBar)은 읽는 쪽에서 뺀다
            long first = Run.Relics.OfType<IgnoreFirstLockMovesRelic>().Where(r => r.Enabled).Sum(r => (long)r.MoveCount);
            long last = Run.Relics.OfType<IgnoreLastLockMovesRelic>().Where(r => r.Enabled).Sum(r => (long)r.MoveCount);
            start = (int)System.Math.Min(first, card.Moves.Count);
            int endSkip = (int)System.Math.Min(last, card.Moves.Count - start);
            count = card.Moves.Count - start - endSkip;
        }

        /// <summary>유물로 해금 조건이 사라진 카드를 이동 없이 해금한다. 전투 시작/유물 획득 시 호출한다.</summary>
        public async Awaitable UnlockCompletedLockCards()
        {
            if (IsOver) return;
            var cards = Piles.Draw.Concat(Piles.Hand).Concat(Piles.Discard);
            if (PlayingCard != null) cards = cards.Append(PlayingCard);
            foreach (var card in cards.Distinct().Where(CanUnlockWithoutMovement).ToList())
                await UnlockCard(card);
        }

        bool CanUnlockWithoutMovement(CardInstance card)
        {
            if (!card.IsLocked) return false;
            // 원래 이동이 없는 카드의 기존 동작은 유물의 유효한 생략 효과가 있을 때만 바꾼다.
            bool hasReduction = Run.Relics.Any(r => r.Enabled && (
                r is IgnoreFirstLockMovesRelic first && first.MoveCount > 0 ||
                r is IgnoreLastLockMovesRelic last && last.MoveCount > 0));
            if (!hasReduction) return false;
            GetLockMoveRange(card, out _, out int count);
            return count == 0;
        }
    }
}
