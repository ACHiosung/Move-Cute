using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 카드를 골라 무언가를 하는 효과의 기반. from에서 후보를 모아 count장까지 한 장씩 고르고, 고를 때마다 Apply를 부른다.
    /// 고르는 창은 ctx.ChooseCardFromList (전투 화면에서는 목록 창, 고르기 전에는 닫을 수 없음).
    /// keywords를 넣으면 그 키워드가 있는 카드만 후보로 보인다. 하위 효과는 Accepts로 조건을 더할 수 있다.
    /// 후보가 count보다 적으면 있는 만큼만 고른다. 후보가 없으면 발동할 수 없어 카드가 흑백이다.
    /// 뽑을 카드 더미에서 고를 때는 뽑을 순서가 드러나지 않게 정렬해서 보여 준다.
    /// 하위 효과 예: MoveChosenCardsEffect (다른 더미로 옮김), UnlockChosenCardsEffect (해금).
    /// </summary>
    [Serializable]
    public abstract class ChooseCardsEffect : Effect
    {
        [Tooltip("고를 카드가 있는 곳")]
        public CardPileKind from = CardPileKind.Hand;

        [Tooltip("고를 카드 수 (있는 만큼만). 설명문의 {count}")]
        public BattleInt count = 1;

        [Tooltip("비어 있으면 모든 카드가 후보. 넣으면 이 키워드가 있는 카드만 후보로 보인다. " +
                 "잠금은 상태까지 구분한다 (잠금 = 아직 잠긴 카드, 해금 = 해금된 카드)")]
        [SerializeReference, SubclassPicker] public List<CardKeyword> keywords = new();

        [Tooltip("keywords가 여러 개일 때: 켜면 모두 가진 카드만, 끄면 하나라도 가진 카드")]
        public bool requireAllKeywords;

        public override bool CanExecute(BattleContext ctx) => Candidates(ctx).Count > 0;

        public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
        {
            int total = Math.Min(count.Get(ctx), Candidates(ctx).Count);
            for (int i = 0; i < total; i++)
            {
                var candidates = Candidates(ctx);
                if (candidates.Count == 0) return;
                if (from == CardPileKind.Draw)
                    candidates = candidates.OrderBy(c => c.Data.id, StringComparer.Ordinal).ToList();

                var card = await ctx.ChooseCardFromList(candidates, Prompt(i + 1, total));
                if (card == null) return;
                await Apply(ctx, card);
            }
        }

        /// <summary>고른 카드 한 장에 할 일. 상태는 ctx 함수로만 바꾼다.</summary>
        protected abstract Awaitable Apply(BattleContext ctx, CardInstance card);

        /// <summary>선택 창 제목. n번째 / 총 total장.</summary>
        protected abstract string Prompt(int n, int total);

        /// <summary>keywords 외에 하위 효과가 더하는 후보 조건 (예: 해금 효과는 잠긴 카드만).</summary>
        protected virtual bool Accepts(CardInstance card) => true;

        /// <summary>from에 있는 카드 중 keywords 조건과 Accepts를 모두 만족하는 카드.</summary>
        protected List<CardInstance> Candidates(BattleContext ctx) =>
            ctx.Piles.Get(from).Where(c => MatchesKeywords(c) && Accepts(c)).ToList();

        bool MatchesKeywords(CardInstance card)
        {
            var wanted = keywords?.Where(k => k != null).ToList();
            if (wanted == null || wanted.Count == 0) return true;
            return requireAllKeywords ? wanted.All(k => HasKeyword(card, k)) : wanted.Any(k => HasKeyword(card, k));
        }

        /// <summary>같은 종류의 키워드가 있는지. 잠금은 상태(잠금/해금)까지 같아야 한다 (자동 잠금 여부는 보지 않음).</summary>
        static bool HasKeyword(CardInstance card, CardKeyword keyword) =>
            keyword is Keyword_Lock wanted
                ? card.GetKeyword<Keyword_Lock>() is { } own && own.isUnlocked == wanted.isUnlocked
                : card.Has(keyword);

        /// <summary>안내 문구 틀(Loc 키)에 {from} {n} {total}을 채운다. 하위 효과는 {to}처럼 더 채워도 된다.</summary>
        protected string FormatPrompt(string key, string fallback, int n, int total) =>
            Loc.Get(key, fallback)
                .Replace("{from}", PileName(from))
                .Replace("{n}", n.ToString()).Replace("{total}", total.ToString());

        protected static string PileName(CardPileKind pile) =>
            Loc.Get("pile." + pile.ToString().ToLowerInvariant(), pile.ToString());
    }
}
