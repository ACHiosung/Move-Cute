using System.IO;
using System.Linq;
using UnityEditor;

namespace MoveCute.EditorTools
{
    /// <summary>SampleContent를 실제 에셋으로 저장. 카드/적 에셋 작성 예시로 참고.</summary>
    public static class SampleAssetMenu
    {
        const string Root = "Assets/Data/Sample";

        [MenuItem("MoveCute/Create Sample Assets")]
        static void Create()
        {
            Directory.CreateDirectory(Root);

            foreach (var card in SampleContent.CreateStarterDeck().Distinct())
                AssetDatabase.CreateAsset(card, $"{Root}/Card_{card.name}.asset");

            var tomb = SampleContent.Tombstone();
            AssetDatabase.CreateAsset(tomb, $"{Root}/Obstacle_{tomb.name}.asset");
            var enemy = SampleContent.Gravekeeper(tomb);
            AssetDatabase.CreateAsset(enemy, $"{Root}/Enemy_{enemy.name}.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
