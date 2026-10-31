#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        public void UpgradeViewComponents()
        {
            EnsureView<BattleCardView>(cardPrefab.gameObject).CaptureReferences();
            EnsureView<BattleEnemyView>(enemyPrefab.gameObject).CaptureReferences();
            EnsureView<BattleTileView>(tilePrefab.gameObject).CaptureReferences();
            handView = EnsureView<BattleHandView>(hand.gameObject);
            var oldLayout = hand.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout) oldLayout.enabled = false;
            previewView = EnsureView<BattlePreviewView>(previewRoot.gameObject);
            browserView = EnsureView<BattleCardCollectionView>(modalContent.gameObject);
            rewardChoicesView = EnsureView<BattleCardCollectionView>(rewardCandidates.gameObject);
            backdropView = EnsureView<BattleBackdropView>(gameObject);
            energyStrip = EnsureView<BattleCellStripView>(energyCells.gameObject);
            turnStrip = EnsureView<BattleCellStripView>(turnCells.gameObject);
            rewardListView = EnsureView<BattleRewardListView>(rewardListPanel);
            rewardListView.Configure(rewardGoldButton, rewardHealButton, rewardCardButton);
        }

        static T EnsureView<T>(GameObject target) where T : Component
        {
            var view = target.GetComponent<T>();
            if (!view) view = target.AddComponent<T>();
            return view;
        }
    }
}
#endif
