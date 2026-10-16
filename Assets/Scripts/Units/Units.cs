using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>HP가 있는 객체 (카드 대상 1번). 적에게 Hp는 사기.</summary>
    public abstract class Unit
    {
        public int Hp;
        public readonly List<Status> Statuses = new();

        public abstract string DisplayName { get; }
        public override string ToString() => DisplayName;
    }

    /// <summary>상태이상 (죄책감 등). 세부 동작은 담당자가 확장.</summary>
    public class Status
    {
        public string Id;
        public int Stacks;

        public Status(string id, int stacks) { Id = id; Stacks = stacks; }
        public override string ToString() => $"{Id}({Stacks})";
    }

    public class PlayerUnit : Unit
    {
        public Vector2Int Pos;
        public int MaxHp;

        public override string DisplayName => "Player";
    }
}
