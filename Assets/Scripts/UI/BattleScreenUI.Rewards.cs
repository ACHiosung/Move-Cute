using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        [Header("전투 보상")]
        public Button rewardChest, rewardGoldButton, rewardHealButton, rewardCardButton;
        public Button rewardNextButton, rewardBackButton;
        public GameObject rewardOverlay, rewardListPanel, rewardCardsPanel;
        public RectTransform rewardCandidates;
        public Text goldBalance;
        bool rewardScreenActive;
        bool rewardTransitioning;
        readonly List<CardInstance> rewardDisplayCards = new();
        BattleRewards displayedRewards;

        void InitializeRewards()
        {
            rewardOverlay.GetComponent<Image>().color = Color.clear;
            rewardOverlay.GetComponent<BattleScreenPointer>().RightClick = RewardBack;
            rewardChest.onClick.AddListener(OpenRewardList);
            rewardGoldButton.onClick.AddListener(() => ClaimGold().Forget());
            rewardHealButton.onClick.AddListener(() => ClaimHealing().Forget());
            rewardCardButton.onClick.AddListener(OpenRewardCards);
            rewardNextButton.onClick.AddListener(RequestNextBattle);
            rewardBackButton.onClick.AddListener(OpenRewardList);
            BindTooltip(rewardChest, () => T("rewardHelp"), () => T("rewards"));
            BindTooltip(rewardGoldButton, () => $"{T("gold")} +{ctx.Rewards.Gold}", () => T("gold"));
            BindTooltip(rewardHealButton, () => $"HP +{ctx.Rewards.Healing}", () => T("healing"));
            BindTooltip(rewardCardButton, () => T("chooseRewardHelp"), () => T("cardReward"));
            foreach (var button in new[] { rewardChest, rewardGoldButton, rewardHealButton, rewardCardButton,
                rewardNextButton, rewardBackButton })
            {
                var relay = button.GetComponent<BattleScreenPointer>();
                if (!relay) relay = button.gameObject.AddComponent<BattleScreenPointer>();
                relay.RightClick = RewardBack;
            }
        }

        void ResetRewardScreen()
        {
            rewardScreenActive = false;
            rewardTransitioning = false;
            rewardChest.gameObject.SetActive(false);
            rewardOverlay.SetActive(false);
            displayedRewards = null;
            rewardDisplayCards.Clear();
            foreach (var view in new[] { hand.gameObject, enemies.gameObject, playerMarker.gameObject,
                energyButton.gameObject, endTurnButton.gameObject, drawText.transform.parent.gameObject,
                discardText.transform.parent.gameObject }) view.SetActive(true);
            SetBattleBackdrop(false);
        }

        void ShowRewardChest()
        {
            rewardScreenActive = true;
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            resultPanel.SetActive(false);
            modal.gameObject.SetActive(false);
            promptRoot.SetActive(false);
            enemies.gameObject.SetActive(false);
            SetBattleBackdrop(true);
            rewardChest.gameObject.SetActive(true);
            ShowInformation(T("rewards"), T("rewardHelp"));
            RefreshScreenHud();
        }

        void SetBattleBackdrop(bool faded)
        {
            if (faded) backdropView.Enter(backdropRoots, blockedBattleRoots);
            else backdropView.Exit();
        }



        void OpenRewardList()
        {
            if (!rewardScreenActive || ctx.Rewards == null || rewardTransitioning) return;
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            rewardOverlay.SetActive(true);
            rewardChest.gameObject.SetActive(false);
            rewardOverlay.transform.SetAsLastSibling();
            rewardListPanel.SetActive(true);
            rewardCardsPanel.SetActive(false);
            detailPanelRoot.SetAsLastSibling();
            playerPanelRoot.SetAsLastSibling();
            RefreshRewardList();
            RestorePanel();
        }

        void RefreshRewardList()
        {
            var rewards = ctx.Rewards;
            rewardListView.Render(rewards, $"{T("gold")} +{rewards.Gold}",
                $"{T("healing")} +{rewards.Healing}", T("cardReward"));
            RefreshScreenHud();
        }



        async Awaitable ClaimGold()
        {
            if (!rewardScreenActive) return;
            await ctx.ClaimRewardGold();
            RefreshRewardList();
        }

        async Awaitable ClaimHealing()
        {
            if (!rewardScreenActive) return;
            await ctx.ClaimRewardHealing();
            RefreshRewardList();
        }

        void OpenRewardCards()
        {
            if (!rewardScreenActive || ctx.Rewards.CardResolved || rewardTransitioning) return;
            rewardListPanel.SetActive(false);
            rewardChest.gameObject.SetActive(true);
            rewardCardsPanel.SetActive(true);
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            if (displayedRewards != ctx.Rewards)
            {
                rewardDisplayCards.Clear();
                foreach (var data in ctx.Rewards.Cards) rewardDisplayCards.Add(CardInstance.Create(data));
                displayedRewards = ctx.Rewards;
            }
            rewardChoicesView.Render(rewardDisplayCards, cardPrefab, BindRewardCard);
            ShowInformation(T("cardReward"), T("chooseRewardHelp"));
        }

        async Awaitable TakeRewardCard(CardData data)
        {
            if (!rewardScreenActive || rewardTransitioning) return;
            if (await ctx.ClaimRewardCard(data)) OpenRewardList();
        }

        void RewardBack()
        {
            if (!rewardScreenActive) return;
            if (modal.gameObject.activeSelf) { CloseCardBrowser(); return; }
            if (rewardCardsPanel.activeSelf) { OpenRewardList(); return; }
            rewardOverlay.SetActive(false);
            rewardChest.gameObject.SetActive(true);
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            RestorePanel();
        }

        void RequestNextBattle()
        {
            if (!rewardScreenActive || rewardTransitioning || ctx.Rewards == null) return;
            ContinueAfterRewards().Forget();
        }

        async Awaitable ContinueAfterRewards()
        {
            if (!rewardScreenActive || rewardTransitioning) return;
            rewardTransitioning = true;
            await ctx.CloseRewards();
            await M.StartBattle();
        }


    }
}
