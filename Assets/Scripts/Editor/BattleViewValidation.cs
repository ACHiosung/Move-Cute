using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MoveCute.UIEditor
{
    /// <summary>Editor-only behavioral checks without adding an assembly/package dependency to the game.</summary>
    public static class BattleViewValidation
    {
        [MenuItem("MoveCute/UI/Validate Runtime Views")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            var root = new GameObject("ViewValidation", typeof(RectTransform));
            CardData data = null;
            GravekeeperData enemyData = null;
            try
            {
                var handRoot = Rect("Hand", root.transform, 870, 175);
                var hand = handRoot.gameObject.AddComponent<BattleHandView>();
                var first = Rect("First", handRoot, 1, 1);
                hand.Add(first); hand.ApplyIfDirty();
                int initial = hand.LayoutPasses;
                for (int i = 0; i < 1000; i++) hand.ApplyIfDirty();
                Check(hand.LayoutPasses == initial, "Idle hand must not repeat layout.");
                hand.Select(first); hand.ApplyIfDirty();
                Check(hand.LayoutPasses == initial + 1 && first.anchoredPosition.y == 26, "Selection must invalidate layout once.");

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(BattleScreenSceneBuilder.PrefabPath).GetComponent<BattleScreenUI>();
                var collection = Rect("Collection", root.transform, 600, 300).gameObject.AddComponent<BattleCardCollectionView>();
                data = ScriptableObject.CreateInstance<CardData>();
                var cards = new[] { CardInstance.Create(data), CardInstance.Create(data) };
                Action<Button, CardInstance> bind = (button, card) => button.GetComponent<BattleCardView>().Bind(card, null, null, null, null);
                collection.Render(cards, source.cardPrefab, bind);
                var slot = collection.transform.GetChild(0);
                collection.Render(cards, source.cardPrefab, bind);
                Check(collection.Capacity == 2 && collection.transform.GetChild(0) == slot, "Reopening must reuse card slots.");

                var scroll = Rect("Scroll", root.transform, 300, 200).gameObject.AddComponent<BattleCardScrollRect>();
                scroll.inertia = true;
                scroll.velocity = new Vector2(0, 900);
                scroll.OnScroll(new PointerEventData(null) { scrollDelta = Vector2.up });
                Check(scroll.velocity == Vector2.zero && scroll.inertia, "Wheel must clear old velocity while retaining drag inertia support.");

                var image = Rect("Image", root.transform, 100, 100).gameObject.AddComponent<Image>();
                image.color = new Color(.8f, .4f, .2f, .7f);
                var backdrop = root.AddComponent<BattleBackdropView>();
                backdrop.Enter(new[] { image.transform }, new[] { image.gameObject });
                Check(image.color == new Color(.8f, .4f, .2f, .7f), "Tint must not mutate the source color/alpha.");
                Check(image.canvasRenderer.GetColor().a == 1 && image.GetComponent<CanvasGroup>().alpha == 1, "Tint must not reduce opacity.");
                backdrop.Exit();
                Check(!backdrop.IsDimmed && image.GetComponent<CanvasGroup>().blocksRaycasts, "Exit must restore input.");

                var group = image.GetComponent<CanvasGroup>();
                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                group.alpha = .7f; group.interactable = group.blocksRaycasts = false;
                var originalTint = new Color(.8f, .7f, .6f, .9f);
                image.canvasRenderer.SetColor(originalTint);
                backdrop.Enter(new[] { image.transform }, new[] { image.gameObject });
                Check(group.alpha == .7f && image.canvasRenderer.GetColor().a == originalTint.a, "Dimming must preserve existing opacity.");
                backdrop.Exit();
                Check(group.alpha == .7f && !group.interactable && !group.blocksRaycasts && image.canvasRenderer.GetColor() == originalTint,
                    "Backdrop must restore pre-existing tint and input locks.");

                enemyData = ScriptableObject.CreateInstance<GravekeeperData>();
                enemyData.startMorale = 10; enemyData.prayMorale = 0; enemyData.curseTiles = 0;
                var victory = BattleFixture(data, enemyData);
                var originalHand = new System.Collections.Generic.HashSet<CardInstance>(victory.Piles.Hand);
                victory.Events.TurnEnd.Add(() => victory.ChangeMorale(EffectSource.System, victory.Enemies[0], -100));
                victory.EndTurn().GetAwaiter().GetResult();
                Check(victory.Phase == BattlePhase.Won && originalHand.SetEquals(victory.Piles.Hand) && victory.Piles.Discard.Count == 0,
                    "End-turn victory must retain the real hand without discarding or reconstructing it.");
                Check(victory.Rewards != null && victory.Rewards.Gold == 10 && victory.Rewards.Healing == 2,
                    "Battle must generate rewards without a UI presenter.");

                var rebound = BattleFixture(data, enemyData);
                rebound.Events.TurnEnd.Add(async () =>
                {
                    await rebound.ChangeMorale(EffectSource.System, rebound.Enemies[0], -100);
                    await rebound.ChangeMorale(EffectSource.System, rebound.Enemies[0], 200);
                });
                rebound.EndTurn().GetAwaiter().GetResult();
                Check(!rebound.IsOver && rebound.Turn == 2 && rebound.Piles.Discard.Count == 6 && rebound.Piles.Hand.Count == 6,
                    "Finish all reactions before judging; continuing battles still discard and draw normally.");

                var limited = BattleFixture(data, enemyData, 1);
                var finalHand = new System.Collections.Generic.HashSet<CardInstance>(limited.Piles.Hand);
                limited.EndTurn().GetAwaiter().GetResult();
                Check(limited.IsOver && finalHand.SetEquals(limited.Piles.Hand) && limited.Piles.Discard.Count == 0,
                    "Turn-limit battle completion must not discard the final hand.");
                Debug.Log("UI validation passed: layouts/pools/scroll, exact backdrop restore, terminal hand, complete reactions, headless rewards.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (data) Object.DestroyImmediate(data);
                if (enemyData) Object.DestroyImmediate(enemyData);
            }
        }

        static BattleContext BattleFixture(CardData card, EnemyData enemy, int turnLimit = 3)
        {
            var run = new RunState(123);
            for (int i = 0; i < 12; i++) run.Deck.Add(CardInstance.Create(card));
            var battle = new BattleContext(run, new BattleConfig { turnLimit = turnLimit }, new[] { enemy });
            battle.StartBattle().GetAwaiter().GetResult();
            return battle;
        }

        static RectTransform Rect(string name, Transform parent, float width, float height)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
