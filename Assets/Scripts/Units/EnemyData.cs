using UnityEngine;

namespace MoveCute
{
    /// <summary>
    /// 적 원본. 적은 코드로 만든다: 적 하나 = Enemies/ 아래 파일 하나 (예: Enemies/GravekeeperData.cs).
    /// 하위 클래스에 기획자가 조정할 숫자를 필드로 두고, CreateEnemy에서 자기 Enemy 하위 클래스를 돌려준다.
    /// ScriptableObject라서 하위 클래스 이름과 파일 이름이 같아야 한다.
    /// </summary>
    public abstract class EnemyData : ScriptableObject
    {
        public string id;
        public string nameKey;
        public int startMorale = 20;
        public Sprite sprite;

        /// <summary>전투에서 쓸 Enemy 객체를 만든다. 전투마다 새로 만든다.</summary>
        public abstract Enemy CreateEnemy();

        /// <summary>현재 언어의 이름. 다국어 표에 없으면 에셋 이름.</summary>
        public string DisplayName => Loc.Get(nameKey, name);
    }
}
