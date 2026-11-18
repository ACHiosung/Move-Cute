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
                Rewards = new BattleRewards(gold, RewardHealingWithRelics(healing), candidates.Take(3).ToList());
            }
            return AwaitableUtil.Completed();
        }

        /// <summary>회복 보상에 켜진 유물의 증가 %를 더해 곱한다 (수상한 송곳니 +10%). 소수점 내림.</summary>
        int RewardHealingWithRelics(int healing)
        {
            int percent = Run.Relics.Where(r => r.Enabled).OfType<RewardHealingBonusRelic>().Sum(r => r.Percent);
            if (percent <= 0 || healing <= 0) return healing;
            int boosted = (int)decimal.Floor(healing * (1m + percent / 100m));
            Log($"회복 보상 {healing} → {boosted} (유물 +{percent}%)");
            return boosted;
        }

        public Awaitable<bool> ClaimRewardGold()
        {
            if (!CanClaimReward || Rewards.GoldClaimed) return AwaitableUtil.FromResult(false);
            Rewards.GoldClaimed = true;
            Run.Gold += Rewards.Gold;
            return AwaitableUtil.FromResult(true);
        }

        /// <summary>
        /// 이긴 전투가 끝난 뒤 런 HP 회복 (자가수리키트 등 유물). 최대 HP를 넘지 않는다.
        /// 전투가 끝난 뒤(BattleEnd 이벤트)에 부르는 함수라 IsOver여도 동작한다. 진 전투에서는 아무것도 하지 않는다.
        /// </summary>
        public async Awaitable HealAfterBattle(EffectSource src, int amount)
        {
            if (Phase != BattlePhase.Won || amount <= 0) return;
            int before = Run.Hp;
            Run.Hp = Mathf.Min(Run.MaxHp, Run.Hp + amount);
            Player.Hp = Run.Hp;
            int healed = Run.Hp - before;
            Log($"{src}: HP {healed} 회복 (HP {Run.Hp})");
            if (healed > 0) await Presenter.PlayerHealed(healed);
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
