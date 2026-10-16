using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 사이클 테스트용 임시 데이터 (메모리에서 생성). 에셋이 준비되면 BattleManager 인스펙터에 넣으면 대체된다.
    /// </summary>
    public static class SampleContent
    {
        public static List<CardData> CreateStarterDeck()
        {
            var deck = new List<CardData>();
            void Add(string id, Direction dir, int copies)
            {
                var c = Card(id, dir, 5);
                for (int i = 0; i < copies; i++) deck.Add(c);
            }
            // 카드 목록 시트의 기본 카드
            Add("Sit", Direction.Down, 3);
            Add("Jump", Direction.Up, 3);
            Add("Retreat", Direction.Left, 2);
            Add("Dash", Direction.Right, 2);
            return deck;
        }

        public static CardData Card(string id, Direction dir, int damage)
        {
            var c = ScriptableObject.CreateInstance<CardData>();
            c.name = id;
            c.id = id.ToLower();
            c.nameKey = $"card.{c.id}.name";
            c.descKey = $"card.{c.id}.desc";
            c.moves.Add(dir);
            c.type = CardType.Attack;
            c.target = TargetType.Unit;
            c.effects.Add(new DamageEffect { damage = damage });
            return c;
        }

        public static ObstacleData Tombstone()
        {
            var o = ScriptableObject.CreateInstance<ObstacleData>();
            o.name = "Tombstone";
            o.id = "tombstone";
            o.hp = 3;
            return o;
        }

        /// <summary>3x3용 묘지기: 턴마다 다른 타일에 사기 증가/즉시 피해, 묘비 배치.</summary>
        public static EnemyData Gravekeeper(ObstacleData tombstone)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.name = "Gravekeeper";
            e.id = "gravekeeper";
            e.startMorale = 26;

            e.pattern.Add(new IntentData
            {
                tileEffects =
                {
                    new TilePlacement { pos = new(0, 0), trigger = TileTrigger.TurnEndStanding, effects = { new GainMoraleEffect { amount = 5 } } },
                    new TilePlacement { pos = new(2, 2), trigger = TileTrigger.Pass, effects = { new DamagePlayerEffect { amount = 2 } } },
                },
                turnEndActions = { new PlaceObstacleEffect { obstacle = tombstone } },
            });
            e.pattern.Add(new IntentData
            {
                tileEffects =
                {
                    new TilePlacement { pos = new(1, 1), trigger = TileTrigger.TurnEndStanding, effects = { new GainMoraleEffect { amount = 5 } } },
                    new TilePlacement { pos = new(2, 0), trigger = TileTrigger.Arrive, effects = { new DamagePlayerEffect { amount = 3 } } },
                },
            });
            return e;
        }
    }
}
