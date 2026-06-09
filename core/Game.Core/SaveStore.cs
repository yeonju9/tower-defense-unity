using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Core
{
    /// <summary>
    /// 스테이지별 클리어 여부·최고 별점을 보관하는 순수 저장소.
    /// Unity의 PlayerPrefs/JsonUtility에 의존하지 않으며, 직렬화 문자열만 주고받는다
    /// (실제 영구 저장은 Unity 레이어가 Serialize() 결과를 PlayerPrefs에 쓰는 식).
    /// 로드 시 손상·범위 밖 값은 건너뛰어 무결성을 지킨다(tech-env.md: 세이브 입력 검증).
    /// </summary>
    public class SaveStore
    {
        private const char EntrySeparator = ';';
        private const char FieldSeparator = ':';
        private const int MinStars = 1;
        private const int MaxStars = 3;

        private readonly Dictionary<int, int> bestStars = new Dictionary<int, int>();

        /// <summary>스테이지 클리어를 기록한다. 같은 스테이지는 더 높은 별점만 갱신한다.</summary>
        public void RecordClear(int stageId, int stars)
        {
            if (stageId < 0)
                throw new ArgumentOutOfRangeException(nameof(stageId), "스테이지 ID는 음수일 수 없습니다.");
            if (stars < MinStars || stars > MaxStars)
                throw new ArgumentOutOfRangeException(nameof(stars), "별점은 1~3 범위여야 합니다.");

            if (!bestStars.TryGetValue(stageId, out int current) || stars > current)
                bestStars[stageId] = stars;
        }

        public bool IsCleared(int stageId) => bestStars.ContainsKey(stageId);

        /// <summary>최고 별점. 클리어한 적 없으면 0.</summary>
        public int GetStars(int stageId) =>
            bestStars.TryGetValue(stageId, out int s) ? s : 0;

        /// <summary>"id:stars;id:stars" 형식으로 직렬화(스테이지 ID 오름차순).</summary>
        public string Serialize()
        {
            var ids = new List<int>(bestStars.Keys);
            ids.Sort();

            var sb = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(EntrySeparator);
                sb.Append(ids[i]).Append(FieldSeparator).Append(bestStars[ids[i]]);
            }
            return sb.ToString();
        }

        /// <summary>직렬화 문자열에서 복원한다. 형식이 깨졌거나 범위 밖인 항목은 무시한다.</summary>
        public static SaveStore Deserialize(string data)
        {
            var store = new SaveStore();
            if (string.IsNullOrEmpty(data))
                return store;

            foreach (var entry in data.Split(EntrySeparator))
            {
                if (string.IsNullOrEmpty(entry))
                    continue;
                var parts = entry.Split(FieldSeparator);
                if (parts.Length != 2)
                    continue;
                if (!int.TryParse(parts[0], out int stageId) || stageId < 0)
                    continue;
                if (!int.TryParse(parts[1], out int stars) || stars < MinStars || stars > MaxStars)
                    continue;

                store.RecordClear(stageId, stars);
            }
            return store;
        }
    }
}
