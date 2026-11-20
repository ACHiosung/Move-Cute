using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>상호작용 계약 — 보드 (담당: 보드/이동). 함수 이름·인자는 합의 없이 바꾸지 않는다.</summary>
    public partial class BattleContext
    {
        /// <summary>
        /// 플레이어 한 칸 이동 + Pass 타일 효과. 막혀 있으면 false (이동 안 함).
        /// 경계 이동이 켜져 있으면 보드 밖 대신 반대편 끝 칸으로 간다 (StepTarget). 이때도 dir 방향 한 칸 이동으로 친다.
        /// </summary>
        public async Awaitable<bool> MovePlayer(EffectSource src, Direction dir)
        {
            if (IsOver) return false;
            if (src.Owner is CardInstance card && SkipsCardMoves(card)) return false;
            var from = Player.Pos;
            var next = StepTarget(from, dir);
            if (!Board.IsPassable(next)) return false;

            Player.Pos = next;
            if (next != from + dir.ToOffset()) await Presenter.PlayerWrapped(from, next, dir);
            else await Presenter.PlayerStepped(from, next);
            await TrackLockMovement(dir);
            await Events.PlayerMoved.Raise(from, next);
            await TriggerTile(next, TileTrigger.Pass);
            return true;
        }

        /// <summary>
        /// 카드 이동: 방향대로 한 칸씩 MovePlayer, 막히면 거기서 멈춘다.
        /// 한 칸이라도 움직였으면 멈춘 칸의 Arrive 타일 효과까지 끝내고 돌아온다.
        /// </summary>
        public Awaitable MovePlayerAlong(EffectSource src, IEnumerable<Direction> moves) =>
            MovePlayerAlong(src, moves, null);

        /// <summary>
        /// MovePlayerAlong + 한 칸 이동할 때마다 onStep(떠난 칸, 도착한 칸)을 기다린다 (Pass 타일 효과 다음).
        /// 지나온 칸에 폭탄을 남기는 카드 등. 마지막 칸의 onStep이 끝난 뒤 Arrive 타일 효과가 발동한다.
        /// </summary>
        public async Awaitable MovePlayerAlong(EffectSource src, IEnumerable<Direction> moves,
                                               Func<Vector2Int, Vector2Int, Awaitable> onStep)
        {
            if (src.Owner is CardInstance card && SkipsCardMoves(card)) return;
            bool moved = false;
            foreach (var dir in moves.ToList())
            {
                var from = Player.Pos;
                if (IsOver || !await MovePlayer(src, dir)) break;
                moved = true;
                if (onStep != null) await onStep(from, Player.Pos);
            }
            if (moved) await TriggerTile(Player.Pos, TileTrigger.Arrive);
        }

        /// <summary>
        /// 카드의 전체 이동이 가능한지 (흑백 판정). 판정만 하므로 동기.
        /// TODO(보드/이동): 이동 중 타일 효과·강제 이동 반영 (NDM은 턴 중 강제 이동 없음).
        /// </summary>
        public bool CanFullyMove(CardInstance card)
        {
            if (card.IsLocked) return false;
            if (SkipsCardMoves(card)) return true;   // 해금 카드·효과만 다시 내는 중인 카드는 경로와 무관
            var pos = Player.Pos;
            foreach (var dir in card.Moves)
            {
                pos = StepTarget(pos, dir);
                if (!Board.IsPassable(pos)) return false;
            }
            return true;
        }

        /// <summary>
        /// from에서 dir로 한 칸 간 칸. 보드 밖이면 보통은 보드 밖 좌표를 그대로 돌려주고 (IsPassable이 false),
        /// 경계 이동 규칙(IEdgeWrapRule, 세계의 경계)이 켜져 있으면 반대편 끝 칸을 돌려준다.
        /// 한 칸 이동할 칸은 pos + dir.ToOffset() 대신 이것으로 구한다 (이동, 흑백 판정, 후보 칸, 경로 미리보기).
        /// </summary>
        public Vector2Int StepTarget(Vector2Int from, Direction dir)
        {
            var next = from + dir.ToOffset();
            return !Board.InBounds(next) && WrapsAtEdge ? Board.Wrap(next) : next;
        }

        /// <summary>보드 밖으로 나가는 이동이 반대편 끝 칸으로 이어지는지 (켜진 유물·버프 중 IEdgeWrapRule이 있음).</summary>
        public bool WrapsAtEdge => Modifiers<IEdgeWrapRule>().Any();

        public async Awaitable<bool> PlaceObstacle(EffectSource src, Vector2Int pos, ObstacleData data)
        {
            if (IsOver) return false;
            var tile = Board.Get(pos);
            if (data == null || tile == null || !tile.Passable || pos == Player.Pos) return false;

            var o = new Obstacle(data, pos);
            o.PlacedByPlayer = src.Owner is not Enemy;
            tile.Occupant = o;
            Obstacles.Add(o);
            Log($"{src}: {o} 배치 {pos}");
            await Presenter.ObstaclePlaced(o);
            return true;
        }

        /// <summary>장애물 파괴 + 파괴 시 효과(폭발 등)까지 끝내고 돌아온다.</summary>
        public async Awaitable DestroyObstacle(EffectSource src, Obstacle o)
        {
            if (IsOver || !Obstacles.Remove(o)) return;
            Board.Get(o.Pos).Occupant = null;
            Log($"{o} 파괴 {o.Pos}");
            await Presenter.ObstacleDestroyed(o);
            await Events.ObstacleDestroyed.Raise(o);
            await o.OnDestroyed(this);
        }

        /// <summary>
        /// 두 칸에 있는 것(장애물·플레이어)을 맞바꾼다 (재배치). 빈 칸과도 바꿀 수 있고, 둘 다 비어 있으면 아무 일도 없다.
        /// 칸에 걸린 타일 효과(적 예고 등)는 칸에 그대로 남는다.
        /// 플레이어가 옮겨져도 이동으로 치지 않는다: 잠금 진행·PlayerMoved(MOVE CUTE 등)·Pass 효과 없이 도착한 칸의 Arrive 효과만 발동한다.
        /// </summary>
        public async Awaitable SwapTiles(EffectSource src, Vector2Int a, Vector2Int b)
        {
            if (IsOver || a == b) return;
            var tileA = Board.Get(a);
            var tileB = Board.Get(b);
            if (tileA == null || tileB == null) return;
            bool playerAtA = Player.Pos == a, playerAtB = Player.Pos == b;
            if (tileA.Occupant == null && tileB.Occupant == null && !playerAtA && !playerAtB) return;

            (tileA.Occupant, tileB.Occupant) = (tileB.Occupant, tileA.Occupant);
            if (tileA.Occupant != null) tileA.Occupant.Pos = a;
            if (tileB.Occupant != null) tileB.Occupant.Pos = b;
            if (playerAtA) Player.Pos = b;
            else if (playerAtB) Player.Pos = a;

            Log($"{src}: {a} ↔ {b} 교환");
            await Presenter.TilesSwapped(a, b);
            if (playerAtA || playerAtB) await TriggerTile(Player.Pos, TileTrigger.Arrive);
        }

        public async Awaitable AddTileEffect(Vector2Int pos, TileEffect effect)
        {
            var tile = Board.Get(pos);
            if (IsOver || tile == null) return;
            tile.Effects.Add(effect);
            await Presenter.TileEffectAdded(pos, effect);
        }

        /// <summary>source가 건 타일 효과 전부 제거 (적의 지난 턴 예고 정리 등).</summary>
        public async Awaitable RemoveTileEffects(object source)
        {
            if (IsOver) return;
            foreach (var tile in Board.AllTiles())
                foreach (var te in tile.Effects.Where(t => t.Source == source).ToList())
                {
                    tile.Effects.Remove(te);
                    await Presenter.TileEffectRemoved(tile.Pos, te);
                }
        }
    }
}
