using System;
using System.Collections.Generic;

namespace MoveCute
{
    /// <summary>한 판(런) 동안 유지되는 상태. 전투가 끝나면 BattleContext는 버리고 이것만 남는다.</summary>
    public class RunState
    {
        /// <summary>이 런의 시드. 같은 시드로 시작하면 셔플·적 행동·무작위 대상이 똑같이 재현된다.</summary>
        public readonly int Seed;
        /// <summary>런 전체가 공유하는 무작위. 무작위가 필요하면 <c>ctx.Rng</c>를 쓴다 (UnityEngine.Random 금지).</summary>
        public readonly Random Rng;

        public int Hp = 50;
        public int MaxHp = 50;
        public int MaxEnergy = 4;
        public int Gold;
        public int Act = 1;   // 1막 3x3, 2막 4x4, 3막 5x5

        public readonly List<CardInstance> Deck = new();
        public readonly List<Relic> Relics = new();

        public int BoardSize => Act + 2;

        /// <param name="seed">null이면 무작위 시드 (1 이상이라 BattleManager.seed에 그대로 적어 둘 수 있다)</param>
        public RunState(int? seed = null)
        {
            Seed = seed ?? Math.Max(1, Guid.NewGuid().GetHashCode() & int.MaxValue);
            Rng = new Random(Seed);
        }
    }
}
