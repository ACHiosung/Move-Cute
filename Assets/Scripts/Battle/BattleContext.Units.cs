using System.Linq;
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
            if (amount < 0) MoraleDealt += Mathf.Min(before, -amount);   // 깎은 사기 기록 (HP 회복 보상용, 전투 흐름)
            if (amount >= 0 || !BattleTestOptions.InfiniteEnemyMorale)   // [TestingTools] 적 사기 무한
            enemy.Hp = Mathf.Max(0, enemy.Hp + amount);
            enemy.MaxHp = Mathf.Max(enemy.MaxHp, enemy.Hp);
            Log($"{enemy} 사기 {(amount >= 0 ? "+" : "")}{amount} → {enemy.Hp}");
            await Presenter.MoraleChanged(enemy, enemy.Hp - before);
        }

        /// <summary>플레이어 HP 피해. 0이 되면 즉시 패배.</summary>
        public Awaitable DamagePlayer(EffectSource src, int amount) => DamagePlayer(src, (Damage)amount);

        public async Awaitable AddStatus(EffectSource src, Unit target, string statusId, int stacks)
        {
            if (IsOver || target == null) return;
            if (statusId == StatusIds.Hesitation && target is not Enemy) return;   // 주저는 적에게만 (장애물·플레이어 불가)
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
        // 공격 측은 Damage에 담겨 오고(효과가 정함), 유물·버프의 공격 측 곱(IDealtDamageModifier, 세계의 경계)과
        // 피격 측 곱(주저 등)은 여기서 곱한다. 내림은 CalculateDamage에서 한 번만.

        /// <summary>주저 수치 1당 받는 피해 증가량 (%).</summary>
        public const int HesitationPercentPerStack = 5;

        /// <summary>주저 효과 증가 버프(StatusIds.HesitationBoost) 수치 1당 주저 효과 증가량 (%).</summary>
        public const int HesitationBoostPercentPerStack = 20;

        /// <summary>이번 전투 동안 주저 효과 증가량 (%). 버프 외의 효과가 직접 더할 때 쓴다.</summary>
        public int HesitationEffectBonusPercent;

        /// <summary>DealDamage(int)의 본체. 공격 측 계산이 끝난 Damage를 받아 피격 측 곱과 내림을 거쳐 적용한다.</summary>
        public async Awaitable DealDamage(EffectSource src, Unit target, Damage damage)
        {
            if (IsOver || target == null) return;
            if (target == Player) { await DamagePlayer(src, damage); return; }

            int amount = CalculateDamage(src, target, damage);
            if (amount <= 0) return;
            if (target is Enemy) MoraleDealt += Mathf.Min(target.Hp, amount);   // 깎은 사기 기록 (HP 회복 보상용, 전투 흐름)
            if (!(target is Enemy && BattleTestOptions.InfiniteEnemyMorale))   // [TestingTools] 적 사기 무한
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
            amount = LosePlayerHp(src, amount, PlayerHpLossKind.Damage);
            if (amount <= 0) return;
            Log($"{src} → 플레이어 피해 {amount} (HP {Player.Hp})");
            await Presenter.Damaged(Player, amount);
            if (Player.Hp <= 0) { await EndBattle(false); return; }
            await Events.Damaged.Raise(Player, amount);
        }

        /// <summary>
        /// 플레이어 HP 감소의 공통 경로. 피해(DamagePlayer)와 순간이동 비용(Teleport)이 모두 여기를 지난다.
        ///   1) 유물의 합·곱(IPlayerHpLossModifier)을 따로 모은다: 잃는 양 = (양 + 합) × 곱. 장착 순서와 무관하다.
        ///      곱으로 바뀐 양은 소수점을 버린다 (두꺼운 패딩 10%: 15 → 감소 1.5를 버려 1 → 14)
        ///   2) 그래도 HP가 0 이하가 되면 버티기 유물(IPlayerLethalGuard)에 묻는다 (기계 심장: HP 1 남기고 생존). 하나만 발동.
        /// HP를 바꾸고 실제로 잃은 양을 돌려준다. 연출·패배 처리는 부르는 쪽이 한다.
        /// </summary>
        int LosePlayerHp(EffectSource src, int amount, PlayerHpLossKind kind)
        {
            if (amount <= 0) return 0;
            var relics = Run.Relics.Where(r => r.Enabled).ToList();

            int add = 0;
            decimal multiplier = 1m;
            foreach (var m in relics.OfType<IPlayerHpLossModifier>())
            {
                add += m.HpLossAdd(kind);
                multiplier *= m.HpLossMultiplier(kind);
            }
            int loss = Mathf.Max(0, amount + add);
            loss = Mathf.Max(0, loss + (int)decimal.Truncate(loss * (multiplier - 1m)));   // 곱으로 바뀐 양은 0 쪽으로 버림
            if (loss != amount) Log($"{src}: 잃는 HP {amount} → {loss} (유물)");
            if (BattleTestOptions.InfinitePlayerHp) return loss;   // [TestingTools] 내 HP 무한 (버티기 유물도 쓰지 않음)

            if (Player.Hp - loss <= 0)
            {
                foreach (var guard in relics.OfType<IPlayerLethalGuard>())
                {
                    if (!guard.TrySurvive(kind, out int leaveHp)) continue;
                    leaveHp = Mathf.Max(1, leaveHp);
                    loss = Mathf.Max(0, Player.Hp - leaveHp);
                    Log($"{guard}: 치명적인 HP 감소를 버팀 (HP {leaveHp} 남김)");
                    break;
                }
            }
            Player.Hp = Mathf.Max(0, Player.Hp - loss);
            return loss;
        }

        /// <summary>최종 피해량. 공격 측 값 × 유물·버프의 공격 측 곱 × 피격 측 곱을 계산하고 마지막에 한 번만 내림한다.</summary>
        public int CalculateDamage(EffectSource src, Unit target, Damage damage)
        {
            decimal value = damage.Value * DealtDamageMultiplier(src, target);
            if (target != null) value *= TakenDamageMultiplier(target);
            if (value <= 0) return 0;
            return (int)decimal.Floor(value);
        }

        /// <summary>
        /// 유물·버프가 정하는 공격 측 곱 (세계의 경계: 카드 피해 ×0.8). 여러 개면 서로 곱한다.
        /// 어떤 피해에 적용할지는 각 유물·버프가 src·target으로 정한다 (IDealtDamageModifier).
        /// </summary>
        public decimal DealtDamageMultiplier(EffectSource src, Unit target)
        {
            decimal multiplier = 1m;
            foreach (var m in Modifiers<IDealtDamageModifier>()) multiplier *= m.DealtDamageMultiplier(src, target);
            return multiplier;
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
        /// 주저 배율 = 1 + 수치 × 5% × (1 + 주저 효과 증가%).
        /// 주저 효과 증가% = 플레이어의 주저 효과 증가 버프 수치 × 20 + HesitationEffectBonusPercent.
        /// 예: 주저 20, 버프 2 → 1 + 1.0 × 1.4 = 2.4배
        /// </summary>
        public decimal HesitationMultiplier(int stacks)
        {
            decimal percent = stacks * HesitationPercentPerStack;
            int bonus = GetStatusStacks(Player, StatusIds.HesitationBoost) * HesitationBoostPercentPerStack + HesitationEffectBonusPercent;
            percent *= 1m + bonus / 100m;
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
        /// 상태가 붙은 직후 반응. AddStatus가 부른다.
        /// 의욕 상실: 적에게 주저가 붙으면 부여량 × 플레이어의 의욕 상실 수치만큼 그 적에게 피해.
        /// 채찍 유물: 부여한 주저 양을 센다.
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
                foreach (var whip in Run.Relics.OfType<HesitationCountRelic>().ToList())
                    await whip.OnHesitationApplied(this, stacks);
            }
        }

        /// <summary>
        /// 카드 효과로 주저를 부여할 때 더하는 양 (인공 눈물 유물). 출처가 카드가 아니면 0 (MOVE CUTE 같은 버프는 제외).
        /// </summary>
        public int CardHesitationBonus(EffectSource src) =>
            src.Owner is CardInstance
                ? Run.Relics.OfType<HesitationCardBonusRelic>().Where(r => r.Enabled).Sum(r => r.Bonus)
                : 0;
    }
}
