using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MoveCute.EditorTools
{
    /// <summary>
    /// [SerializeReference, SubclassPicker] 필드에 하위 타입 선택 버튼을 그리고,
    /// 그 아래에 선택한 타입의 값(피해량 등)을 접지 않고 항상 보여 준다.
    ///
    /// 버튼을 누르면 검색창이 있는 목록이 열린다. 하위 타입은 정의된 스크립트 파일별로 묶인다
    /// (예: BasicEffects ▸ DamageEffect). 파일에 고를 타입이 하나뿐이면 묶지 않고 바로 보인다.
    /// → 새 효과를 비슷한 효과끼리 한 파일에 넣으면 목록에서도 같이 모인다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
    public class SubclassPickerDrawer : PropertyDrawer
    {
        const string NoneLabel = "(없음)";

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

            var current = property.managedReferenceValue?.GetType();
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var buttonRect = EditorGUI.PrefixLabel(line, label);
            var content = current == null
                ? new GUIContent(NoneLabel)
                : new GUIContent(current.Name, ScriptGroups.Of(current) is { } file ? $"{file}.cs" : null);

            if (EditorGUI.DropdownButton(buttonRect, content, FocusType.Keyboard))
            {
                // 목록에서 고르는 시점은 OnGUI가 끝난 뒤라 property를 그대로 쓸 수 없다. 대상과 경로를 들고 가서 다시 찾는다
                var targets = property.serializedObject.targetObjects;
                var path = property.propertyPath;
                new TypeDropdown(GetTypes(property), type => Assign(targets, path, type)).Show(buttonRect);
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

        /// <summary>고른 타입의 새 객체를 넣는다. 이미 같은 타입이면 입력해 둔 값이 지워지지 않게 그대로 둔다.</summary>
        static void Assign(UnityEngine.Object[] targets, string path, Type type)
        {
            if (targets.Any(t => t == null)) return;   // 목록이 열린 사이 에셋이 지워짐
            var so = new SerializedObject(targets);
            var property = so.FindProperty(path);
            if (property == null) return;
            if (!property.hasMultipleDifferentValues && property.managedReferenceValue?.GetType() == type) return;

            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            so.ApplyModifiedProperties();
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

        /// <summary>검색창 + 파일별 묶음 목록.</summary>
        class TypeDropdown : AdvancedDropdown
        {
            readonly Type[] types;
            readonly Action<Type> onPicked;

            public TypeDropdown(Type[] types, Action<Type> onPicked) : base(new AdvancedDropdownState())
            {
                this.types = types;
                this.onPicked = onPicked;
                minimumSize = new Vector2(260, 320);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("타입 선택");
                root.AddChild(new TypeItem(NoneLabel, null));

                var groups = types.GroupBy(ScriptGroups.Of).OrderBy(g => g.Key).ToList();
                var loose = new List<Type>();
                foreach (var group in groups)
                {
                    // 전부 한 파일이거나(키워드 등) 파일에 하나뿐이면 묶지 않는다
                    if (groups.Count == 1 || group.Key == null || group.Count() == 1)
                    {
                        loose.AddRange(group);
                        continue;
                    }
                    var folder = new AdvancedDropdownItem(group.Key);
                    foreach (var type in group.OrderBy(t => t.Name)) folder.AddChild(new TypeItem(type.Name, type));
                    root.AddChild(folder);
                }
                foreach (var type in loose.OrderBy(t => t.Name)) root.AddChild(new TypeItem(type.Name, type));
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is TypeItem picked) onPicked(picked.Type);
            }

            class TypeItem : AdvancedDropdownItem
            {
                public readonly Type Type;
                public TypeItem(string name, Type type) : base(name) { Type = type; }
            }
        }

        /// <summary>
        /// 타입이 정의된 스크립트 파일 이름 (확장자 없이). Assets/Scripts의 .cs를 읽어 "class 이름"을 찾는다.
        /// 스크립트가 바뀌면 도메인 리로드로 static이 비워져 다시 읽는다.
        /// </summary>
        static class ScriptGroups
        {
            static Dictionary<string, string> fileOfClass;

            static readonly Regex comments = new(@"//[^\n]*|/\*.*?\*/", RegexOptions.Singleline);
            static readonly Regex declaration = new(@"\b(?:class|struct)\s+([A-Za-z_]\w*)");

            public static string Of(Type type)
            {
                fileOfClass ??= Build();
                return fileOfClass.TryGetValue(type.Name, out var file) ? file : null;
            }

            static Dictionary<string, string> Build()
            {
                var map = new Dictionary<string, string>();
                foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".cs") || !File.Exists(path)) continue;
                    var code = comments.Replace(File.ReadAllText(path), "");
                    var file = Path.GetFileNameWithoutExtension(path);
                    foreach (Match m in declaration.Matches(code)) map.TryAdd(m.Groups[1].Value, file);
                }
                return map;
            }
        }
    }
}
