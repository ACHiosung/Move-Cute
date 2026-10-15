using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>[SerializeReference, SubclassPicker] 필드에 하위 타입 선택 드롭다운을 그린다.</summary>
    [CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
    public class SubclassPickerDrawer : PropertyDrawer
    {
        static readonly Dictionary<Type, Type[]> cache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

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

            var popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            int next = EditorGUI.Popup(popupRect, index, names);
            if (next != index)
            {
                property.managedReferenceValue = next == 0 ? null : Activator.CreateInstance(types[next - 1]);
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.PropertyField(position, property, label, true);
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
