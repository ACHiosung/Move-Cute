using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MoveCute
{
    /// <summary>카드 아이콘과 상세 텍스트가 공유하는 이동 표시 규칙.</summary>
    public static class CardMoveDisplay
    {
        public const string IgnoredColorHex = "292929";
        public static readonly Color IgnoredColor = new Color32(41, 41, 41, 255);

        public static IReadOnlyList<Direction> VisibleMoves(CardInstance card) =>
            card.IsUnlocked ? System.Array.Empty<Direction>() : card.Moves;

        public static void GetRange(CardInstance card, BattleContext ctx, out int start, out int count)
        {
            start = 0;
            count = VisibleMoves(card).Count;
            if (ctx != null && card.IsLocked) ctx.GetLockMoveRange(card, out start, out count);
        }

        public static string FormatArrows(CardInstance card, BattleContext ctx)
        {
            var moves = VisibleMoves(card);
            GetRange(card, ctx, out int start, out int count);
            var text = new StringBuilder();
            for (int i = 0; i < moves.Count; i++)
            {
                bool ignored = i < start || i >= start + count;
                if (ignored) text.Append("<color=#").Append(IgnoredColorHex).Append('>');
                text.Append(moves[i].ToArrow());
                if (ignored) text.Append("</color>");
            }
            return text.ToString();
        }
    }
}
