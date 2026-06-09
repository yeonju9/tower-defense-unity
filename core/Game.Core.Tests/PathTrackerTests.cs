using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class PathTrackerTests
    {
        private const float Tolerance = 1e-4f;

        private static PathTracker StraightLine() =>
            new PathTracker(new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) });

        // (0,0) -> (0,5) -> (5,5)  : ㄱ자 경로, 총 길이 10
        private static PathTracker LShape() =>
            new PathTracker(new List<Vec2> { new Vec2(0f, 0f), new Vec2(0f, 5f), new Vec2(5f, 5f) });

        [Test]
        public void 초기위치는_첫_waypoint()
        {
            var path = StraightLine();
            Assert.AreEqual(0f, path.Position.X, Tolerance);
            Assert.AreEqual(0f, path.Position.Y, Tolerance);
            Assert.IsFalse(path.ReachedEnd);
        }

        [Test]
        public void Advance_직선구간에서_거리만큼_전진한다()
        {
            var path = StraightLine();
            path.Advance(3f);
            Assert.AreEqual(3f, path.Position.X, Tolerance);
            Assert.AreEqual(0f, path.Position.Y, Tolerance);
            Assert.AreEqual(3f, path.Progress, Tolerance);
            Assert.IsFalse(path.ReachedEnd);
        }

        [Test]
        public void Advance_waypoint를_넘어가면_다음구간으로_이어진다()
        {
            var path = LShape();
            path.Advance(7f);   // 첫 구간 5 소비 후, 둘째 구간 2 진행 → (2,5)
            Assert.AreEqual(2f, path.Position.X, Tolerance);
            Assert.AreEqual(5f, path.Position.Y, Tolerance);
            Assert.AreEqual(7f, path.Progress, Tolerance);
            Assert.IsFalse(path.ReachedEnd);
        }

        [Test]
        public void Advance_마지막_waypoint를_넘으면_ReachedEnd이고_끝점에_고정된다()
        {
            var path = LShape();
            path.Advance(100f); // 총 길이 10보다 큼
            Assert.IsTrue(path.ReachedEnd);
            Assert.AreEqual(5f, path.Position.X, Tolerance);
            Assert.AreEqual(5f, path.Position.Y, Tolerance);
        }

        [Test]
        public void 생성자_waypoint가_2개미만이면_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => new PathTracker(new List<Vec2> { new Vec2(0f, 0f) }));
        }
    }
}
