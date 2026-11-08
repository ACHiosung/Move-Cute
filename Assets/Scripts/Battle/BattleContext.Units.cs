using UnityEngine;

namespace MoveCute
{
    /// <summary>상호작용 계약 — 유닛 (담당: 효과/상태). 함수 이름·인자는 합의 없이 바꾸지 않는다.</summary>
    public partial class BattleContext
    {
        /// <summary>적·장애물에게 피해. 플레이어가 대상이면 DamagePlayer로 넘긴다. 장애물 HP가 0이 되면 파괴까지 끝내고 돌아온다.</summary>
        public Awaitable DealDamage(EffectSource src, Unit target, int amount) => DealDamage(src, target, (Damage)amount);

        /// <summary>적 사기 증감. 상한 없음, 0 미만 없음.</summary>
        public async Awaitable ChangeMorale(EffectSource src, Enemy enemy, int amount)
        {
            if (IsOver || enemy == null) return;
            int before = enemy.Hp;
            enemy.Hp = Mathf.Max(0, enemy.Hp + amount);
            Log($"{enemy} 사기 {(amount >= 0 ? "+" : "")}{amount} → {enemy.Hp}");
            await Presenter.MoraleChanged(enemy, enemy.Hp - before);
        }

        /// <summary>플레이어 HP 피해. 0이 되면 즉시 패배.</summary>
        public Awaitable DamagePlayer(EffectSource src, int amount) => DamagePlayer(src, (Damage)amount);

        public async Awaitable AddStatus(EffectSource src, Unit target, string statusId, int stacks)
        {
            if (IsOver || target == null) return;
            var s = target.Statuses.Find(x => x.Id == statusId);
            if (s != null) s.Stacks += stacks;
            else target.Statuses.Add(new Status(statusId, stacks));
            Log($"{target}에게 {statusId} {stacks} 부여");
            await Presenter.StatusAdded(target, statusId, stacks);
            await AfterStatusAdded(src, target, statusId, stacks);
        }

        /// <summary>피해량 보정 (주저, 유물 등). 계산만 하므로 동기. 정수 피해를 CalculateDamage로 넘긴다.</summary>
        public int ModifyDamage(EffectSource src, Unit target, int baseAmount) => CalculateDamage(src, target, baseAmount);

        // ───────────── 피해 계산 공식 ─────────────
        // 최종 피해 = 내림( (기본 + 합연산) × 공격 측 곱 × 피격 측 곱 )
        // 공격 측은 Damage에 담겨 오고, 피격 측 곱(주저 등)은 여기서 곱한다. 내림은 CalculateDamage에서 한 번만.

        /// <summary>주저 기본 증가량 (%).</summary>
        public const int HesitationBasePercent = 20;

        /// <summary>주저 수치가 남은 턴 수보다 클 때, 그 차이 1당 추가 증가량 (%).</summary>
        public const int HesitationExcessPercentPerStack = 5;

        /// <summary>이번 전투 동안 주저 효과 증가량 (%). '주저의 효과가 n% 증가' 카드가 더한다.</summary>
        public int HesitationEffectBonusPercent;

        /// <summary>DealDamage(int)의 본체. 공격 측 계산이 끝난 Damage를 받아 피격 측 곱과 내림을 거쳐 적용한다.</summary>
        public async Awaitable DealDamage(EffectSource src, Unit target, Damage damage)
        {
            if (IsOver || target == null) return;
            if (target == Player) { await DamagePlayer(src, damage); return; }

            int amount = CalculateDamage(src, target, damage);
            if (amount <= 0) return;
            target.Hp = Mathf.Max(0, target.Hp - amount);
            Log($"{src} → {target} 피해 {amount} (남은 {target.Hp})");
            await Presenter.Damaged(target, amount);
            await Events.Damaged.Raise(target, amount);

            if (target is Obstacle o && o.Hp <= 0) await DestroyObstacle(src, o);
        }

        /// <summary>DamagePlayer(int)의 본체.</summary>
        public async Awaitable DamagePlayer(EffectSource src, Damage damage)
        {
            if (IsOver) return;
            int amount = CalculateDamage(src, Player, damage);
            if (amount <= 0) return;
            Player.Hp = Mathf.Max(0, Player.Hp - amount);
            Log($"{src} → 플레이어 피해 {amount} (HP {Player.Hp})");
            await Presenter.Damaged(Player, amount);
            if (Player.Hp <= 0) { await EndBattle(false); return; }
            await Events.Damaged.Raise(Player, amount);
        }

        /// <summary>최종 피해량. 공격 측 값 × 피격 측 곱을 계산하고 마지막에 한 번만 내림한다.</summary>
        public int CalculateDamage(EffectSource src, Unit target, Damage damage)
        {
            decimal value = damage.Value;
            if (target != null) value *= TakenDamageMultiplier(target);
            if (value <= 0) return 0;
            return (int)decimal.Floor(value);
        }

        /// <summary>피격 측 곱. 받는 피해를 바꾸는 상태가 여러 개면 서로 곱한다.</summary>
        public decimal TakenDamageMultiplier(Unit target)
        {
            decimal multiplier = 1m;
            foreach (var s in target.Statuses)
            {
                if (s.Stacks <= 0) continue;
                if (s.Id == StatusIds.Hesitation) multiplier *= HesitationMultiplier(s.Stacks);
            }
            return multiplier;
        }

        /// <summary>
        /// 주저 배율 = 1 + (20% + max(0, 수치 − 남은 턴) × 5%) × (1 + 주저 효과 증가%).
        /// 남은 턴은 지금 턴을 포함한다 (마지막 턴이면 1).
        /// </summary>
        public decimal HesitationMultiplier(int stacks)
        {
            int remainingTurns = Mathf.Max(0, Config.turnLimit - Turn + 1);
            int excess = Mathf.Max(0, stacks - remainingTurns);
            decimal percent = HesitationBasePercent + excess * HesitationExcessPercentPerStack;
            percent *= 1m + HesitationEffectBonusPercent / 100m;
            return 1m + percent / 100m;
        }

        // ───────────── 상태 보조 ─────────────

        /// <summary>대상이 가진 그 상태의 수치 합. 없으면 0.</summary>
        public int GetStatusStacks(Unit target, string statusId)
        {
            if (target == null) return 0;
            int sum = 0;
            foreach (var s in target.Statuses)
                if (s.Id == statusId) sum += s.Stacks;
            return sum;
        }

        /// <summary>
        /// 같은 Id가 있어도 합치지 않고 새 상태를 하나 더 붙인다 (MOVE CUTE처럼 카드마다 따로 세는 버프).
        /// 붙인 상태를 돌려준다. 전투가 끝났거나 대상이 없으면 null.
        /// </summary>
        public async Awaitable<Status> AddSeparateStatus(EffectSource src, Unit target, string statusId, int stacks)
        {
            if (IsOver || target == null) return null;
            var s = new Status(statusId, stacks);
            target.Statuses.Add(s);
            Log($"{target}에게 {statusId} {stacks} 부여 (별도)");
            await Presenter.StatusAdded(target, statusId, stacks);
            return s;
        }

        /// <summary>
        /// 상태가 붙은 직후 반응. AddStatus가 부른다.
        /// 의욕 상실: 적에게 주저가 붙으면 부여량 × 플레이어의 의욕 상실 수치만큼 그 적에게 피해.
        /// </summary>
        async Awaitable AfterStatusAdded(EffectSource src, Unit target, string statusId, int stacks)
        {
            if (IsOver || stacks <= 0) return;
            if (statusId == StatusIds.Hesitation && target is Enemy)
            {
                int demotivation = GetStatusStacks(Player, StatusIds.Demotivation);
                if (demotivation > 0)
                {
                    var buff = Player.Statuses.Find(x => x.Id == StatusIds.Demotivation);
                    await DealDamage(new EffectSource(buff), target, stacks * demotivation);
                }
            }
        }
    }
}
