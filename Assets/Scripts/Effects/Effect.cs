using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 모든 효과의 기반. 카드·타일·유물·적·장애물이 공유한다.
    /// 효과는 BattleContext의 함수만 호출해서 게임 상태를 바꾼다.
    /// 새 효과 = 이 클래스를 상속한 [Serializable] 클래스 하나 추가.
    /// </summary>
    [Serializable]
    public abstract class Effect
    {
        /// <summary>
        /// 반환되는 시점에 이 효과의 모든 결과(연출·연쇄 반응 포함)가 끝나 있어야 한다.
        /// ctx 함수는 반드시 await한다. 전투 종료 체크는 ctx가 하므로 신경 쓰지 않아도 된다.
        /// </summary>
        public abstract Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target);
    }

    public static class EffectListExtensions
    {
        /// <summary>
        /// 효과 목록을 순서대로 끝까지 실행. 전투가 끝나면 남은 효과는 건너뛴다.
        /// 카드·타일·유물·적·장애물은 자기 효과를 이걸로 발동한다 (BattleContext는 언제 발동할지만 알린다).
        /// </summary>
        public static async Awaitable ExecuteAll(this IEnumerable<Effect> effects, BattleContext ctx, EffectSource src, EffectTarget target)
        {
            if (effects == null) return;
            foreach (var e in effects.ToList())
            {
                if (ctx.IsOver) return;
                if (e != null) await e.Execute(ctx, src, target);
            }
        }
    }

    /// <summary>효과를 발생시킨 주체. CardInstance / Relic / TileEffect / Enemy / Obstacle / Status 중 하나.</summary>
    public readonly struct EffectSource
    {
        public readonly object Owner;
        public EffectSource(object owner) { Owner = owner; }

        public static readonly EffectSource System = new(null);
        public override string ToString() => Owner?.ToString() ?? "System";
    }

    /// <summary>효과의 대상. 카드의 TargetType에 따라 하나만 채워진다.</summary>
    public struct EffectTarget
    {
        public Unit Unit;
        public Vector2Int? Tile;
        public CardInstance Card;

        public static readonly EffectTarget None = default;
        public static EffectTarget OfUnit(Unit u) => new() { Unit = u };
        public static EffectTarget OfTile(Vector2Int p) => new() { Tile = p };
        public static EffectTarget OfCard(CardInstance c) => new() { Card = c };
    }

    /// <summary>[SerializeReference] 필드에 붙이면 인스펙터에서 하위 타입을 고르는 드롭다운이 나온다.</summary>
    public class SubclassPickerAttribute : PropertyAttribute { }
}
