using UnityEditor;

namespace MoveCute.EditorTools
{
    /// <summary>특수 유물(RelicData 하위 클래스)은 Bind를 직접 구현해 trigger를 쓰지 않으므로 인스펙터에서 숨긴다.</summary>
    [CustomEditor(typeof(RelicData), true)]
    public class RelicDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target.GetType() == typeof(RelicData)) { base.OnInspectorGUI(); return; }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "trigger");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
