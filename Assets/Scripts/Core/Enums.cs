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

    public enum Keyword
    {
        Retain,     // 보존: 턴 종료 시 버리지 않음
        Dash,       // 질주
        Lock,       // 잠금
        Unlock,     // 해금
        AutoLock,   // 자동 잠금
        Delete,     // 삭제: 사용 후 휴지통
        Free,       // 무료: 에너지 소모 없음
    }

    // 타일 효과 발동 시점 (UI 색: 노랑 / 파랑 / 빨강)
    public enum TileTrigger
    {
        Pass,               // 지나갈 때 (도착 포함)
        Arrive,             // 이동이 끝나 도착할 때
        TurnEndStanding,    // 그 타일에 서 있는 상태로 턴이 끝날 때
    }

    public enum BattlePhase { NotStarted, PlayerAction, Resolving, Won, Lost }
}
