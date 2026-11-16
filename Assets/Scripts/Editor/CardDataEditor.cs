using System.Linq;
using UnityEditor;

namespace MoveCute.EditorTools
{
    /// <summary>
    /// 카드 인스펙터. 기본 인스펙터와 같은 순서로 그리고, target이 None이면 target 바로 아래에 경고를 띄운다.
    /// 기획상 카드는 누르자마자 발동하지 않는다. 시트의 "적" = Unit, "보드" = SelfTile(내 칸) 또는 OtherTile(다른 칸).
    /// </summary>
    [CustomEditor(typeof(CardData)), CanEditMultipleObjects]
    public class CardDataEditor : Editor
    {
        const string NoneTargetWarning =
            "대상(target)이 None입니다. 카드는 누르자마자 발동하지 않아야 합니다.\n" +
            "시트의 \"적\"은 Unit, \"보드\"는 SelfTile(내 칸) 또는 OtherTile(다른 칸)으로 정하세요.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false)
            {
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);

                if (property.propertyPath == nameof(CardData.target) &&
                    targets.Any(t => t is CardData card && card.target == TargetType.None))
                    EditorGUILayout.HelpBox(NoneTargetWarning, MessageType.Warning);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
