using System;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 숫자를 어디서 가져올지. Fixed면 적어 둔 수, 나머지는 BattleContext가 전투 중 값을 준다.
    /// 새 수치가 필요하면 여기에 항목을 추가하고 BattleContext.GetValue에 계산을 한 줄 추가한다.
    /// </summary>
    public enum BattleValue
    {
        [InspectorName("고정 값")] Fixed,
        [InspectorName("휴지통 카드 수")] TrashCount,
        [InspectorName("손패 카드 수")] HandCount,
        [InspectorName("묘지 카드 수")] DiscardCount,
        [InspectorName("드로우 더미 카드 수")] DrawCount,
    }

    /// <summary>
    /// 고정 값 또는 전투 중 수치를 고를 수 있는 숫자. 키워드·효과의 int 필드 대신 쓴다.
    /// 인스펙터에서는 한 줄(가져올 곳 + 고정 값)로 보인다.
    /// 설명문 {이름}에는 고정 값일 때만 숫자가 들어간다. 전투 수치는 카드를 낼 때 정해지므로 문장으로 직접 쓴다.
    /// </summary>
    [Serializable]
    public struct BattleInt : ILocValue
    {
        public BattleValue source;
        [Tooltip("source가 고정 값일 때 쓰는 수")]
        public int value;

        public BattleInt(int value) { source = BattleValue.Fixed; this.value = value; }
        public BattleInt(BattleValue source) { this.source = source; value = 0; }

        public static implicit operator BattleInt(int value) => new(value);

        public bool IsFixed => source == BattleValue.Fixed;

        /// <summary>지금 값. 전투 수치는 부르는 순간의 값이다.</summary>
        public int Get(BattleContext ctx) => IsFixed ? value : ctx.GetValue(source);

        object ILocValue.LocValue => IsFixed ? value : null;

        public override string ToString() => IsFixed ? value.ToString() : source.ToString();
    }

    public partial class BattleContext
    {
        /// <summary>BattleInt가 가져가는 전투 중 수치.</summary>
        public int GetValue(BattleValue source) => source switch
        {
            BattleValue.TrashCount => Piles.Trash.Count,
            BattleValue.HandCount => Piles.Hand.Count,
            BattleValue.DiscardCount => Piles.Discard.Count,
            BattleValue.DrawCount => Piles.Draw.Count,
            _ => 0,
        };
    }
}
