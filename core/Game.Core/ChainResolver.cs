using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 번개탑(체인 타격)의 연쇄 대상을 산출한다. 시작 적에서 jumpRange 안의
    /// '가장 가까운 아직 안 맞은 살아있는 적'으로 maxTargets만큼 순차 점프하며,
    /// 점프할 때마다 데미지가 falloff 비율로 곱해져 감쇠한다.
    /// 광역(SplashResolver)이 '반경 내 동시타'라면 이쪽은 '인접 적으로 순차 점프'다.
    /// </summary>
    public class ChainResolver
    {
        /// <summary>체인 한 타: 맞은 적과 그 적이 받을 (감쇠 적용된) 데미지.</summary>
        public readonly struct ChainHit
        {
            public EnemyUnit Unit { get; }
            public int Damage { get; }

            public ChainHit(EnemyUnit unit, int damage)
            {
                Unit = unit;
                Damage = damage;
            }
        }

        public IReadOnlyList<ChainHit> Resolve(
            TargetSelector.Candidate start, int baseDamage, int maxTargets, float jumpRange, float falloff,
            IReadOnlyList<TargetSelector.Candidate> candidates)
        {
            if (baseDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(baseDamage), "데미지는 음수일 수 없습니다.");
            if (maxTargets < 1)
                throw new ArgumentOutOfRangeException(nameof(maxTargets), "최대 타격 수는 1 이상이어야 합니다.");
            if (jumpRange <= 0f)
                throw new ArgumentOutOfRangeException(nameof(jumpRange), "점프 거리는 0보다 커야 합니다.");
            if (falloff <= 0f || falloff > 1f)
                throw new ArgumentOutOfRangeException(nameof(falloff), "감쇠 비율은 (0,1] 범위여야 합니다.");

            var hits = new List<ChainHit>();
            if (start.Unit == null || start.Unit.IsDead)
                return hits;

            var visited = new HashSet<EnemyUnit>();
            Vec2 currentPos = start.Position;
            int currentDamage = baseDamage;

            hits.Add(new ChainHit(start.Unit, currentDamage));
            visited.Add(start.Unit);

            while (hits.Count < maxTargets)
            {
                int nextIndex = FindNearestUnvisited(currentPos, jumpRange, candidates, visited);
                if (nextIndex < 0)
                    break;

                var next = candidates[nextIndex];
                currentDamage = (int)(currentDamage * falloff); // 점프마다 누적 감쇠(내림)
                hits.Add(new ChainHit(next.Unit, currentDamage));
                visited.Add(next.Unit);
                currentPos = next.Position;
            }

            return hits;
        }

        private static int FindNearestUnvisited(
            Vec2 from, float jumpRange,
            IReadOnlyList<TargetSelector.Candidate> candidates, HashSet<EnemyUnit> visited)
        {
            if (candidates == null)
                return -1;

            int bestIndex = -1;
            float bestDist = float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Unit == null || c.Unit.IsDead || visited.Contains(c.Unit))
                    continue;

                float dist = from.DistanceTo(c.Position);
                if (dist > jumpRange)
                    continue;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
