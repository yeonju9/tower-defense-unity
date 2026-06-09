using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>타워 한 대가 사거리 내 적 중 누구를 쏠지 결정한다.</summary>
    public class TargetSelector
    {
        /// <summary>타워 후보 목록의 한 항목: 적 본체, 현재 위치, 경로 진행도.</summary>
        public readonly struct Candidate
        {
            public EnemyUnit Unit { get; }
            public Vec2 Position { get; }
            public float Progress { get; }

            public Candidate(EnemyUnit unit, Vec2 position, float progress)
            {
                Unit = unit;
                Position = position;
                Progress = progress;
            }
        }

        /// <summary>
        /// 슬라이스 정책: 사거리 안의 살아있는 적 중 '경로를 가장 많이 전진한(=목표에 가장 가까운)' 적을 고른다.
        /// 대상이 없으면 null.
        /// </summary>
        public EnemyUnit Select(Vec2 towerPosition, float range, IReadOnlyList<Candidate> candidates)
        {
            int index = FindBestIndex(towerPosition, range, candidates);
            return index < 0 ? null : candidates[index].Unit;
        }

        /// <summary>
        /// Select와 동일한 정책으로 고르되, 착탄 위치 계산 등을 위해 후보 전체(위치 포함)를 반환한다.
        /// 대상이 없으면 null.
        /// </summary>
        public Candidate? SelectCandidate(Vec2 towerPosition, float range, IReadOnlyList<Candidate> candidates)
        {
            int index = FindBestIndex(towerPosition, range, candidates);
            return index < 0 ? (Candidate?)null : candidates[index];
        }

        private static int FindBestIndex(Vec2 towerPosition, float range, IReadOnlyList<Candidate> candidates)
        {
            if (candidates == null)
                return -1;

            int bestIndex = -1;
            float bestProgress = float.NegativeInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Unit == null || c.Unit.IsDead)
                    continue;
                if (towerPosition.DistanceTo(c.Position) > range)
                    continue;
                if (c.Progress > bestProgress)
                {
                    bestProgress = c.Progress;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
