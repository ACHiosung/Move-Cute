using UnityEditor;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>
    /// [테스트용] 플레이 중 장착한 유물을 켜고 끄는 에디터 창. 메뉴 MoveCute > Debug > Relic Toggle.
    /// 게임 화면에는 아무것도 그리지 않는다 (유물 표시는 UI 담당 영역). 체크를 끄면 Relic.Enabled = false가 되어
    /// 전투 도중에도 바로 적용된다. 런을 새로 시작하면 유물이 새로 만들어져 다시 모두 켜진다.
    /// 누적 수치가 있는 유물(채찍)은 이름 옆에 "쌓인 수치/발동 기준"을 보여 준다.
    /// </summary>
    public class RelicToggleWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("MoveCute/Debug/Relic Toggle")]
        static void Open() => GetWindow<RelicToggleWindow>("Relic Toggle");

        // 플레이 중 수치(채찍 등)가 바뀌는 것을 따라가도록 계속 다시 그린다
        void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }

        void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 쓸 수 있습니다. 장착할 유물은 씬의 BattleManager.relics에서 고릅니다.", MessageType.Info);
                return;
            }
            var relics = BattleManager.Instance != null ? BattleManager.Instance.Run?.Relics : null;
            if (relics == null || relics.Count == 0)
            {
                EditorGUILayout.HelpBox("장착한 유물이 없습니다.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("모두 켜기")) foreach (var r in relics) r.Enabled = true;
                if (GUILayout.Button("모두 끄기")) foreach (var r in relics) r.Enabled = false;
            }
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var relic in relics)
            {
                string label = relic.ToString();
                string count = CounterText(relic);
                if (count != null) label += $"  ({count})";
                relic.Enabled = EditorGUILayout.ToggleLeft(new GUIContent(label, relic.Data != null ? relic.Data.Description : ""), relic.Enabled);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>누적 수치가 있는 유물의 표시 문자열. 없으면 null (채찍: 쌓인 주저/발동 기준)</summary>
        static string CounterText(Relic relic)
        {
            if (relic is HesitationCountRelic whip && relic.Data is HesitationCountRelicData data)
                return $"{whip.Counter}/{data.threshold}";
            return null;
        }
    }
}
