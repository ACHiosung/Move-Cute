using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>상호작용 계약 — 보드 (담당: 보드/이동). 함수 이름·인자는 합의 없이 바꾸지 않는다.</summary>
    public partial class BattleContext
    {
        /// <summary>플레이어 한 칸 이동 + Pass 타일 효과. 막혀 있으면 false (이동 안 함).</summary>
        public async Awaitable<bool> MovePlayer(EffectSource src, Direction dir)
        {
            if (IsOver) return false;
            var next = Player.Pos + dir.ToOffset();
            if (!Board.IsPassable(next)) return false;

            var from = Player.Pos;
            Player.Pos = next;
            await Presenter.PlayerStepped(from, next);
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
            var pos = Player.Pos;
            foreach (var dir in card.Moves)
            {
                pos += dir.ToOffset();
                if (!Board.IsPassable(pos)) return false;
            }
            return true;
        }

        public async Awaitable<bool> PlaceObstacle(EffectSource src, Vector2Int pos, ObstacleData data)
        {
            if (IsOver) return false;
            var tile = Board.Get(pos);
            if (data == null || tile == null || !tile.Passable || pos == Player.Pos) return false;

            var o = new Obstacle(data, pos);
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
