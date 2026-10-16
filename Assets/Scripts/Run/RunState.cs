using System.Collections.Generic;

namespace MoveCute
{
    /// <summary>한 판(런) 동안 유지되는 상태. 전투가 끝나면 BattleContext는 버리고 이것만 남는다.</summary>
    public class RunState
    {
        public int Hp = 50;
        public int MaxHp = 50;
        public int MaxEnergy = 4;
        public int Gold;
        public int Act = 1;   // 1막 3x3, 2막 4x4, 3막 5x5

        public readonly List<CardInstance> Deck = new();
        public readonly List<Relic> Relics = new();

        public int BoardSize => Act + 2;
    }
}
