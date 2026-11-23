using System;
using System.Collections.Generic;

namespace MoveCute
{
    /// <summary>
    /// [테스트용] 전투 테스트 옵션. 에디터 창(메뉴 MoveCute > Debug > Testing Tools)이 켜고 끈다.
    /// 값은 에디터 PC에 저장되고(EditorPrefs), 빌드에서는 모두 꺼져 있다.
    /// 무한 옵션은 플레이 중 바꾸면 바로 적용된다. 순서대로 뽑기는 다음 전투부터, 모든 유물 넣기는 새 런부터 적용된다.
    /// </summary>
    public static class BattleTestOptions
    {
        /// <summary>뽑을 카드 더미를 섞지 않고 시작 덱(Run.Deck)에 넣은 순서대로 뽑는다. 묘지를 다시 섞을 때는 평소처럼 섞는다.</summary>
        public static bool OrderedDraw;

        /// <summary>새 런을 시작할 때 BattleManager.relics 대신 Assets/Data/Relics의 모든 유물 에셋(_Test 폴더 제외)을 꺼진 상태로 넣는다.</summary>
        public static bool AllRelicsOff;

        /// <summary>에너지가 줄지 않는다 (카드 비용·순간이동). 켜는 순간 최대 에너지로 채운다.</summary>
        public static bool InfiniteEnergy;

        /// <summary>제한 턴이 끝나지 않는다 (마지막 턴 처리·남은 사기 피해 없음).</summary>
        public static bool InfiniteTurns;

        /// <summary>플레이어 HP가 줄지 않는다 (피해·순간이동). 피해 로그와 연출은 그대로 나온다.</summary>
        public static bool InfinitePlayerHp;

        /// <summary>적 사기가 줄지 않는다 (피해·사기 감소 효과). 장애물은 평소처럼 피해를 받는다. 깎은 사기 기록(MoraleDealt)은 그대로 센다.</summary>
        public static bool InfiniteEnemyMorale;

        /// <summary>모든 유물 에셋 목록. AssetDatabase는 에디터 전용이라 에디터 창 쪽에서 채운다. 빌드에서는 null.</summary>
        public static Func<IEnumerable<RelicData>> FindAllRelics;
    }
}
