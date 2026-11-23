using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>
    /// [TestingTools] 테스트용 덱 편집 창. 메뉴 MoveCute > Debug > Deck Builder.
    /// 왼쪽: Assets/Data/Cards의 모든 카드 (테마 폴더별, 검색). 끌어서 덱의 원하는 자리에 놓거나, 더블클릭·[+]로 덱 끝에 추가.
    /// 오른쪽: 고른 덱 프리셋(DeckPreset). 같은 카드가 이어지면 ×N으로 묶는다. 줄을 끌어 순서를 바꾸고, −/+로 장수를 바꾼다.
    /// Project 창의 카드 에셋도 덱에 끌어 놓을 수 있다 (여러 장 한 번에).
    /// 플레이 중에는 카드를 손패나 뽑을 카드 맨 위에 바로 넣을 수 있다 (이번 전투에만).
    /// "덱 프리셋 사용"을 켜면 새 런부터 이 덱으로 시작한다 (BattleTestOptions.OnNewRun).
    /// </summary>
    public class DeckBuilderWindow : EditorWindow
    {
        const string PresetFolder = "Assets/Data/TestingTools/Decks";
        const string CardsFolder = "Assets/Data/Cards";

        string search = "";
        Vector2 catalogScroll, deckScroll;
        readonly Dictionary<string, bool> foldouts = new();
        List<(string Theme, List<CardData> Cards)> catalog = new();
        /// <summary>GUI를 그리는 도중 목록을 바꾸지 않도록 편집은 다 그린 뒤 한 번에 한다.</summary>
        Action pendingEdit;

        [MenuItem("MoveCute/Debug/Deck Builder")]
        public static void Open() => GetWindow<DeckBuilderWindow>("Deck Builder");

        void OnEnable() => RefreshCatalog();
        void OnProjectChange() { RefreshCatalog(); Repaint(); }
        void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }

        void RefreshCatalog()
        {
            catalog = AssetDatabase.FindAssets("t:CardData", new[] { CardsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => (Theme: Theme(path), Data: AssetDatabase.LoadAssetAtPath<CardData>(path)))
                .Where(x => x.Data != null)
                .GroupBy(x => x.Theme)
                .OrderBy(g => g.Key.StartsWith("_") ? 1 : 0).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (g.Key, g.Select(x => x.Data).OrderBy(d => d.name, StringComparer.Ordinal).ToList()))
                .ToList();
        }

        static string Theme(string path)
        {
            string rel = path.Substring(CardsFolder.Length + 1);
            int slash = rel.IndexOf('/');
            return slash < 0 ? "(폴더 없음)" : rel.Substring(0, slash);
        }

        void OnGUI()
        {
            StartArmedDrag();
            if (Event.current.type == EventType.DragUpdated) dropIndex = -1;   // 덱 위가 아니면 표시 줄을 지운다 (HandleDeckDrop이 다시 정함)
            var preset = BattleTestOptions.DeckPreset;
            using (new EditorGUILayout.HorizontalScope())
            {
                var picked = (DeckPreset)EditorGUILayout.ObjectField("덱 프리셋", preset, typeof(DeckPreset), false);
                if (picked != preset) { TestingToolsPrefs.SetDeckPreset(picked); preset = picked; }
                if (GUILayout.Button("새 프리셋", GUILayout.Width(80))) preset = CreatePreset();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                TestingToolsPrefs.DeckPresetToggle();
                using (new EditorGUI.DisabledScope(TestingToolsPrefs.PlayingManager == null))
                    if (GUILayout.Button("새 런 시작", GUILayout.Width(80))) TestingToolsPrefs.StartNewRun();
            }
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.5f))) DrawCatalog(preset);
                using (new EditorGUILayout.VerticalScope()) DrawDeck(preset);
            }

            var edit = pendingEdit;
            pendingEdit = null;
            if (edit != null) { edit(); Repaint(); }
        }

        // ════════════ 왼쪽: 카드 목록 ════════════

        void DrawCatalog(DeckPreset preset)
        {
            EditorGUILayout.LabelField("카드 목록", EditorStyles.boldLabel);
            search = EditorGUILayout.TextField("검색", search);
            var ctx = PlayingContext();

            catalogScroll = EditorGUILayout.BeginScrollView(catalogScroll);
            foreach (var (theme, cards) in catalog)
            {
                var shown = cards.Where(Matches).ToList();
                if (shown.Count == 0) continue;
                bool searching = !string.IsNullOrEmpty(search);
                bool open = foldouts.TryGetValue(theme, out bool saved) ? saved : !theme.StartsWith("_");
                open = EditorGUILayout.Foldout(open || searching, $"{theme} ({shown.Count})", true);
                if (!searching) foldouts[theme] = open;
                if (!open) continue;

                foreach (var card in shown)
                {
                    var row = EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(12);
                    EditorGUILayout.LabelField(new GUIContent(CardLabel(card), SafeDescription(card) + "\n\n끌어서 덱에 놓기 · 더블클릭으로 덱 끝에 추가"));
                    using (new EditorGUI.DisabledScope(preset == null))
                        if (GUILayout.Button(new GUIContent("+", "덱 끝에 추가"), GUILayout.Width(24)))
                            pendingEdit = () => Edit(preset, () => preset.cards.Add(card));
                    if (ctx != null) DrawPlayButtons(ctx, card);
                    EditorGUILayout.EndHorizontal();

                    var e = Event.current;
                    if (preset != null && e.type == EventType.MouseDown && e.button == 0 && e.clickCount == 2 && row.Contains(e.mousePosition))
                    {
                        pendingEdit = () => Edit(preset, () => preset.cards.Add(card));
                        e.Use();
                    }
                    else ArmDrag(row, card, null);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        bool Matches(CardData card) =>
            string.IsNullOrEmpty(search)
            || CardLabel(card).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || card.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        // ════════════ 오른쪽: 덱 ════════════

        void DrawDeck(DeckPreset preset)
        {
            if (preset == null)
            {
                EditorGUILayout.LabelField("덱", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("덱 프리셋을 고르거나 [새 프리셋]으로 만드세요.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField($"덱: {preset.name} ({preset.cards.Count}장)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("시작 덱 가져오기", "열린 씬의 BattleManager.startingDeck을 복사한다")))
                    pendingEdit = () => CopyStartingDeck(preset);
                if (GUILayout.Button("비우기")) pendingEdit = () => Edit(preset, () => preset.cards.Clear());
            }

            EditorGUILayout.LabelField("줄을 끌어 순서 변경 · 왼쪽 목록이나 Project 창의 카드를 끌어 놓아 추가", EditorStyles.miniLabel);

            var runs = Runs(preset.cards);
            var ctx = PlayingContext();
            var rows = new List<Rect>();
            deckScroll = EditorGUILayout.BeginScrollView(deckScroll);
            for (int r = 0; r < runs.Count; r++)
            {
                var (card, start, count) = runs[r];
                var row = EditorGUILayout.BeginHorizontal();
                string label = card != null ? CardLabel(card) : "(없는 카드)";
                GUILayout.Label("≡", GUILayout.Width(14));
                EditorGUILayout.LabelField(new GUIContent($"{start + 1}. {label}  ×{count}", card != null ? SafeDescription(card) : ""));
                if (GUILayout.Button(new GUIContent("−", "한 장 빼기"), GUILayout.Width(22))) pendingEdit = () => Edit(preset, () => preset.cards.RemoveAt(start));
                if (GUILayout.Button(new GUIContent("+", "한 장 더"), GUILayout.Width(22))) pendingEdit = () => Edit(preset, () => preset.cards.Insert(start, card));
                if (ctx != null && card != null) DrawPlayButtons(ctx, card);
                EditorGUILayout.EndHorizontal();
                rows.Add(row);
                if (card != null) ArmDrag(row, card, (start, count));
            }
            // 목록 아래 빈 곳도 놓을 수 있게 남은 높이를 채운다 (여기 놓으면 맨 끝)
            var endZone = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            HandleDeckDrop(preset, runs, rows, endZone);
            EditorGUILayout.EndScrollView();
        }

        // ════════════ 끌어 놓기 ════════════

        /// <summary>덱 줄을 끌 때 DragAndDrop에 붙이는 데이터 키. 값은 (시작 위치, 장수).</summary>
        const string DragRunKey = "MoveCute.DeckBuilder.Run";
        CardData armedCard;
        (int Start, int Count)? armedRun;
        Vector2 armedScreenPos;
        /// <summary>끌고 있는 동안 표시할 끼워 넣을 위치. 없으면 -1.</summary>
        int dropIndex = -1;

        /// <summary>줄 위에서 마우스를 누르면 끌기를 준비한다. 실제 시작은 조금 움직였을 때 (StartArmedDrag).</summary>
        void ArmDrag(Rect row, CardData card, (int, int)? run)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !row.Contains(e.mousePosition)) return;
            armedCard = card;
            armedRun = run;
            armedScreenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
        }

        void StartArmedDrag()
        {
            var e = Event.current;
            if (e.type == EventType.MouseUp) { armedCard = null; armedRun = null; return; }
            if (e.type != EventType.MouseDrag || armedCard == null) return;
            if ((GUIUtility.GUIToScreenPoint(e.mousePosition) - armedScreenPos).sqrMagnitude < 25) return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { armedCard };
            if (armedRun.HasValue) DragAndDrop.SetGenericData(DragRunKey, armedRun.Value);
            DragAndDrop.StartDrag(SafeName(armedCard));
            armedCard = null;
            armedRun = null;
            e.Use();
        }

        void HandleDeckDrop(DeckPreset preset, List<(CardData Card, int Start, int Count)> runs, List<Rect> rows, Rect endZone)
        {
            var e = Event.current;
            if (e.type == EventType.DragExited) { dropIndex = -1; Repaint(); return; }
            if (e.type == EventType.Repaint && dropIndex >= 0)
            {
                // 끼워 넣을 자리에 파란 줄
                float y = dropIndex < preset.cards.Count && runs.FindIndex(run => run.Start == dropIndex) is int r && r >= 0 && r < rows.Count
                    ? rows[r].yMin : (rows.Count > 0 ? rows[^1].yMax : endZone.yMin);
                EditorGUI.DrawRect(new Rect(endZone.x, y - 1, endZone.width, 2), new Color(0.25f, 0.6f, 1f));
            }
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

            // 첫 줄 위부터 목록 아래 빈 곳까지 (줄 사이 틈 포함)
            float top = rows.Count > 0 ? rows[0].yMin : endZone.yMin;
            var pos = e.mousePosition;
            bool inside = pos.y >= top && pos.y <= endZone.yMax && pos.x >= endZone.xMin && pos.x <= endZone.xMax;
            var moved = DragAndDrop.GetGenericData(DragRunKey) as (int Start, int Count)?;
            var added = DragAndDrop.objectReferences.OfType<CardData>().ToList();
            if (!inside || (moved == null && added.Count == 0)) { dropIndex = -1; return; }

            // 마우스가 줄의 위쪽 절반이면 그 줄 앞, 아니면 다음 줄 앞
            int index = preset.cards.Count;
            for (int r = 0; r < runs.Count && r < rows.Count; r++)
                if (e.mousePosition.y < rows[r].center.y) { index = runs[r].Start; break; }

            DragAndDrop.visualMode = moved != null ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Copy;
            dropIndex = index;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                dropIndex = -1;
                if (moved is { } run) pendingEdit = () => MoveRun(preset, run.Start, run.Count, index);
                else pendingEdit = () => Edit(preset, () => preset.cards.InsertRange(Mathf.Min(index, preset.cards.Count), added));
            }
            e.Use();
        }

        /// <summary>[start, start+count) 구간을 index 앞으로 옮긴다 (index는 옮기기 전 기준).</summary>
        static void MoveRun(DeckPreset preset, int start, int count, int index)
        {
            if (start < 0 || start + count > preset.cards.Count) return;
            Edit(preset, () =>
            {
                var block = preset.cards.GetRange(start, count);
                preset.cards.RemoveRange(start, count);
                int target = index > start ? index - count : index;
                preset.cards.InsertRange(Mathf.Clamp(target, 0, preset.cards.Count), block);
            });
        }

        /// <summary>같은 카드가 이어진 구간들 (카드, 시작 위치, 장수).</summary>
        static List<(CardData Card, int Start, int Count)> Runs(List<CardData> cards)
        {
            var runs = new List<(CardData, int, int)>();
            for (int i = 0; i < cards.Count;)
            {
                int j = i + 1;
                while (j < cards.Count && cards[j] == cards[i]) j++;
                runs.Add((cards[i], i, j - i));
                i = j;
            }
            return runs;
        }

        static void CopyStartingDeck(DeckPreset preset)
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<BattleManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                Debug.LogWarning("[Deck Builder] 열린 씬에 BattleManager가 없습니다.");
                return;
            }
            Edit(preset, () =>
            {
                preset.cards.Clear();
                preset.cards.AddRange(manager.startingDeck.Where(c => c != null));
            });
        }

        static void Edit(DeckPreset preset, Action change)
        {
            Undo.RecordObject(preset, "덱 프리셋 편집");
            change();
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssetIfDirty(preset);
        }

        static DeckPreset CreatePreset()
        {
            EnsureFolder(PresetFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{PresetFolder}/Deck_New.asset");
            var preset = CreateInstance<DeckPreset>();
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            TestingToolsPrefs.SetDeckPreset(preset);
            EditorGUIUtility.PingObject(preset);
            return preset;
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        // ════════════ 플레이 중: 카드 바로 넣기 ════════════

        static BattleContext PlayingContext()
        {
            var manager = TestingToolsPrefs.PlayingManager;
            return manager != null && manager.Ctx != null && !manager.Ctx.IsOver ? manager.Ctx : null;
        }

        static void DrawPlayButtons(BattleContext ctx, CardData card)
        {
            if (GUILayout.Button(new GUIContent("손패", "이번 전투 손패에 바로 넣기"), GUILayout.Width(40)))
                ctx.TestAddCardToHand(card).Forget();
            if (GUILayout.Button(new GUIContent("맨 위", "뽑을 카드 맨 위에 넣기 (다음에 뽑힘)"), GUILayout.Width(44)))
                ctx.TestAddCardToDrawTop(card).Forget();
        }

        // ════════════ 표시 ════════════

        static string CardLabel(CardData card) =>
            $"{SafeName(card)} {string.Concat(card.moves.Select(m => m.ToArrow()))}  ({card.cost})";

        static string SafeName(CardData card)
        {
            try { return card.DisplayName; }
            catch { return card.name; }
        }

        static string SafeDescription(CardData card)
        {
            try { return card.Description; }
            catch { return ""; }
        }
    }
}
