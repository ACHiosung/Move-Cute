namespace MoveCute
{
    /// <summary>
    /// 공격 측 피해 값. 소수점은 여기서 버리지 않는다.
    ///   1. 합연산: Base에 더한다 (기본 피해, 폭탄 피해 유물 +n 등)
    ///   2. 공격 측 곱연산: Multiplier에 곱한다 (예술 폭탄 1.5^k 등)
    /// 피격 측 곱연산(주저 등)과 마지막 내림은 BattleContext.CalculateDamage가 한다.
    /// Add/Multiply를 어떤 순서로 불러도 Value = Base × Multiplier이므로 합연산이 먼저 적용된다.
    /// 소수 오차(20 × 1.15 = 22.999…)를 막기 위해 decimal을 쓴다.
    /// </summary>
    public readonly struct Damage
    {
        public readonly decimal Base;
        public readonly decimal Multiplier;

        public Damage(decimal baseAmount, decimal multiplier = 1m)
        {
            Base = baseAmount;
            Multiplier = multiplier;
        }

        /// <summary>내림 전 공격 측 피해.</summary>
        public decimal Value => Base * Multiplier;

        public Damage Add(decimal flat) => new(Base + flat, Multiplier);
        public Damage Multiply(decimal factor) => new(Base, Multiplier * factor);

        public static implicit operator Damage(int amount) => new(amount);

        public override string ToString() => Multiplier == 1m ? $"{Base}" : $"{Base}×{Multiplier}";
    }
}
