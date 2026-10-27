using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>카드 원본. 카드 목록 시트를 보고 에셋으로 만든다. 런타임에 수정하지 않는다.</summary>
    [CreateAssetMenu(menuName = "MoveCute/Card/Basic", fileName = "Card_")]
    public class CardData : ScriptableObject
    {
        public string id;
        public string nameKey;
        [Tooltip("설명문 텍스트 키. 예: card.sit.desc → \"적에게 피해를 {damage} 줍니다\"")]
        public string descKey;
        public Sprite art;

        public List<Direction> moves = new();
        public int cost = 1;
        public CardType type;
        public Rarity rarity;
        public TargetType target = TargetType.None;
        public List<Keyword> keywords = new();

        [Tooltip("모든 이동이 성공했을 때 발동하는 부가 효과")]
        [SerializeReference, SubclassPicker] public List<Effect> effects = new();

        /// <summary>현재 언어의 카드 이름. 다국어 표에 없으면 에셋 이름.</summary>
        public string DisplayName => Loc.Get(nameKey, name);

        [Tooltip("특수 카드만 고른다. 고른 클래스의 Play가 카드 해결을 맡는다. (없음) = 기본 카드 (이동 → 부가 효과)")]
        [SerializeReference, SubclassPicker] public CardInstance behavior;

        /// <summary>현재 언어의 설명. {damage}처럼 중괄호 안 이름은 behavior와 effects의 같은 이름 필드 값으로 바뀐다.</summary>
        public string Description => Loc.Format(descKey, new object[] { behavior }.Concat(effects), "");

        /// <summary>런 덱에 넣을 카드를 만든다. behavior를 골랐으면 그 하위 클래스로 만든다.</summary>
        public CardInstance CreateCard() => CardInstance.Create(this);
    }

    /// <summary>
    /// 실제로 덱/손패에 있는 카드. 바뀔 수 있는 값(코스트, 이동, 키워드)을 복사해 들고 있다.
    /// 런 덱의 인스턴스를 전투 시작 때 한 번 더 복사 → 전투 중 변경은 런에 남지 않음.
    ///
    /// 특수 카드는 이 클래스를 상속해 Play를 재정의하고, 카드 에셋의 behavior에서 고른다 (예: Cards/ExampleComplicateCard.cs).
    /// 하위 클래스의 public 필드는 카드 에셋에 저장된다. 전투 중 상태 필드에는 [NonSerialized]를 붙인다.
    /// </summary>
    [Serializable]
    public class CardInstance
    {
        // 런타임 값. 에셋에는 저장하지 않고 Create에서 CardData 값을 복사해 온다
        public CardData Data { get; private set; }
        [NonSerialized] public int Cost;
        [NonSerialized] public List<Direction> Moves;
        [NonSerialized] public List<Keyword> Keywords;

        /// <summary>패에서 흑백(효과 미발동) 상태인지. 카드 사용이 끝날 때마다 갱신.</summary>
        [NonSerialized] public bool IsGrayed;

        /// <summary>
        /// data로 카드 한 장을 만든다. data.behavior를 골랐으면 그 객체를 복사해 하위 클래스와 조정 값을 그대로 가져간다.
        /// 보통은 CardData.CreateCard()로 부른다.
        /// </summary>
        public static CardInstance Create(CardData data)
        {
            var card = data.behavior != null ? (CardInstance)data.behavior.MemberwiseClone() : new CardInstance();
            card.Data = data;
            card.Cost = data.cost;
            card.Moves = new List<Direction>(data.moves);
            card.Keywords = new List<Keyword>(data.keywords);
            return card;
        }

        /// <summary>
        /// 하위 클래스 타입과 필드 값까지 그대로 복사한다. 목록(Moves, Keywords)은 새로 만든다.
        /// 얕은 복사라서 하위 클래스의 참조 필드(효과 등)는 복사본끼리 공유한다.
        /// </summary>
        public CardInstance Clone()
        {
            var card = (CardInstance)MemberwiseClone();
            card.Moves = new List<Direction>(Moves);
            card.Keywords = new List<Keyword>(Keywords);
            return card;
        }

        public bool Has(Keyword k) => Keywords.Contains(k);
        public int EffectiveCost => Has(Keyword.Free) ? 0 : Cost;

        /// <summary>
        /// 흑백 판정: 지금 내면 효과까지 모두 발동하는지. 기본은 전체 이동이 가능한지.
        /// 이동을 쓰지 않는 특수 카드는 재정의한다.
        /// </summary>
        public virtual bool CanPlayFully(BattleContext ctx) => ctx.CanFullyMove(this);

        /// <summary>지금 낼 때 드는 에너지. 흑백이면 0.</summary>
        public int PlayCost(BattleContext ctx) => CanPlayFully(ctx) ? EffectiveCost : 0;

        /// <summary>
        /// 카드 해결: 에너지 지불 → 한 칸씩 이동(타일 효과) → 모든 이동 성공 시 부가 효과 → 묘지/휴지통.
        /// 흑백 카드(전체 이동 불가)는 가능한 만큼만 이동, 부가 효과 없음, 에너지 소모 없음.
        /// BattleContext.PlayCard가 낼 수 있는지 확인하고 입력을 잠근 뒤 호출한다. 상태는 ctx 함수로만 바꾼다.
        /// 특수 카드는 재정의한다. 에너지 지불 → BeginCardPlay → (카드가 하는 일) → Finish 순서는 지킨다.
        /// </summary>
        public virtual async Awaitable Play(BattleContext ctx, EffectTarget target)
        {
            bool full = CanPlayFully(ctx);
            var src = new EffectSource(this);

            await ctx.SpendEnergy(PlayCost(ctx));
            await ctx.BeginCardPlay(this, full);
            await ctx.MovePlayerAlong(src, Moves);
            if (full) await Data.effects.ExecuteAll(ctx, src, target);

            // TODO(카드/덱): 질주 재사용, 잠금 이동 큐 등 키워드 처리
            await Finish(ctx);
        }

        /// <summary>해결이 끝난 카드를 보낸다: 삭제 키워드면 휴지통, 아니면 묘지.</summary>
        protected Awaitable Finish(BattleContext ctx) =>
            Has(Keyword.Delete) ? ctx.TrashCard(this) : ctx.SendToDiscard(this);

        public override string ToString() =>
            $"{(Data != null ? Data.DisplayName : "Card")} [{string.Concat(Moves.Select(m => m.ToArrow()))}]";
    }

    public class CardPiles
    {
        public readonly List<CardInstance> Draw = new();
        public readonly List<CardInstance> Hand = new();
        public readonly List<CardInstance> Discard = new();   // 사용한 카드 더미(묘지)
        public readonly List<CardInstance> Trash = new();     // 휴지통(삭제)

        readonly System.Random rng;

        /// <param name="rng">셔플에 쓸 시드 RNG (보통 run.Rng). 생략하면 재현 불가능한 무작위.</param>
        public CardPiles(System.Random rng = null) { this.rng = rng ?? new System.Random(); }

        public void Shuffle(List<CardInstance> list) => rng.Shuffle(list);

        /// <summary>한 장 뽑기. 드로우 더미가 비면 묘지를 섞어 보충. 둘 다 비면 null.</summary>
        public CardInstance DrawOne()
        {
            if (Draw.Count == 0)
            {
                if (Discard.Count == 0) return null;
                Draw.AddRange(Discard);
                Discard.Clear();
                Shuffle(Draw);
            }
            var card = Draw[^1];
            Draw.RemoveAt(Draw.Count - 1);
            Hand.Add(card);
            return card;
        }
    }
}
