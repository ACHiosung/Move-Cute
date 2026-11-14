using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [임시] 장착 중인 유물을 화면 위쪽에 아이콘 줄로 보여 준다 (슬레이 더 스파이어처럼).
    /// 마우스를 올리면 이름과 설명이 나온다. 아이콘이 없는 유물은 이름 앞 두 글자로 표시한다.
    /// 아이콘을 클릭하면 그 유물을 끄고 켠다 (Relic.Enabled, 꺼진 유물은 흐리게). 전투 도중에도 바로 적용된다.
    /// 씬에 넣지 않아도 실행 시 스스로 생기고, BattleManager가 있는 씬에서만 그린다.
    /// 정식 유물 UI가 생기면 이 파일을 지운다.
    /// </summary>
    public class TempRelicBar : MonoBehaviour
    {
        const float RefHeight = 1080f;   // 이 해상도 기준으로 크기를 잡고 화면에 맞춰 늘린다
        const float Size = 56f, Gap = 8f, Top = 12f;

        GUIStyle label, tooltip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            var go = new GameObject(nameof(TempRelicBar));
            go.AddComponent<TempRelicBar>();
            DontDestroyOnLoad(go);
        }

        void OnGUI()
        {
            var relics = BattleManager.Instance != null ? BattleManager.Instance.Run?.Relics : null;
            if (relics == null || relics.Count == 0) return;

            label ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            tooltip ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 18, wordWrap = true, richText = true, padding = new RectOffset(10, 10, 8, 8) };

            float scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;

            float total = relics.Count * Size + (relics.Count - 1) * Gap;
            float x = (width - total) / 2f;
            var mouse = Event.current.mousePosition;
            Relic hovered = null;
            Rect hoveredRect = default;

            foreach (var relic in relics)
            {
                var rect = new Rect(x, Top, Size, Size);
                var data = relic.Data;
                var icon = data != null ? data.icon : null;
                GUI.color = relic.Enabled ? Color.white : new Color(1, 1, 1, 0.35f);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) relic.Enabled = !relic.Enabled;
                if (icon != null)
                {
                    GUI.Box(rect, GUIContent.none);
                    var tr = icon.textureRect;
                    var tex = icon.texture;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x + 4, rect.y + 4, Size - 8, Size - 8), tex,
                        new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
                }
                else
                {
                    string name = relic.ToString();
                    GUI.Box(rect, name.Length > 2 ? name.Substring(0, 2) : name, label);
                }

                if (rect.Contains(mouse)) { hovered = relic; hoveredRect = rect; }
                x += Size + Gap;
            }
            GUI.color = Color.white;

            if (hovered != null)
            {
                string state = hovered.Enabled ? "켜짐 (클릭해서 끄기)" : "<color=#ff8080>꺼짐</color> (클릭해서 켜기)";
                string text = $"<b>{hovered}</b>  {state}\n{(hovered.Data != null ? hovered.Data.Description : "")}";
                const float tipWidth = 320f;
                float tipHeight = tooltip.CalcHeight(new GUIContent(text), tipWidth);
                float tipX = Mathf.Clamp(hoveredRect.x, 8, width - tipWidth - 8);
                GUI.Box(new Rect(tipX, hoveredRect.yMax + 6, tipWidth, tipHeight), text, tooltip);
            }
        }
    }
}
