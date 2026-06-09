using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class TowerSplashTests
    {
        [Test]
        public void 기본타워는_단일타깃이라_IsSplash가_false()
        {
            var archer = new TowerUnit(2.5f, 0.8f, 10);
            Assert.IsFalse(archer.IsSplash);
            Assert.AreEqual(0f, archer.SplashRadius, 1e-4f);
        }

        [Test]
        public void 범위반경을_주면_IsSplash가_true()
        {
            var cannon = new TowerUnit(3f, 1.5f, 12, splashRadius: 1.2f);
            Assert.IsTrue(cannon.IsSplash);
            Assert.AreEqual(1.2f, cannon.SplashRadius, 1e-4f);
        }

        [Test]
        public void 범위반경이_음수면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new TowerUnit(3f, 1.5f, 12, splashRadius: -1f));
        }

        [Test]
        public void SelectCandidate_타깃의_위치까지_반환한다()
        {
            var selector = new TargetSelector();
            var enemy = new EnemyUnit(30, 1.5f, 8);
            var candidates = new List<TargetSelector.Candidate>
            {
                new TargetSelector.Candidate(enemy, new Vec2(2f, 0f), progress: 5f),
            };

            var picked = selector.SelectCandidate(new Vec2(0f, 0f), range: 2.5f, candidates);

            Assert.IsTrue(picked.HasValue);
            Assert.AreSame(enemy, picked.Value.Unit);
            Assert.AreEqual(2f, picked.Value.Position.X, 1e-4f);
        }

        [Test]
        public void SelectCandidate_대상없으면_null()
        {
            var selector = new TargetSelector();
            var picked = selector.SelectCandidate(new Vec2(0f, 0f), 2.5f, new List<TargetSelector.Candidate>());
            Assert.IsFalse(picked.HasValue);
        }
    }
}
