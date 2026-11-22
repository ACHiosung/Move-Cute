using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    [System.Serializable]
    public sealed class BattleRewardPolicy
    {
        [Min(0)] public int gold = 10;
        [Tooltip("HP 회복 보상 = 이 전투에서 적 사기를 깎은 양(피해 누적)의 이 비율(%), 소수점 내림. 최대 HP를 넘으면 최대 HP도 늘어남")]
        [Min(0)] public int healingPercentOfMorale = 10;
        [Tooltip("Empty uses the run deck's card types. A configured pool is independent of the UI.")]
        public List<CardData> cardPool = new();
    }

    public sealed class BattleRewards
    {
        public int Gold { get; }
        public int Healing { get; }
        public IReadOnlyList<CardData> Cards { get; }
        public bool GoldClaimed { get; internal set; }
        public bool HealingClaimed { get; internal set; }
        public bool CardResolved { get; internal set; }
        public bool Closed { get; internal set; }
        public bool HasRemaining => !GoldClaimed || !HealingClaimed || !CardResolved;

        internal BattleRewards(int gold, int healing, List<CardData> cards)
        {
            Gold = Mathf.Max(0, gold);
            Healing = Mathf.Max(0, healing);
            Cards = cards.AsReadOnly();
            CardResolved = cards.Count == 0;
        }
    }

    public partial class BattleContext
    {
        public BattleRewards Rewards { get; private set; }
        bool CanClaimReward => Phase == BattlePhase.Won && Rewards != null && !Rewards.Closed;

        Awaitable PrepareConfiguredRewards()
        {
            var policy = Config.rewards ?? new BattleRewardPolicy();
            IEnumerable<CardData> pool = policy.cardPool != null && policy.cardPool.Count > 0
                ? policy.cardPool : Run.Deck.Select(card => card.Data);
            return PrepareRewards(pool, policy.gold, MoraleDealt * policy.healingPercentOfMorale / 100);
        }

        // Generated once per battle. Reopening the chest never rerolls candidates.
        public Awaitable PrepareRewards(IEnumerable<CardData> pool, int gold = 10, int healing = 2)
        {
            if (Phase == BattlePhase.Won && Rewards == null)
            {
                var candidates = pool.Where(c => c != null).Distinct().ToList();
                Rng.Shuffle(candidates);
                Rewards = new BattleRewards(gold, healing, candidates.Take(3).ToList());
            }
            return AwaitableUtil.Completed();
        }

        public Awaitable<bool> ClaimRewardGold()
        {
            if (!CanClaimReward || Rewards.GoldClaimed) return AwaitableUtil.FromResult(false);
            Rewards.GoldClaimed = true;
            Run.Gold += Rewards.Gold;
            return AwaitableUtil.FromResult(true);
        }

        public Awaitable<bool> ClaimRewardHealing()
        {
            if (!CanClaimReward || Rewards.HealingClaimed) return AwaitableUtil.FromResult(false);
            Rewards.HealingClaimed = true;
            // 최대 HP를 넘는 만큼은 최대 HP도 함께 늘어난다 (HP 49/50 + 2 -> 51/51)
            Run.Hp += Rewards.Healing;
            Run.MaxHp = Mathf.Max(Run.MaxHp, Run.Hp);
            Player.Hp = Run.Hp;
            Player.MaxHp = Run.MaxHp;
            return AwaitableUtil.FromResult(true);
        }

        public Awaitable<bool> ClaimRewardCard(CardData card)
        {
            if (!CanClaimReward || Rewards.CardResolved || !Rewards.Cards.Contains(card))
                return AwaitableUtil.FromResult(false);
            Rewards.CardResolved = true;
            Run.Deck.Add(CardInstance.Create(card));
            return AwaitableUtil.FromResult(true);
        }

        public Awaitable<bool> SkipRewardCard()
        {
            if (!CanClaimReward || Rewards.CardResolved) return AwaitableUtil.FromResult(false);
            Rewards.CardResolved = true;
            return AwaitableUtil.FromResult(true);
        }

        public Awaitable CloseRewards()
        {
            if (CanClaimReward) Rewards.Closed = true;
            return AwaitableUtil.Completed();
        }
    }
}
