using UnityEditor;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>BattleInt를 한 줄로 그린다: [가져올 곳 ▼] [고정 값]. 고정 값 칸은 Fixed일 때만 보인다.</summary>
    [CustomPropertyDrawer(typeof(BattleInt))]
    public class BattleIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var source = property.FindPropertyRelative(nameof(BattleInt.source));
            var value = property.FindPropertyRelative(nameof(BattleInt.value));

            EditorGUI.BeginProperty(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (source.intValue == (int)BattleValue.Fixed)
            {
                const float gap = 4f;
                float half = (rect.width - gap) / 2f;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, half, rect.height), source, GUIContent.none);
                EditorGUI.PropertyField(new Rect(rect.x + half + gap, rect.y, half, rect.height), value, GUIContent.none);
            }
            else
            {
                EditorGUI.PropertyField(rect, source, GUIContent.none);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;
    }
}
