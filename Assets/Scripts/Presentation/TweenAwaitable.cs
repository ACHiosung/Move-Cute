using DG.Tweening;
using UnityEngine;

namespace MoveCute
{
    /// <summary>DOTween → Awaitable 연결. IBattlePresenter 구현에서 쓴다.</summary>
    public static class TweenAwaitable
    {
        /// <summary>
        /// 트윈이 끝나거나 Kill되면 완료되는 Awaitable.
        /// 예: public Awaitable Damaged(Unit u, int amount) => view.DOPunchScale(Vector3.one * 0.2f, 0.25f).ToAwaitable();
        /// 기존 OnComplete/OnKill 콜백은 유지된다.
        /// </summary>
        public static Awaitable ToAwaitable(this Tween tween)
        {
            var source = new AwaitableCompletionSource();
            if (tween == null || !tween.active)
            {
                source.SetResult();
                return source.Awaitable;
            }

            var prevComplete = tween.onComplete;
            var prevKill = tween.onKill;
            tween.onComplete = () => { prevComplete?.Invoke(); source.TrySetResult(); };
            tween.onKill = () => { prevKill?.Invoke(); source.TrySetResult(); };
            return source.Awaitable;
        }
    }
}
