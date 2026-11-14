using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 플레이어 선택 계약 (담당: UI). 카드 대상 지정, "손패에서 1장 골라 버린다" 같은 효과 중간의 선택.
    /// 플레이어가 고를 때까지 기다렸다가 고른 후보를 반환한다. 취소하면 null.
    /// candidates는 비어 있지 않다 (BattleContext.Choose*가 보장).
    /// 로직 코드는 이걸 직접 부르지 않고 ctx.ChooseUnit/ChooseTile/ChooseCard를 쓴다.
    /// </summary>
    public interface IPlayerInput
    {
        Awaitable<Unit> PickUnit(IReadOnlyList<Unit> candidates, string prompt);
        Awaitable<Vector2Int?> PickTile(IReadOnlyList<Vector2Int> candidates, string prompt);
        Awaitable<CardInstance> PickCard(IReadOnlyList<CardInstance> candidates, string prompt);

        /// <summary>
        /// 손패에 없는 카드(휴지통·더미 등)를 목록 창에서 고른다.
        /// 목록 창을 따로 구현하지 않은 입력은 PickCard로 대신한다 (예: BattleScreenUI.CardListPicker.cs가 없으면 이 기본 구현).
        /// </summary>
        Awaitable<CardInstance> PickCardFromList(IReadOnlyList<CardInstance> candidates, string prompt) =>
            PickCard(candidates, prompt);
    }

    /// <summary>항상 첫 번째 후보를 고른다 (UI 없이 로직을 돌릴 때의 기본값).</summary>
    public class AutoPlayerInput : IPlayerInput
    {
        public Awaitable<Unit> PickUnit(IReadOnlyList<Unit> candidates, string prompt) =>
            AwaitableUtil.FromResult(candidates[0]);
        public Awaitable<Vector2Int?> PickTile(IReadOnlyList<Vector2Int> candidates, string prompt) =>
            AwaitableUtil.FromResult<Vector2Int?>(candidates[0]);
        public Awaitable<CardInstance> PickCard(IReadOnlyList<CardInstance> candidates, string prompt) =>
            AwaitableUtil.FromResult(candidates[0]);
    }
}
