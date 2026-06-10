using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>타워의 타깃 우선순위 정책. 플레이어가 타워별로 바꿀 수 있다.</summary>
    public enum TargetingMode
    {
        /// <summary>선두(경로를 가장 많이 전진한 = 목표에 가장 가까운) 적. 기본값.</summary>
        First,
        /// <summary>타워에서 물리적으로 가장 가까운 적(연사탑처럼 코앞을 빠르게 정리).</summary>
        Nearest,
        /// <summary>체력이 가장 높은 적(저격탑처럼 단단한 적을 우선 저격).</summary>
        Strongest,
    }

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
        public EnemyUnit Select(Vec2 towerPosition, float range, IReadOnlyList<Candidate> candidates,
            TargetingMode mode = TargetingMode.First)
        {
            int index = FindBestIndex(towerPosition, range, candidates, mode);
            return index < 0 ? null : candidates[index].Unit;
        }

        /// <summary>
        /// Select와 동일한 정책으로 고르되, 착탄 위치 계산 등을 위해 후보 전체(위치 포함)를 반환한다.
        /// 대상이 없으면 null.
        /// </summary>
        public Candidate? SelectCandidate(Vec2 towerPosition, float range, IReadOnlyList<Candidate> candidates,
            TargetingMode mode = TargetingMode.First)
        {
            int index = FindBestIndex(towerPosition, range, candidates, mode);
            return index < 0 ? (Candidate?)null : candidates[index];
        }

        private static int FindBestIndex(Vec2 towerPosition, float range, IReadOnlyList<Candidate> candidates,
            TargetingMode mode)
        {
            if (candidates == null)
                return -1;

            int bestIndex = -1;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Unit == null || c.Unit.IsDead)
                    continue;
                float dist = towerPosition.DistanceTo(c.Position);
                if (dist > range)
                    continue;

                // 점수가 클수록 우선. 모드별로 무엇을 '클수록 좋다'로 볼지만 다르다.
                float score;
                switch (mode)
                {
                    case TargetingMode.Nearest:
                        score = -dist;          // 가까울수록(거리 작을수록) 높은 점수
                        break;
                    case TargetingMode.Strongest:
                        score = c.Unit.Hp;      // 체력이 높을수록
                        break;
                    default:
                        score = c.Progress;     // First: 가장 전진한 적
                        break;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
