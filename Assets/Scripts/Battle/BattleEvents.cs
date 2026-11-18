using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 전투 트리거 허브. 유물·상태·키워드가 구독한다.
    /// 발생(Raise)은 BattleContext만 한다. 구독한 반응은 등록 순서대로 하나씩 끝까지 await된다.
    /// 필요한 이벤트는 여기에 추가. UI 갱신용이 아니다 — UI는 IBattlePresenter로 받는다.
    /// </summary>
    public class BattleEvents
    {
        public readonly BattleEvent BattleStart = new();
        public readonly BattleEvent TurnStart = new();
        public readonly BattleEvent TurnEnd = new();
        public readonly BattleEvent LastTurnEnd = new();                            // 마지막 턴 종료, 장애물(폭탄) 처리 직전
        public readonly BattleEvent<CardInstance> CardPlayed = new();
        public readonly BattleEvent<CardInstance, int> CardReused = new();
        // 카드의 이동·효과·재사용이 모두 끝난 직후, 묘지/휴지통으로 가기 전 (카드, 낼 때 고른 대상, 재사용 횟수)
        public readonly BattleEvent<CardInstance, EffectTarget, int> CardResolved = new();
        public readonly BattleEvent<CardInstance> CardTrashed = new();             // 휴지통으로 감 (삭제 키워드·효과로 보내기 모두)
        public readonly BattleEvent<CardInstance> CardUnlocked = new();            // 해금됨 (이동·만능열쇠·유물·해금 부여 모두. SetLockState에서)
        public readonly BattleEvent<Vector2Int, Vector2Int> PlayerMoved = new();   // from, to
        public readonly BattleEvent<Unit, int> Damaged = new();
        public readonly BattleEvent<Obstacle> ObstacleDestroyed = new();
        public readonly BattleEvent<bool> BattleEnd = new();                        // won
    }

    /// <summary>await되는 이벤트. 구독: ev.Add(() => 반응(ctx)) — 반응은 Awaitable을 반환한다.</summary>
    public class BattleEvent
    {
        readonly List<Func<Awaitable>> handlers = new();

        public void Add(Func<Awaitable> handler) => handlers.Add(handler);
        public void Remove(Func<Awaitable> handler) => handlers.Remove(handler);

        internal async Awaitable Raise()
        {
            foreach (var h in handlers.ToArray()) await h();
        }
    }

    public class BattleEvent<T>
    {
        readonly List<Func<T, Awaitable>> handlers = new();

        public void Add(Func<T, Awaitable> handler) => handlers.Add(handler);
        public void Remove(Func<T, Awaitable> handler) => handlers.Remove(handler);

        internal async Awaitable Raise(T arg)
        {
            foreach (var h in handlers.ToArray()) await h(arg);
        }
    }

    public class BattleEvent<T1, T2>
    {
        readonly List<Func<T1, T2, Awaitable>> handlers = new();

        public void Add(Func<T1, T2, Awaitable> handler) => handlers.Add(handler);
        public void Remove(Func<T1, T2, Awaitable> handler) => handlers.Remove(handler);

        internal async Awaitable Raise(T1 arg1, T2 arg2)
        {
            foreach (var h in handlers.ToArray()) await h(arg1, arg2);
        }
    }

    public class BattleEvent<T1, T2, T3>
    {
        readonly List<Func<T1, T2, T3, Awaitable>> handlers = new();

        public void Add(Func<T1, T2, T3, Awaitable> handler) => handlers.Add(handler);
        public void Remove(Func<T1, T2, T3, Awaitable> handler) => handlers.Remove(handler);

        internal async Awaitable Raise(T1 arg1, T2 arg2, T3 arg3)
        {
            foreach (var h in handlers.ToArray()) await h(arg1, arg2, arg3);
        }
    }
}
