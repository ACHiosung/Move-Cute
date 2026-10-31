using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    [System.Serializable]
    public sealed class BattleRewardPolicy
    {
        [Min(0)] public int gold = 10;
        [Min(0)] public int healing = 2;
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
            return PrepareRewards(pool, policy.gold, policy.healing);
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
            Run.Hp = Mathf.Min(Run.MaxHp, Run.Hp + Rewards.Healing);
            Player.Hp = Run.Hp;
            return AwaitableUtil.FromResult(true);
        }

        public Awaitable<bool> ClaimRewardCard(CardData card)
        {
            if (!CanClaimReward || Rewards.CardResolved || !Rewards.Cards.Contains(card))
                return AwaitableUtil.FromResult(false);
            Rewards.CardResolved = true;
            Run.Deck.Add(new CardInstance(card));
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
