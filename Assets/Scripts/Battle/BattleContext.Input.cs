using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 상호작용 계약 — 플레이어 선택 (담당: 전투 흐름, 실제 입력은 IPlayerInput 구현).
    /// 플레이어가 고를 때까지 기다린다. 후보가 없거나, 취소했거나, 전투가 끝났으면 null.
    /// </summary>
    public partial class BattleContext
    {
        public async Awaitable<Unit> ChooseUnit(IReadOnlyList<Unit> candidates, string prompt)
        {
            if (IsOver || candidates == null || candidates.Count == 0) return null;
            return await Input.PickUnit(candidates, prompt);
        }

        public async Awaitable<Vector2Int?> ChooseTile(IReadOnlyList<Vector2Int> candidates, string prompt)
        {
            if (IsOver || candidates == null || candidates.Count == 0) return null;
            return await Input.PickTile(candidates, prompt);
        }

        public async Awaitable<CardInstance> ChooseCard(IReadOnlyList<CardInstance> candidates, string prompt)
        {
            if (IsOver || candidates == null || candidates.Count == 0) return null;
            return await Input.PickCard(candidates, prompt);
        }
    }
}
