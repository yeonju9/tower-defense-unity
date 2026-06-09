using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 범위 피해(대포탑 등)의 착탄점 반경 안에 들어오는 적들을 산출한다.
    /// 단일 타깃 타워는 이 로직을 쓰지 않는다(GameLoop에서 분기).
    /// </summary>
    public class SplashResolver
    {
        /// <summary>착탄점에서 radius 이내에 있는, 살아있는 적들을 모두 반환한다.</summary>
        public IReadOnlyList<EnemyUnit> Resolve(
            Vec2 impactPoint, float radius, IReadOnlyList<TargetSelector.Candidate> candidates)
        {
            var hit = new List<EnemyUnit>();
            if (candidates == null)
                return hit;

            foreach (var c in candidates)
            {
                if (c.Unit == null || c.Unit.IsDead)
                    continue;
                if (impactPoint.DistanceTo(c.Position) <= radius)
                    hit.Add(c.Unit);
            }

            return hit;
        }
    }
}
