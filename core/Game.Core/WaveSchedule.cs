using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>스폰 한 건: 언제(time, 초) 어떤 적(spec)을 낼지.</summary>
    public readonly struct SpawnEntry
    {
        public float Time { get; }
        public EnemySpec Spec { get; }

        public SpawnEntry(float time, EnemySpec spec)
        {
            if (time < 0f)
                throw new ArgumentOutOfRangeException(nameof(time), "스폰 시각은 음수일 수 없습니다.");
            Time = time;
            Spec = spec;
        }
    }

    /// <summary>
    /// 스테이지의 스폰 스케줄. 누적 경과 시간을 주면 그 시점까지 도래한 스폰들을 한 번씩만 반환한다.
    /// 모든 스폰이 소진되면 AllSpawned가 true가 된다.
    /// </summary>
    public class WaveSchedule
    {
        private readonly List<SpawnEntry> entries;
        private int nextIndex;

        public bool AllSpawned => nextIndex >= entries.Count;

        public WaveSchedule(IReadOnlyList<SpawnEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            // 시각 순으로 정렬해 순차 소비를 보장(입력 순서에 의존하지 않음).
            this.entries = new List<SpawnEntry>(entries);
            this.entries.Sort((a, b) => a.Time.CompareTo(b.Time));
            nextIndex = 0;
        }

        /// <summary>
        /// elapsedTime 시점까지 도래했고 아직 반환하지 않은 스폰들을 반환한다.
        /// 같은 스폰을 두 번 반환하지 않으며, 한 번에 여러 건이 도래하면 모두 반환한다.
        /// </summary>
        public IReadOnlyList<EnemySpec> Collect(float elapsedTime)
        {
            var due = new List<EnemySpec>();
            while (nextIndex < entries.Count && entries[nextIndex].Time <= elapsedTime)
            {
                due.Add(entries[nextIndex].Spec);
                nextIndex++;
            }
            return due;
        }

        /// <summary>
        /// 아직 스폰되지 않은 적들을 시각 순서대로 조회한다(소비하지 않음).
        /// 웨이브 미리보기 UI가 '다음에 무엇이 올지' 표시하는 데 쓴다.
        /// </summary>
        public IReadOnlyList<EnemySpec> PeekRemaining()
        {
            var remaining = new List<EnemySpec>(entries.Count - nextIndex);
            for (int i = nextIndex; i < entries.Count; i++)
                remaining.Add(entries[i].Spec);
            return remaining;
        }
    }
}
