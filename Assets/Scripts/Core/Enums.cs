using UnityEngine;

namespace MoveCute
{
    public enum Direction { Left, Up, Right, Down }

    public static class DirectionExt
    {
        // 보드 좌표: (0,0) = 왼쪽 아래, y가 위로 증가
        public static Vector2Int ToOffset(this Direction dir) => dir switch
        {
            Direction.Left => Vector2Int.left,
            Direction.Up => Vector2Int.up,
            Direction.Right => Vector2Int.right,
            Direction.Down => Vector2Int.down,
            _ => Vector2Int.zero,
        };

        public static string ToArrow(this Direction dir) => dir switch
        {
            Direction.Left => "←",
            Direction.Up => "↑",
            Direction.Right => "→",
            Direction.Down => "↓",
            _ => "?",
        };
    }

    public enum CardType { None, Attack, Skill }

    public enum Rarity { Basic, Common, Uncommon, Rare }

    // 카드 대상 4종 + 대상 없음
    public enum TargetType { None, Unit, SelfTile, OtherTile, Card }
    // 타일 효과 발동 시점 (UI 색: 노랑 / 파랑 / 빨강)
    public enum TileTrigger
    {
        Pass,               // 지나갈 때 (도착 포함)
        Arrive,             // 이동이 끝나 도착할 때
        TurnEndStanding,    // 그 타일에 서 있는 상태로 턴이 끝날 때
    }

    public enum BattlePhase { NotStarted, PlayerAction, Resolving, Won, Lost }

    // 대상이 Unit인 카드가 고를 수 있는 종류. 여러 개를 함께 고를 수 있다 (CardData.targetKinds)
    [System.Flags]
    public enum UnitTargetKind
    {
        Enemy = 1,
        Obstacle = 2,   // 폭탄이 아닌 장애물 (묘비 등)
        Bomb = 4,       // ObstacleData.isBomb가 켜진 장애물
        All = Enemy | Obstacle | Bomb,
    }
}
