using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>카드 원본. 카드 목록 시트를 보고 에셋으로 만든다. 런타임에 수정하지 않는다.</summary>
    [CreateAssetMenu(menuName = "MoveCute/Card", fileName = "Card_")]
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

        /// <summary>현재 언어의 설명. {damage}처럼 중괄호 안 이름은 effects의 같은 이름 필드 값으로 바뀐다.</summary>
        public string Description => Loc.Format(descKey, effects, "");
    }

    /// <summary>
    /// 실제로 덱/손패에 있는 카드. 바뀔 수 있는 값(코스트, 이동, 키워드)을 복사해 들고 있다.
    /// 런 덱의 인스턴스를 전투 시작 때 한 번 더 복사 → 전투 중 변경은 런에 남지 않음.
    /// </summary>
    public class CardInstance
    {
        public readonly CardData Data;
        public int Cost;
        public List<Direction> Moves;
        public List<Keyword> Keywords;

        /// <summary>패에서 흑백(효과 미발동) 상태인지. 카드 사용이 끝날 때마다 갱신.</summary>
        public bool IsGrayed;

        public CardInstance(CardData data)
        {
            Data = data;
            Cost = data.cost;
            Moves = new List<Direction>(data.moves);
            Keywords = new List<Keyword>(data.keywords);
        }

        public CardInstance Clone() => new(Data)
        {
            Cost = Cost,
            Moves = new List<Direction>(Moves),
            Keywords = new List<Keyword>(Keywords),
        };

        public bool Has(Keyword k) => Keywords.Contains(k);
        public int EffectiveCost => Has(Keyword.Free) ? 0 : Cost;

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
