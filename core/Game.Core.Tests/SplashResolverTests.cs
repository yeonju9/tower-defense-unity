using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class SplashResolverTests
    {
        private static EnemyUnit Bug() => new EnemyUnit(maxHp: 10, speed: 2f, goldReward: 3);

        private static TargetSelector.Candidate At(EnemyUnit u, float x, float y, float progress) =>
            new TargetSelector.Candidate(u, new Vec2(x, y), progress);

        [Test]
        public void Resolve_반경안의_모든적을_반환한다()
        {
            var resolver = new SplashResolver();
            var a = Bug(); var b = Bug(); var far = Bug();
            var candidates = new List<TargetSelector.Candidate>
            {
                At(a, 5f, 0f, 1f),
                At(b, 5.5f, 0f, 2f),
                At(far, 20f, 0f, 3f), // 착탄점에서 멀다
            };

            var hit = resolver.Resolve(new Vec2(5f, 0f), radius: 1f, candidates);

            CollectionAssert.Contains(hit, a);
            CollectionAssert.Contains(hit, b);
            CollectionAssert.DoesNotContain(hit, far);
            Assert.AreEqual(2, hit.Count);
        }

        [Test]
        public void Resolve_죽은적은_제외한다()
        {
            var resolver = new SplashResolver();
            var dead = Bug(); dead.TakeDamage(10);
            var alive = Bug();
            var candidates = new List<TargetSelector.Candidate>
            {
                At(dead, 5f, 0f, 1f),
                At(alive, 5f, 0f, 2f),
            };

            var hit = resolver.Resolve(new Vec2(5f, 0f), radius: 1f, candidates);

            Assert.AreEqual(1, hit.Count);
            CollectionAssert.Contains(hit, alive);
        }

        [Test]
        public void Resolve_반경밖만_있으면_빈목록()
        {
            var resolver = new SplashResolver();
            var candidates = new List<TargetSelector.Candidate> { At(Bug(), 100f, 0f, 1f) };
            var hit = resolver.Resolve(new Vec2(0f, 0f), radius: 1f, candidates);
            Assert.AreEqual(0, hit.Count);
        }
    }
}
