using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>
    /// [TestingTools] 전투 테스트 옵션 창. 메뉴 MoveCute > Debug > Testing Tools.
    /// 체크 상태는 이 PC의 EditorPrefs에 저장되어 다음 플레이에도 유지된다. 씬은 건드리지 않는다.
    /// 실제 동작은 BattleTestOptions를 읽는 쪽에 있다 (게임 코드의 "// [TestingTools]" 줄, README.md).
    /// </summary>
    public class TestingToolsWindow : EditorWindow
    {
        [MenuItem("MoveCute/Debug/Testing Tools")]
        static void Open() => GetWindow<TestingToolsWindow>("Testing Tools");

        void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("덱 · 유물 (새 런·다음 전투부터)", EditorStyles.boldLabel);
            TestingToolsPrefs.Toggle(ref BattleTestOptions.OrderedDraw, TestingToolsPrefs.OrderedDraw,
                "시작 덱 순서대로 뽑기", "섞지 않고 시작 덱에 넣은 순서대로 뽑는다 (첫 턴 6장도). 다음 전투부터 적용");
            using (new EditorGUILayout.HorizontalScope())
            {
                TestingToolsPrefs.DeckPresetToggle();
                if (GUILayout.Button("덱 편집 창", GUILayout.Width(80))) DeckBuilderWindow.Open();
            }
            TestingToolsPrefs.Toggle(ref BattleTestOptions.AllRelicsOff, TestingToolsPrefs.AllRelicsOff,
                "모든 유물 넣기 (꺼진 상태)", "BattleManager.relics 대신 Assets/Data/Relics의 모든 유물(_Test 제외)을 꺼진 채로 넣는다. 새 런부터 적용. 전투 화면의 유물 아이콘을 클릭해 켜고 끈다");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("무한 (플레이 중 바로 적용)", EditorStyles.boldLabel);
            bool energyBefore = BattleTestOptions.InfiniteEnergy;
            TestingToolsPrefs.Toggle(ref BattleTestOptions.InfiniteEnergy, TestingToolsPrefs.InfiniteEnergy,
                "에너지 무한", "카드 비용·순간이동에 에너지를 쓰지 않는다. 켜는 순간 최대 에너지로 채운다");
            TestingToolsPrefs.Toggle(ref BattleTestOptions.InfiniteTurns, TestingToolsPrefs.InfiniteTurns,
                "턴 수 무한", "제한 턴이 끝나지 않는다");
            TestingToolsPrefs.Toggle(ref BattleTestOptions.InfinitePlayerHp, TestingToolsPrefs.InfinitePlayerHp,
                "내 HP 무한", "피해·순간이동으로 HP가 줄지 않는다 (로그·연출은 그대로)");
            TestingToolsPrefs.Toggle(ref BattleTestOptions.InfiniteEnemyMorale, TestingToolsPrefs.InfiniteEnemyMorale,
                "적 사기 무한", "피해·사기 감소로 적 사기가 줄지 않는다 (장애물은 평소대로)");

            var manager = TestingToolsPrefs.PlayingManager;
            if (!energyBefore && BattleTestOptions.InfiniteEnergy && manager != null && manager.Ctx != null)
                manager.Ctx.ApplyTestOptions().Forget();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(manager == null))
                    if (GUILayout.Button("새 런 시작")) TestingToolsPrefs.StartNewRun();
                if (GUILayout.Button("유물 켜기·끄기 창")) GetWindow<RelicToggleWindow>("Relic Toggle");
            }
            if (manager == null) EditorGUILayout.HelpBox("새 런 시작은 플레이 중에만 쓸 수 있습니다.", MessageType.None);
        }
    }

    /// <summary>
    /// [TestingTools] 테스트 옵션 저장·불러오기. 에디터가 켜질 때·스크립트가 다시 로드될 때(플레이 시작 포함)
    /// 저장된 체크 상태를 BattleTestOptions에 넣는다.
    /// </summary>
    [InitializeOnLoad]
    static class TestingToolsPrefs
    {
        public const string OrderedDraw = "MoveCute.Test.OrderedDraw";
        public const string AllRelicsOff = "MoveCute.Test.AllRelicsOff";
        public const string UseDeckPreset = "MoveCute.Test.UseDeckPreset";
        public const string DeckPresetGuid = "MoveCute.Test.DeckPresetGuid";
        public const string InfiniteEnergy = "MoveCute.Test.InfiniteEnergy";
        public const string InfiniteTurns = "MoveCute.Test.InfiniteTurns";
        public const string InfinitePlayerHp = "MoveCute.Test.InfinitePlayerHp";
        public const string InfiniteEnemyMorale = "MoveCute.Test.InfiniteEnemyMorale";

        static TestingToolsPrefs()
        {
            BattleTestOptions.OrderedDraw = EditorPrefs.GetBool(OrderedDraw);
            BattleTestOptions.AllRelicsOff = EditorPrefs.GetBool(AllRelicsOff);
            BattleTestOptions.UseDeckPreset = EditorPrefs.GetBool(UseDeckPreset);
            string guid = EditorPrefs.GetString(DeckPresetGuid);
            BattleTestOptions.DeckPreset = string.IsNullOrEmpty(guid) ? null
                : AssetDatabase.LoadAssetAtPath<DeckPreset>(AssetDatabase.GUIDToAssetPath(guid));
            BattleTestOptions.InfiniteEnergy = EditorPrefs.GetBool(InfiniteEnergy);
            BattleTestOptions.InfiniteTurns = EditorPrefs.GetBool(InfiniteTurns);
            BattleTestOptions.InfinitePlayerHp = EditorPrefs.GetBool(InfinitePlayerHp);
            BattleTestOptions.InfiniteEnemyMorale = EditorPrefs.GetBool(InfiniteEnemyMorale);
            BattleTestOptions.FindAllRelics = FindAllRelics;
        }

        /// <summary>플레이 중이면 BattleManager, 아니면 null.</summary>
        public static BattleManager PlayingManager => EditorApplication.isPlaying ? BattleManager.Instance : null;

        public static void StartNewRun()
        {
            var manager = PlayingManager;
            if (manager == null) return;
            manager.NewRun();
            manager.StartBattle().Forget();
        }

        public static void Toggle(ref bool value, string prefKey, string label, string tooltip)
        {
            bool next = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), value);
            if (next == value) return;
            value = next;
            EditorPrefs.SetBool(prefKey, next);
        }

        /// <summary>"덱 프리셋 사용" 체크 (Testing Tools·Deck Builder 두 창이 같이 쓴다).</summary>
        public static void DeckPresetToggle()
        {
            string name = BattleTestOptions.DeckPreset != null ? BattleTestOptions.DeckPreset.name : "없음";
            Toggle(ref BattleTestOptions.UseDeckPreset, UseDeckPreset, $"덱 프리셋 사용 ({name})",
                "BattleManager.startingDeck 대신 Deck Builder에서 고른 프리셋으로 덱을 만든다. 새 런부터 적용");
        }

        public static void SetDeckPreset(DeckPreset preset)
        {
            BattleTestOptions.DeckPreset = preset;
            EditorPrefs.SetString(DeckPresetGuid,
                preset != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset)) : "");
        }

        /// <summary>Assets/Data/Relics 아래 모든 유물 에셋 (_Test 폴더 제외, 버프 에셋 제외). 폴더(테마)·이름 순.</summary>
        static IEnumerable<RelicData> FindAllRelics() =>
            AssetDatabase.FindAssets("t:RelicData", new[] { "Assets/Data/Relics" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.Contains("/_Test/"))
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<RelicData>)
                .Where(data => data != null)
                .ToList();
    }
}
