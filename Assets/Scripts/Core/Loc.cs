using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 다국어 텍스트. Resources/Localization/Strings.csv (key,ko,en,...)를 읽어 키를 현재 언어 문장으로 바꾼다.
    /// 언어를 추가하려면 CSV에 열만 추가한다. 현재 언어 칸이 비어 있으면 첫 번째 언어로 보여 준다.
    /// </summary>
    public static class Loc
    {
        const string TablePath = "Localization/Strings";
        const string PrefsKey = "MoveCute.Language";

        static Dictionary<string, string[]> table;
        static string[] languages;
        static string language;
        static readonly HashSet<string> warned = new();

        /// <summary>언어가 바뀌면 호출된다. 화면의 텍스트를 다시 그릴 때 구독한다.</summary>
        public static event Action LanguageChanged;

        /// <summary>CSV 헤더의 언어 코드 목록 (예: ko, en).</summary>
        public static IReadOnlyList<string> Languages { get { Load(); return languages; } }

        /// <summary>현재 언어 코드. 바꾸면 저장되고 LanguageChanged가 호출된다.</summary>
        public static string Language
        {
            get { Load(); return language; }
            set
            {
                Load();
                if (Array.IndexOf(languages, value) < 0) { Debug.LogWarning($"[Loc] 표에 없는 언어: {value}"); return; }
                if (language == value) return;
                language = value;
                PlayerPrefs.SetString(PrefsKey, value);
                LanguageChanged?.Invoke();
            }
        }

        public static bool Has(string key)
        {
            Load();
            return !string.IsNullOrEmpty(key) && table.ContainsKey(key);
        }

        /// <summary>키에 해당하는 현재 언어 문장. 표에 없으면 fallback(없으면 키 그대로).</summary>
        public static string Get(string key, string fallback = null)
        {
            Load();
            if (string.IsNullOrEmpty(key)) return fallback ?? "";
            if (!table.TryGetValue(key, out var row))
            {
                Warn(key, $"[Loc] 표에 없는 키: {key}");
                return fallback ?? key;
            }

            int i = Array.IndexOf(languages, language);
            if (i < row.Length && row[i].Length > 0) return row[i];
            Warn($"{language}:{key}", $"[Loc] [{language}] 번역 없음: {key}");
            return row[0];
        }

        static readonly Regex Placeholder = new(@"\{([\w.]+)\}");

        /// <summary>
        /// {이름} 자리에 sources의 같은 이름 public 필드 값을 넣는다. 앞의 source부터 찾는다.
        /// 예: "피해를 {damage} 줍니다" + DamageEffect{damage=5} → "피해를 5 줍니다".
        /// 점(.)으로 안쪽까지 찾는다: {onBreak.damage} = onBreak 목록 안 효과의 damage.
        /// </summary>
        public static string Format(string key, IEnumerable<object> sources, string fallback = null)
        {
            var text = Get(key, fallback);
            if (text.IndexOf('{') < 0) return text;

            var list = new List<object>(sources);
            return Placeholder.Replace(text, m =>
            {
                if (TryFind(list, m.Groups[1].Value.Split('.'), 0, out var v)) return Convert.ToString(v, CultureInfo.InvariantCulture);
                Warn($"{key}:{m.Value}", $"[Loc] {key}: {m.Value}에 넣을 값이 없습니다");
                return m.Value;
            });
        }

        /// <summary>obj에서 path[i..]의 필드 값을 찾는다. 목록을 만나면 원소를 앞에서부터 차례로 찾아본다.</summary>
        static bool TryFind(object obj, string[] path, int i, out object value)
        {
            value = null;
            if (obj == null) return false;
            if (i == path.Length)
            {
                value = obj;
                return obj.GetType().IsPrimitive || obj.GetType().IsEnum || obj is string;
            }
            if (obj is IEnumerable items && obj is not string)
            {
                foreach (var item in items)
                    if (TryFind(item, path, i, out value)) return true;
                return false;
            }
            var f = obj.GetType().GetField(path[i], BindingFlags.Public | BindingFlags.Instance);
            return f != null && TryFind(f.GetValue(obj), path, i + 1, out value);
        }

        /// <summary>CSV를 다시 읽는다 (에디터에서 표를 고친 뒤).</summary>
        public static void Reload()
        {
            table = null;
            warned.Clear();
            Load();
            LanguageChanged?.Invoke();
        }

        static void Load()
        {
            if (table != null) return;
            table = new Dictionary<string, string[]>();
            languages = new[] { "ko" };

            var asset = Resources.Load<TextAsset>(TablePath);
            if (asset == null) Debug.LogError($"[Loc] Resources/{TablePath}.csv가 없습니다");
            else
            {
                // 엑셀에서 "CSV(쉼표로 분리)"로 저장하면 UTF-8이 아니게 되어 한글이 깨진다
                if (asset.text.IndexOf('�') >= 0)
                    Debug.LogError($"[Loc] {TablePath}.csv가 UTF-8이 아닙니다. 엑셀에서 'CSV UTF-8(쉼표로 분리)' 형식으로 다시 저장하세요");
                Parse(asset.text.TrimStart('﻿'));
            }

            var saved = PlayerPrefs.GetString(PrefsKey, "");
            var system = Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en";
            language = Array.IndexOf(languages, saved) >= 0 ? saved
                     : Array.IndexOf(languages, system) >= 0 ? system
                     : languages[0];
        }

        /// <summary>첫 줄 = key,언어1,언어2... / key가 비었거나 #으로 시작하는 줄은 무시.</summary>
        static void Parse(string csv)
        {
            var rows = ParseCsv(csv);
            if (rows.Count == 0 || rows[0].Count < 2) { Debug.LogError("[Loc] 첫 줄은 key,ko,en 형식이어야 합니다"); return; }
            languages = rows[0].GetRange(1, rows[0].Count - 1).ConvertAll(s => s.Trim()).ToArray();

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                var key = row[0].Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                var values = new string[languages.Length];
                for (int i = 0; i < values.Length; i++) values[i] = i + 1 < row.Count ? row[i + 1] : "";
                if (!table.TryAdd(key, values)) Debug.LogWarning($"[Loc] 중복 키: {key} ({r + 1}번째 줄)");
            }
        }

        /// <summary>따옴표("...")로 감싼 칸 안의 쉼표·줄바꿈·"" 이스케이프를 지원하는 CSV 파서.</summary>
        static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c != '"') cell.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = false;
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(cell.ToString()); cell.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }

        static void Warn(string id, string message)
        {
            if (warned.Add(id)) Debug.LogWarning(message);
        }
    }
}
