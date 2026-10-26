using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>
    /// [SerializeReference, SubclassPicker] 필드에 하위 타입 선택 드롭다운을 그리고,
    /// 그 아래에 선택한 타입의 값(피해량 등)을 접지 않고 항상 보여 준다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
    public class SubclassPickerDrawer : PropertyDrawer
    {
        static readonly Dictionary<Type, Type[]> cache = new();

        static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float height = EditorGUIUtility.singleLineHeight;
            foreach (var child in Children(property))
                height += Spacing + EditorGUI.GetPropertyHeight(child, true);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var types = GetTypes(property);
            var current = property.managedReferenceValue?.GetType();
            int index = current == null ? 0 : Array.IndexOf(types, current) + 1;
            var names = new[] { "(없음)" }.Concat(types.Select(t => t.Name)).ToArray();

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var popupRect = EditorGUI.PrefixLabel(line, label);
            int next = EditorGUI.Popup(popupRect, index, names);
            if (next != index)
            {
                property.managedReferenceValue = next == 0 ? null : Activator.CreateInstance(types[next - 1]);
                property.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();   // 타입이 바뀌어 하위 필드 구성이 달라졌으므로 다음 프레임에 다시 그린다
            }

            EditorGUI.indentLevel++;
            float y = line.yMax;
            foreach (var child in Children(property))
            {
                float h = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y + Spacing, position.width, h), child, true);
                y += Spacing + h;
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>바로 아래 단계의 보이는 필드들.</summary>
        static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            var it = property.Copy();
            var end = property.GetEndProperty();
            if (!it.NextVisible(true)) yield break;
            while (!SerializedProperty.EqualContents(it, end))
            {
                yield return it.Copy();
                if (!it.NextVisible(false)) yield break;
            }
        }

        static Type[] GetTypes(SerializedProperty property)
        {
            // managedReferenceFieldTypename = "어셈블리 네임스페이스.타입"
            var parts = property.managedReferenceFieldTypename.Split(' ');
            var baseType = parts.Length == 2 ? Type.GetType($"{parts[1]}, {parts[0]}") : null;
            if (baseType == null) return Array.Empty<Type>();

            if (!cache.TryGetValue(baseType, out var types))
            {
                types = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(t => !t.IsAbstract && !t.IsGenericType && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                                && t.GetConstructor(Type.EmptyTypes) != null)
                    .OrderBy(t => t.Name)
                    .ToArray();
                cache[baseType] = types;
            }
            return types;
        }
    }
}
