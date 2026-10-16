using UnityEngine;

namespace MoveCute
{
    /// <summary>상호작용 계약 — 유닛 (담당: 효과/상태). 함수 이름·인자는 합의 없이 바꾸지 않는다.</summary>
    public partial class BattleContext
    {
        /// <summary>적·장애물에게 피해. 플레이어가 대상이면 DamagePlayer로 넘긴다. 장애물 HP가 0이 되면 파괴까지 끝내고 돌아온다.</summary>
        public async Awaitable DealDamage(EffectSource src, Unit target, int amount)
        {
            if (IsOver || target == null) return;
            if (target == Player) { await DamagePlayer(src, amount); return; }

            amount = ModifyDamage(src, target, amount);
            if (amount <= 0) return;
            target.Hp = Mathf.Max(0, target.Hp - amount);
            Log($"{src} → {target} 피해 {amount} (남은 {target.Hp})");
            await Presenter.Damaged(target, amount);
            await Events.Damaged.Raise(target, amount);

            if (target is Obstacle o && o.Hp <= 0) await DestroyObstacle(src, o);
        }

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
        public async Awaitable DamagePlayer(EffectSource src, int amount)
        {
            if (IsOver) return;
            amount = ModifyDamage(src, Player, amount);
            if (amount <= 0) return;
            Player.Hp = Mathf.Max(0, Player.Hp - amount);
            Log($"{src} → 플레이어 피해 {amount} (HP {Player.Hp})");
            await Presenter.Damaged(Player, amount);
            if (Player.Hp <= 0) { await EndBattle(false); return; }
            await Events.Damaged.Raise(Player, amount);
        }

        public async Awaitable AddStatus(EffectSource src, Unit target, string statusId, int stacks)
        {
            if (IsOver || target == null) return;
            var s = target.Statuses.Find(x => x.Id == statusId);
            if (s != null) s.Stacks += stacks;
            else target.Statuses.Add(new Status(statusId, stacks));
            Log($"{target}에게 {statusId} {stacks} 부여");
            await Presenter.StatusAdded(target, statusId, stacks);
        }

        /// <summary>피해량 보정 (죄책감, 유물 등). 계산만 하므로 동기. TODO(효과/상태): 보정 등록 구조.</summary>
        public int ModifyDamage(EffectSource src, Unit target, int baseAmount) => baseAmount;
    }
}
