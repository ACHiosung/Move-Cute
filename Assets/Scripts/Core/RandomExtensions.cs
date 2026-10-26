using System.Collections.Generic;
using System.Linq;

namespace MoveCute
{
    /// <summary>
    /// 시드 RNG(`ctx.Rng`) 보조 함수. 무작위는 UnityEngine.Random 대신 이걸 쓴다 — 같은 시드면 같은 런이 재현된다.
    /// 호출 순서도 결과에 영향을 주므로, 한 번 쓴 무작위 호출을 조건문으로 건너뛰거나 추가하면 그 뒤 결과가 달라진다.
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>min 이상 maxExclusive 미만의 정수 (UnityEngine.Random.Range(int, int)와 같은 범위 규칙).</summary>
        public static int Range(this System.Random rng, int min, int maxExclusive) => rng.Next(min, maxExclusive);

        /// <summary>목록에서 하나. 비어 있으면 default.</summary>
        public static T Pick<T>(this System.Random rng, IReadOnlyList<T> list) =>
            list.Count == 0 ? default : list[rng.Next(list.Count)];

        /// <summary>목록을 제자리에서 섞는다 (Fisher-Yates).</summary>
        public static void Shuffle<T>(this System.Random rng, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>원본은 건드리지 않고 섞은 복사본을 돌려준다. 무작위로 n개 뽑기: <c>rng.Shuffled(src).Take(n)</c>.</summary>
        public static List<T> Shuffled<T>(this System.Random rng, IEnumerable<T> source)
        {
            var list = source.ToList();
            rng.Shuffle(list);
            return list;
        }
    }
}
