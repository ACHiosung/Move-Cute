using System.Collections.Generic;
using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// [TestingTools] 테스트용 시작 덱 프리셋. 목록 순서가 덱 순서다 ("시작 덱 순서대로 뽑기"와 같이 쓴다).
    /// Deck Builder 창(메뉴 MoveCute > Debug > Deck Builder)에서 만들고 편집한다. 에셋은 Assets/Data/TestingTools/Decks에 둔다.
    /// </summary>
    [CreateAssetMenu(menuName = "MoveCute/Testing/Deck Preset", fileName = "Deck_")]
    public class DeckPreset : ScriptableObject
    {
        public List<CardData> cards = new();
    }
}
