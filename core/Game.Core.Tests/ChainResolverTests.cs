using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>
    /// 번개탑(체인 타격) 로직. 시작 적에서 jumpRange 안의 '가장 가까운 아직 안 맞은 적'으로
    /// maxTargets만큼 순차 점프하며, 점프할 때마다 데미지가 falloff 비율로 감쇠한다.
    /// </summary>
    public class ChainResolverTests
    {
        private static TargetSelector.Candidate At(float x, EnemyUnit unit = null) =>
            new TargetSelector.Candidate(unit ?? new EnemyUnit(100, 1f, 1), new Vec2(x, 0f), x);

        [Test]
        public void 일렬로_늘어선_적을_maxTargets만큼_데미지_감쇠하며_타격한다()
        {
            var resolver = new ChainResolver();
            var c0 = At(0f);
            var c1 = At(1f);
            var c2 = At(2f);
            var c3 = At(3f);
            var candidates = new List<TargetSelector.Candidate> { c0, c1, c2, c3 };

            var hits = resolver.Resolve(c0, baseDamage: 40, maxTargets: 3,
                jumpRange: 1.5f, falloff: 0.5f, candidates: candidates);

            Assert.AreEqual(3, hits.Count);
            Assert.AreSame(c0.Unit, hits[0].Unit);
            Assert.AreEqual(40, hits[0].Damage);
            Assert.AreSame(c1.Unit, hits[1].Unit);
            Assert.AreEqual(20, hits[1].Damage); // 40 * 0.5
            Assert.AreSame(c2.Unit, hits[2].Unit);
            Assert.AreEqual(10, hits[2].Damage); // 20 * 0.5
        }

        [Test]
        public void jumpRange_밖이면_더_이상_튕기지_않는다()
        {
            var resolver = new ChainResolver();
            var c0 = At(0f);
            var c1 = At(1f);
            var far = At(5f); // c1에서 4칸 떨어져 점프 불가

            var hits = resolver.Resolve(c0, baseDamage: 30, maxTargets: 5,
                jumpRange: 1.5f, falloff: 0.5f,
                candidates: new List<TargetSelector.Candidate> { c0, c1, far });

            Assert.AreEqual(2, hits.Count); // c0, c1 까지만
        }

        [Test]
        public void 항상_가장_가까운_적부터_점프한다()
        {
            var resolver = new ChainResolver();
            var c0 = At(0f);
            var near = At(1f);
            var mid = At(2f);
            // 입력 순서를 일부러 섞는다.
            var candidates = new List<TargetSelector.Candidate> { c0, mid, near };

            var hits = resolver.Resolve(c0, baseDamage: 30, maxTargets: 3,
                jumpRange: 1.5f, falloff: 1f, candidates: candidates);

            Assert.AreEqual(3, hits.Count);
            Assert.AreSame(near.Unit, hits[1].Unit); // 0 → 1(가까움) → 2
            Assert.AreSame(mid.Unit, hits[2].Unit);
        }

        [Test]
        public void 죽은_적은_점프_대상에서_제외된다()
        {
            var resolver = new ChainResolver();
            var c0 = At(0f);
            var deadUnit = new EnemyUnit(10, 1f, 1);
            deadUnit.TakeDamage(10); // 사망
            var dead = At(1f, deadUnit);
            var alive = At(2f);

            var hits = resolver.Resolve(c0, baseDamage: 30, maxTargets: 3,
                jumpRange: 1.5f, falloff: 1f,
                candidates: new List<TargetSelector.Candidate> { c0, dead, alive });

            // c0 → (1칸 거리의 죽은적 스킵) → 2칸 거리의 산 적은 jumpRange 1.5 밖이라 점프 불가
            Assert.AreEqual(1, hits.Count);
            Assert.AreSame(c0.Unit, hits[0].Unit);
        }

        [Test]
        public void 같은_적을_두_번_타격하지_않는다()
        {
            var resolver = new ChainResolver();
            var c0 = At(0f);
            var c1 = At(1f);

            var hits = resolver.Resolve(c0, baseDamage: 30, maxTargets: 5,
                jumpRange: 1.5f, falloff: 1f,
                candidates: new List<TargetSelector.Candidate> { c0, c1 });

            Assert.AreEqual(2, hits.Count); // 더 점프할 새 적이 없으면 멈춤(왕복 금지)
        }

        [Test]
        public void 시작_적이_죽어있으면_빈_결과()
        {
            var resolver = new ChainResolver();
            var deadUnit = new EnemyUnit(10, 1f, 1);
            deadUnit.TakeDamage(10);
            var start = At(0f, deadUnit);

            var hits = resolver.Resolve(start, baseDamage: 30, maxTargets: 3,
                jumpRange: 1.5f, falloff: 1f,
                candidates: new List<TargetSelector.Candidate> { start });

            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void 잘못된_인자는_예외()
        {
            var resolver = new ChainResolver();
            var c0 = At(0f);
            var list = new List<TargetSelector.Candidate> { c0 };

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                resolver.Resolve(c0, 30, maxTargets: 0, jumpRange: 1f, falloff: 0.5f, candidates: list));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                resolver.Resolve(c0, 30, maxTargets: 3, jumpRange: 0f, falloff: 0.5f, candidates: list));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                resolver.Resolve(c0, 30, maxTargets: 3, jumpRange: 1f, falloff: 0f, candidates: list));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                resolver.Resolve(c0, 30, maxTargets: 3, jumpRange: 1f, falloff: 1.5f, candidates: list));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                resolver.Resolve(c0, -1, maxTargets: 3, jumpRange: 1f, falloff: 0.5f, candidates: list));
        }
    }
}
