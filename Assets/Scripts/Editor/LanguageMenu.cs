using UnityEditor;

namespace MoveCute.EditorTools
{
    /// <summary>언어 전환 (플레이 중에도 바로 반영). 선택한 언어는 PlayerPrefs에 저장된다.</summary>
    public static class LanguageMenu
    {
        const string Root = "MoveCute/Language/";

        [MenuItem(Root + "한국어")] static void Korean() => Loc.Language = "ko";
        [MenuItem(Root + "English")] static void English() => Loc.Language = "en";

        [MenuItem(Root + "한국어", true)] static bool KoreanCheck() => Check("한국어", "ko");
        [MenuItem(Root + "English", true)] static bool EnglishCheck() => Check("English", "en");

        [MenuItem(Root + "Reload Strings.csv", priority = 100)] static void Reload() => Loc.Reload();

        static bool Check(string item, string code)
        {
            Menu.SetChecked(Root + item, Loc.Language == code);
            return true;
        }
    }
}
