using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 전투 연출 계약 (담당: UI/연출).
    /// BattleContext가 상태를 바꾼 "직후" 호출하고, 반환된 Awaitable이 끝날 때까지 다음 로직으로 넘어가지 않는다.
    /// → 애니메이션이 끝난 뒤 완료되는 Awaitable을 반환하면 로직이 그만큼 기다린다.
    ///
    /// 모든 함수에 기본 구현(즉시 완료)이 있어 필요한 것만 구현하면 된다.
    /// 주의: 구현 메서드는 public이고 시그니처가 정확히 같아야 한다. 다르면 기본 구현이 조용히 쓰인다.
    /// 상태는 이미 바뀐 뒤이므로 ctx를 읽어서 그린다. 여기서 ctx의 상태 변경 함수를 호출하지 않는다.
    /// </summary>
    public interface IBattlePresenter
    {
        // ── 사이클 ──
        /// <summary>전투 시작. 이후 화면 갱신에 쓸 ctx를 여기서 받아 둔다.</summary>
        Awaitable BattleStarted(BattleContext ctx) => AwaitableUtil.Completed();
        Awaitable TurnStarted(int turn) => AwaitableUtil.Completed();
        /// <summary>적의 이번 턴 예고 공개 (enemy.CurrentIntent, 타일 효과는 이미 배치됨).</summary>
        Awaitable IntentDeclared(Enemy enemy) => AwaitableUtil.Completed();
        Awaitable TurnEnded(int turn) => AwaitableUtil.Completed();
        Awaitable BattleEnded(bool won) => AwaitableUtil.Completed();
        /// <summary>행동 하나가 끝나 플레이어가 다시 조작할 수 있음 (패 흑백 갱신 완료). 입력 잠금 해제 시점.</summary>
        Awaitable InputReady() => AwaitableUtil.Completed();

        // ── 유닛 ──
        Awaitable Damaged(Unit target, int amount) => AwaitableUtil.Completed();
        /// <summary>amount = 실제 변화량 (음수 가능).</summary>
        Awaitable MoraleChanged(Enemy enemy, int amount) => AwaitableUtil.Completed();
        Awaitable StatusAdded(Unit target, string statusId, int stacks) => AwaitableUtil.Completed();

        // ── 보드 ──
        Awaitable PlayerStepped(Vector2Int from, Vector2Int to) => AwaitableUtil.Completed();
        Awaitable PlayerTeleported(Vector2Int from, Vector2Int to, int hpCost) => AwaitableUtil.Completed();
        Awaitable ObstaclePlaced(Obstacle obstacle) => AwaitableUtil.Completed();
        Awaitable ObstacleDestroyed(Obstacle obstacle) => AwaitableUtil.Completed();
        Awaitable TileEffectAdded(Vector2Int pos, TileEffect effect) => AwaitableUtil.Completed();
        Awaitable TileEffectRemoved(Vector2Int pos, TileEffect effect) => AwaitableUtil.Completed();
        /// <summary>타일 효과 발동 직전.</summary>
        Awaitable TileTriggered(Vector2Int pos, TileEffect effect) => AwaitableUtil.Completed();

        // ── 카드 ──
        Awaitable CardDrawn(CardInstance card) => AwaitableUtil.Completed();
        /// <summary>카드가 손패를 떠나 해결 시작. fullyMoved = false면 흑백(효과 없음).</summary>
        Awaitable CardPlayed(CardInstance card, bool fullyMoved) => AwaitableUtil.Completed();
        Awaitable CardDiscarded(CardInstance card) => AwaitableUtil.Completed();
        Awaitable CardTrashed(CardInstance card) => AwaitableUtil.Completed();
        /// <summary>카드 내용 변경 (키워드 추가 등).</summary>
        Awaitable CardChanged(CardInstance card) => AwaitableUtil.Completed();
        Awaitable EnergyChanged(int energy) => AwaitableUtil.Completed();
    }

    /// <summary>연출 없음. 모든 호출이 즉시 끝난다 (UI 없이 로직을 돌릴 때의 기본값).</summary>
    public class NullBattlePresenter : IBattlePresenter { }
}
