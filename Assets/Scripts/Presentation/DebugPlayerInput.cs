using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 디버그 GUI용 선택 입력. 후보가 하나면 바로 고르고, 여러 개면 Pending에 요청을 올려 두고 Choose를 기다린다.
    /// 정식 UI는 IPlayerInput을 직접 구현한다.
    /// </summary>
    public class DebugPlayerInput : IPlayerInput
    {
        public class Request
        {
            public string Prompt;
            public string[] Labels;
            internal AwaitableCompletionSource<int> Source;
        }

        /// <summary>대기 중인 선택 요청. 없으면 null.</summary>
        public Request Pending { get; private set; }

        /// <summary>선택 확정. -1 = 취소.</summary>
        public void Choose(int index)
        {
            var req = Pending;
            if (req == null) return;
            Pending = null;   // 결과 처리 중에 새 요청이 올 수 있으므로 먼저 비운다
            req.Source.SetResult(index);
        }

        public void CancelPending() => Choose(-1);

        public async Awaitable<Unit> PickUnit(IReadOnlyList<Unit> candidates, string prompt)
        {
            int i = await Ask(prompt, candidates.Select(u => u.ToString()));
            return i < 0 ? null : candidates[i];
        }

        public async Awaitable<Vector2Int?> PickTile(IReadOnlyList<Vector2Int> candidates, string prompt)
        {
            int i = await Ask(prompt, candidates.Select(p => $"({p.x},{p.y})"));
            return i < 0 ? null : (Vector2Int?)candidates[i];
        }

        public async Awaitable<CardInstance> PickCard(IReadOnlyList<CardInstance> candidates, string prompt)
        {
            int i = await Ask(prompt, candidates.Select(c => c.ToString()));
            return i < 0 ? null : candidates[i];
        }

        async Awaitable<int> Ask(string prompt, IEnumerable<string> labels)
        {
            var arr = labels.ToArray();
            if (arr.Length == 1) return 0;

            CancelPending();
            var req = new Request { Prompt = prompt, Labels = arr, Source = new AwaitableCompletionSource<int>() };
            Pending = req;
            return await req.Source.Awaitable;
        }
    }
}
