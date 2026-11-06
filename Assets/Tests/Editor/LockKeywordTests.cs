using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoveCute.Tests
{
    public class LockKeywordTests
    {
        readonly List<ScriptableObject> assets = new();
        BattleContext ctx;

        sealed class IdleEnemy : Enemy
        {
            public IdleEnemy(EnemyData data) : base(data) { }
            protected override IntentData PlanIntent(BattleContext context) => null;
        }

        sealed class CardChangePresenter : IBattlePresenter
        {
            readonly IBattlePresenter ui;
            public CardChangePresenter(IBattlePresenter ui) { this.ui = ui; }
            public Awaitable CardChanged(CardInstance card) => ui.CardChanged(card);
        }

        [SetUp]
        public void SetUp()
        {
            var enemy = ScriptableObject.CreateInstance<GravekeeperData>();
            enemy.nameKey = "enemy.gravekeeper.name";
            enemy.startMorale = 1000;
            assets.Add(enemy);
            ctx = new BattleContext(new RunState(123) { Act = 3 }, new BattleConfig(), new EnemyData[0]);
            ctx.Enemies.Add(new IdleEnemy(enemy));
            Complete(ctx.StartBattle());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        CardInstance Card(params Direction[] moves)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.nameKey = "card.sit.name";
            data.moves.AddRange(moves);
            data.cost = 3;
            data.effects.Add(new DamageEffect { damage = 2 });
            assets.Add(data);
            var card = data.CreateCard();
            ctx.Piles.Hand.Add(card);
            return card;
        }

        static void Complete(Awaitable operation)
        {
            var awaiter = operation.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Null presenter should complete synchronously");
            awaiter.GetResult();
        }

        static T Complete<T>(Awaitable<T> operation)
        {
            var awaiter = operation.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True);
            return awaiter.GetResult();
        }

        void Step(Direction direction) => Assert.That(Complete(ctx.MovePlayer(EffectSource.System, direction)), Is.True);

        [Test]
        [TestCase(typeof(BattleScreenUI))]
        [TestCase(typeof(TempBattleUI))]
        public void LeftDownUpDownAcrossTurnsUnlocksCardsWithoutHandViews(System.Type uiType)
        {
            // 비활성 UI로 Awake의 씬 바인딩 없이 실제 CardChanged 경로를 검증한다.
            var root = new GameObject("Off-hand keyword change test");
            root.SetActive(false);
            try
            {
                var ui = (IBattlePresenter)root.AddComponent(uiType);
                var enemyData = ctx.Enemies[0].Data;
                ctx = new BattleContext(new RunState(123), new BattleConfig { handSize = 0 },
                    new EnemyData[0], new CardChangePresenter(ui));
                ctx.Enemies.Add(new IdleEnemy(enemyData));
                Complete(ctx.StartBattle());
                var drawCard = Card(Direction.Left, Direction.Down, Direction.Up, Direction.Down);
                drawCard.Keywords.Add(new Keyword_Lock());
                ctx.Piles.Hand.Remove(drawCard);
                ctx.Piles.Draw.Add(drawCard);
                var discardCard = drawCard.Clone();
                ctx.Piles.Discard.Add(discardCard);

                Step(Direction.Left);
                Step(Direction.Down);
                Complete(ctx.EndTurn());
                Step(Direction.Up);
                Step(Direction.Down);

                Assert.That(drawCard.IsUnlocked, Is.True);
                Assert.That(discardCard.IsUnlocked, Is.True);
                Assert.That(ctx.CanAct, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void LockAndUnlockShareOneKeywordAndPreserveAutomaticLockTag()
        {
            var card = Card(Direction.Left);
            var sourceKeyword = new Keyword_Lock { autoLock = true };
            Complete(ctx.AddKeyword(card, sourceKeyword));
            var keyword = card.GetKeyword<Keyword_Lock>();
            Assert.That(keyword, Is.Not.SameAs(sourceKeyword));
            Assert.That(keyword.isUnlocked, Is.False);
            Assert.That(keyword.ToString(), Is.EqualTo("Lock"));
            Complete(ctx.UnlockCard(card));
            Assert.That(card.GetKeyword<Keyword_Lock>(), Is.SameAs(keyword));
            Assert.That(keyword.isUnlocked, Is.True);
            Assert.That(keyword.ToString(), Is.EqualTo("Unlock"));
            Assert.That(keyword.autoLock, Is.True);
            Assert.That(sourceKeyword.isUnlocked, Is.False);
            Assert.That(card.Keywords, Has.Count.EqualTo(1));
            Complete(ctx.LockCard(card));
            Assert.That(card.GetKeyword<Keyword_Lock>(), Is.SameAs(keyword));
            Assert.That(keyword.isUnlocked, Is.False);
            Assert.That(keyword.autoLock, Is.True);
        }

        [Test]
        public void AddKeywordAcceptsUnlockedStateAndCloneKeepsIndependentState()
        {
            var card = Card(Direction.Left);
            Complete(ctx.AddKeyword(card, new Keyword_Lock { isUnlocked = true, autoLock = true }));
            Assert.That(card.IsUnlocked, Is.True);
            var clone = card.Clone();
            Complete(ctx.LockCard(clone));
            Assert.That(card.IsUnlocked, Is.True);
            Assert.That(clone.IsLocked, Is.True);
            Assert.That(clone.AutoLock, Is.True);
            Assert.That(card.Keywords, Has.Count.EqualTo(1));
            Assert.That(clone.Keywords, Has.Count.EqualTo(1));
        }

        [Test]
        public void LockedCardCannotBePlayedOrSpendEnergy()
        {
            var card = Card(Direction.Left);
            Complete(ctx.AddKeyword(card, new Keyword_Lock()));
            int energy = ctx.Energy;
            var position = ctx.Player.Pos;
            Assert.That(Complete(ctx.PlayCard(card, EffectTarget.None)), Is.False);
            Assert.That(ctx.Energy, Is.EqualTo(energy));
            Assert.That(ctx.Player.Pos, Is.EqualTo(position));
            Assert.That(ctx.Piles.Hand, Does.Contain(card));
        }

        [Test]
        public void PatternMatchesAcrossCardsAndPilesWithOverlappingPrefix()
        {
            var card = Card(Direction.Left, Direction.Left, Direction.Right);
            Complete(ctx.LockCard(card));
            Complete(ctx.DiscardCard(card));
            ctx.Player.Pos = new Vector2Int(3, 2);
            Step(Direction.Right);
            Step(Direction.Left);
            Step(Direction.Left);
            Step(Direction.Left);
            Assert.That(card.IsLocked, Is.True);
            Step(Direction.Right);
            Assert.That(card.IsLocked, Is.False);
            Assert.That(card.IsUnlocked, Is.True);
            Assert.That(card.Moves, Has.Count.EqualTo(3));
            Assert.That(card.Cost, Is.EqualTo(3));
        }

        [Test]
        public void TwoPlayedCardsCanUnlockACardInDiscard()
        {
            var locked = Card(Direction.Left, Direction.Right);
            Complete(ctx.LockCard(locked));
            Complete(ctx.DiscardCard(locked));
            var left = Card(Direction.Left);
            var right = Card(Direction.Right);
            ctx.Energy = 6;
            Assert.That(Complete(ctx.PlayCard(left, EffectTarget.None)), Is.True);
            Assert.That(locked.IsLocked, Is.True);
            Assert.That(Complete(ctx.PlayCard(right, EffectTarget.None)), Is.True);
            Assert.That(locked.IsUnlocked, Is.True);
        }

        [Test]
        public void DifferentMovementBreaksTheSequence()
        {
            var card = Card(Direction.Left, Direction.Right);
            Complete(ctx.LockCard(card));
            Step(Direction.Left);
            Step(Direction.Up);
            Step(Direction.Right);
            Assert.That(card.IsLocked, Is.True);
            Step(Direction.Left);
            Step(Direction.Right);
            Assert.That(card.IsUnlocked, Is.True);
        }

        [Test]
        public void HpTeleportAndBlockedStepsDoNotAdvanceUnlock()
        {
            var card = Card(Direction.Left);
            Complete(ctx.LockCard(card));
            Assert.That(Complete(ctx.Teleport(new Vector2Int(1, 2))), Is.True);
            Assert.That(card.IsLocked, Is.True);
            ctx.Player.Pos = new Vector2Int(0, 2);
            Assert.That(Complete(ctx.MovePlayer(EffectSource.System, Direction.Left)), Is.False);
            Assert.That(card.IsLocked, Is.True);
        }

        [Test]
        public void UnlockedCardIgnoresBlockedMovementAndZeroEnergyButRunsEffects()
        {
            var card = Card(Direction.Left, Direction.Left, Direction.Left);
            Complete(ctx.UnlockCard(card));
            ctx.Energy = 0;
            var position = ctx.Player.Pos;
            Assert.That(card.CanPlayFully(ctx), Is.True);
            Assert.That(card.PlayCost(ctx), Is.Zero);
            Assert.That(Complete(ctx.PlayCard(card, EffectTarget.None)), Is.True);
            Assert.That(ctx.Player.Pos, Is.EqualTo(position));
            Assert.That(ctx.Energy, Is.Zero);
            Assert.That(ctx.Enemies[0].Hp, Is.EqualTo(998));
        }

        [Test]
        public void UnlockedDashCompletesReuseLimitBeforeAutomaticLock()
        {
            var card = Card(Direction.Left, Direction.Left, Direction.Left);
            card.Keywords.Add(new Keyword_Lock { autoLock = true });
            card.Keywords.Add(new Keyword_Dash { maxReuseCount = 2 });
            Complete(ctx.UnlockCard(card));
            ctx.Energy = 0;
            var position = ctx.Player.Pos;
            Assert.That(Complete(ctx.PlayCard(card, EffectTarget.None)), Is.True);
            Assert.That(ctx.Player.Pos, Is.EqualTo(position));
            Assert.That(ctx.Enemies[0].Hp, Is.EqualTo(994));
            Assert.That(card.IsLocked, Is.True);
            Assert.That(card.IsUnlocked, Is.False);
            Step(Direction.Left);
            Assert.That(card.IsLocked, Is.True);
        }

        [Test]
        public void BattleCloneDoesNotChangeRunDeckOrAsset()
        {
            var original = Card(Direction.Left);
            original.Data.keywords.Add(new Keyword_Lock { autoLock = true });
            original = original.Data.CreateCard();
            var battleCard = original.Clone();
            Complete(ctx.UnlockCard(battleCard));
            Assert.That(original.IsLocked, Is.True);
            Assert.That(original.IsUnlocked, Is.False);
            Assert.That(original.Data.keywords[0], Is.TypeOf<Keyword_Lock>());
            Assert.That(battleCard.AutoLock, Is.True);
        }

        [Test]
        public void UnlockProgressSurvivesTurnChange()
        {
            var card = Card(Direction.Left, Direction.Right);
            Complete(ctx.LockCard(card));
            Step(Direction.Left);
            Complete(ctx.EndTurn());
            Step(Direction.Right);
            Assert.That(card.IsUnlocked, Is.True);
        }

        [Test]
        public void UnlockedTrailCardRunsEffectsWithoutLeavingObstacles()
        {
            var card = Card(Direction.Left, Direction.Right);
            card.Data.behavior = new ObstacleTrailCard();
            ctx.Piles.Hand.Remove(card);
            card = card.Data.CreateCard();
            ctx.Piles.Hand.Add(card);
            Complete(ctx.UnlockCard(card));
            var position = ctx.Player.Pos;
            ctx.Energy = 0;
            Assert.That(card.CanPlayFully(ctx), Is.True);
            Assert.That(Complete(ctx.PlayCard(card, EffectTarget.None)), Is.True);
            Assert.That(ctx.Player.Pos, Is.EqualTo(position));
            Assert.That(ctx.Obstacles, Is.Empty);
            Assert.That(ctx.Enemies[0].Hp, Is.EqualTo(998));
        }

        [Test]
        public void RelockingClearsPreviousProgressAndRemovesUnlock()
        {
            var card = Card(Direction.Left, Direction.Right);
            Complete(ctx.LockCard(card));
            Step(Direction.Left);
            Complete(ctx.UnlockCard(card));
            Complete(ctx.LockCard(card));
            Step(Direction.Right);
            Assert.That(card.IsLocked, Is.True);
            Assert.That(card.IsUnlocked, Is.False);
            Step(Direction.Left);
            Step(Direction.Right);
            Assert.That(card.IsUnlocked, Is.True);
        }

        [Test]
        public void OrdinaryCardStillMovesAndPaysEnergy()
        {
            var card = Card(Direction.Left);
            var position = ctx.Player.Pos;
            int energy = ctx.Energy;
            Assert.That(Complete(ctx.PlayCard(card, EffectTarget.None)), Is.True);
            Assert.That(ctx.Player.Pos, Is.EqualTo(position + Vector2Int.left));
            Assert.That(ctx.Energy, Is.EqualTo(energy - 3));
            Assert.That(ctx.Enemies[0].Hp, Is.EqualTo(998));
        }
    }
}
