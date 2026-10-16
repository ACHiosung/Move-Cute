using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    public class Board
    {
        public readonly int Size;
        readonly Tile[,] tiles;

        public Board(int size)
        {
            Size = size;
            tiles = new Tile[size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tiles[x, y] = new Tile(new Vector2Int(x, y));
        }

        public bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Size && p.y < Size;
        public Tile Get(Vector2Int p) => InBounds(p) ? tiles[p.x, p.y] : null;
        public bool IsPassable(Vector2Int p) => InBounds(p) && tiles[p.x, p.y].Passable;

        public IEnumerable<Tile> AllTiles()
        {
            foreach (var t in tiles) yield return t;
        }

        /// <summary>3x3, 5x5는 정중앙. 4x4는 중앙 4칸 중 하나(기본 왼쪽 아래) — 선택 UI는 추후.</summary>
        public Vector2Int Center => new((Size - 1) / 2, (Size - 1) / 2);

        public Vector2Int? GetRandomEmptyTile(Vector2Int exclude)
        {
            var list = new List<Vector2Int>();
            foreach (var t in tiles)
                if (t.Passable && t.Pos != exclude) list.Add(t.Pos);
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }
    }

    public class Tile
    {
        public readonly Vector2Int Pos;
        public Obstacle Occupant;
        public readonly List<TileEffect> Effects = new();

        public Tile(Vector2Int pos) { Pos = pos; }

        public bool Passable => Occupant == null;
    }

    /// <summary>타일에 걸린 효과. Trigger = 언제 발동, Source = 누가 걸었나(지울 때·조건 판정용).</summary>
    public class TileEffect
    {
        public readonly TileTrigger Trigger;
        public readonly List<Effect> Effects;
        public readonly object Source;
        /// <summary>남은 턴 수. -1 = 영구. 적 예고는 다음 턴 DeclareIntent에서 Source로 지운다.</summary>
        public int Duration;

        public TileEffect(TileTrigger trigger, List<Effect> effects, object source, int duration = -1)
        {
            Trigger = trigger;
            Effects = effects;
            Source = source;
            Duration = duration;
        }

        public override string ToString() => $"{Trigger}({Source})";
    }
}
