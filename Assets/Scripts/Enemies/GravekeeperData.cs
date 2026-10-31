using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [참고용 예시] 묘지기.
    ///   기도 — 플레이어가 지금 선 칸. 그 칸에서 턴을 끝내면 사기 회복 → 플레이어를 움직이게 만든다
    ///   저주 — 무작위 빈 칸. 지나가면 피해. 사기가 절반 이하로 떨어지면 더 많아진다
    ///   묘비 — 몇 턴마다 턴 종료 시 빈 칸에 묘비를 세운다 (칸만 막는 장애물)
    ///
    /// 적 하나 = 이 파일 하나. 두 클래스로 나눈다:
    ///   GravekeeperData — 에셋에 저장되는 숫자. 기획자가 인스펙터에서 조정한다 (파일 이름과 같아야 함)
    ///   Gravekeeper     — 전투 중 행동. 매 턴 PlanIntent로 예고를 정한다
    ///
    /// 이 파일에서 볼 것:
    ///   1. 좌표를 박아 두지 않고 보드·플레이어 위치에서 계산 → 3x3·4x4·5x5 어디서나 동작
    ///   2. 상태(사기, 턴 수)에 따라 행동이 바뀐다
    ///   3. 무작위 선택은 ctx.Rng (시드 RNG → 같은 시드면 같은 예고. UnityEngine.Random 금지)
    ///   4. 예고는 IntentData로 만들어 돌려주기만 한다 → UI 공개·타일 배치·턴 종료 실행은 Enemy 기본 구현이 처리
    ///   5. 효과는 그때그때 new로 만들고, 숫자는 Data에서 가져온다
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Enemy/Gravekeeper", fileName = "Enemy_Gravekeeper")]
    public class GravekeeperData : EnemyData
    {
        [Header("기도 (플레이어 발밑)")]
        [Tooltip("그 칸에서 턴을 끝내면 오르는 사기")]
        public int prayMorale = 5;

        [Header("저주 (무작위 빈 칸)")]
        [Min(0)] public int curseTiles = 1;
        [Tooltip("저주 칸을 지나가면 받는 피해")]
        public int curseDamage = 2;
        [Tooltip("사기가 시작값의 절반 이하일 때 늘어나는 저주 칸 수")]
        public int desperateExtraCurses = 1;

        [Header("묘비")]
        public ObstacleData tombstone;
        [Tooltip("몇 턴마다 턴 종료 시 묘비를 세우는지 (1턴부터)")]
        [Min(1)] public int tombstoneEvery = 2;

        public override Enemy CreateEnemy() => new Gravekeeper(this);
    }

    public class Gravekeeper : Enemy
    {
        readonly GravekeeperData data;

        public Gravekeeper(GravekeeperData data) : base(data) { this.data = data; }

        bool Desperate => Hp * 2 <= data.startMorale;

        protected override IntentData PlanIntent(BattleContext ctx)
        {
            var intent = new IntentData();
            var player = ctx.Player.Pos;

            // 기도: 플레이어 발밑. 가만히 있으면 사기를 회복하니 움직이게 된다
            intent.AddTile(player, TileTrigger.TurnEndStanding, new GainMoraleEffect { amount = data.prayMorale });

            // 저주: 플레이어 칸을 뺀 빈 칸 중 무작위. 몰리면 더 많이
            int curses = data.curseTiles + (Desperate ? data.desperateExtraCurses : 0);
            foreach (var pos in RandomEmptyTiles(ctx, curses, except: player))
                intent.AddTile(pos, TileTrigger.Pass, new DamagePlayerEffect { amount = data.curseDamage });

            // 묘비: 1턴부터 N턴마다
            if (data.tombstone != null && (ctx.Turn - 1) % data.tombstoneEvery == 0)
                intent.AddTurnEnd(new PlaceObstacleEffect { obstacle = data.tombstone });

            return intent;
        }

        /// <summary>장애물이 없는 칸 중 무작위로 count개 (except 칸 제외).</summary>
        static List<Vector2Int> RandomEmptyTiles(BattleContext ctx, int count, Vector2Int except) =>
            ctx.Rng.Shuffled(ctx.Board.AllTiles()
                    .Where(t => t.Passable && t.Pos != except)
                    .Select(t => t.Pos))
                .Take(count)
                .ToList();
    }
}
