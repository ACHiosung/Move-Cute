using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>Awaitable 보조 함수.</summary>
    public static class AwaitableUtil
    {
        /// <summary>이미 끝난 Awaitable. 연출이 없는 기본 구현에서 반환한다.</summary>
        public static Awaitable Completed()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        public static Awaitable<T> FromResult<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }

        /// <summary>
        /// 기다리지 않고 실행. UI 버튼·Start 같은 최상위 호출 전용이며, 예외는 콘솔에 남긴다.
        /// 로직 코드(효과·적·문맥) 안에서는 쓰지 않는다 — 항상 await.
        /// </summary>
        public static async void Forget(this Awaitable awaitable)
        {
            try { await awaitable; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }

        public static async void Forget<T>(this Awaitable<T> awaitable)
        {
            try { await awaitable; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
