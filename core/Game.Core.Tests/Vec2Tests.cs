using System;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class Vec2Tests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void DistanceTo_두점거리를_정확히계산한다()
        {
            var a = new Vec2(0f, 0f);
            var b = new Vec2(3f, 4f);
            Assert.AreEqual(5f, a.DistanceTo(b), Tolerance);
        }

        [Test]
        public void MoveTowards_목표까지_거리보다_큰스텝이면_목표에_정확히_도달한다()
        {
            var from = new Vec2(0f, 0f);
            var target = new Vec2(3f, 4f); // 거리 5

            var result = from.MoveTowards(target, 10f);

            Assert.AreEqual(target.X, result.X, Tolerance);
            Assert.AreEqual(target.Y, result.Y, Tolerance);
        }

        [Test]
        public void MoveTowards_스텝이_작으면_그만큼만_이동한다()
        {
            var from = new Vec2(0f, 0f);
            var target = new Vec2(10f, 0f);

            var result = from.MoveTowards(target, 3f);

            Assert.AreEqual(3f, result.X, Tolerance);
            Assert.AreEqual(0f, result.Y, Tolerance);
            Assert.AreEqual(3f, from.DistanceTo(result), Tolerance);
        }

        [Test]
        public void MoveTowards_음수스텝이면_예외()
        {
            var from = new Vec2(0f, 0f);
            Assert.Throws<ArgumentOutOfRangeException>(() => from.MoveTowards(new Vec2(1f, 1f), -1f));
        }
    }
}
