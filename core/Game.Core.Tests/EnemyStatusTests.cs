using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class EnemyStatusTests
    {
        private const float Tolerance = 1e-4f;

        private static EnemyUnit Enemy(int hp = 30, float speed = 2f) =>
            new EnemyUnit(maxHp: hp, speed: speed, goldReward: 5);

        // --- 둔화 ---

        [Test]
        public void 둔화가_없으면_EffectiveSpeed는_Speed와_같다()
        {
            var e = Enemy(speed: 2f);
            Assert.AreEqual(2f, e.EffectiveSpeed, Tolerance);
            Assert.IsFalse(e.IsSlowed);
        }

        [Test]
        public void ApplySlow_EffectiveSpeed가_느려진다()
        {
            var e = Enemy(speed: 2f);
            e.ApplySlow(factor: 0.5f, duration: 2f);
            Assert.AreEqual(1f, e.EffectiveSpeed, Tolerance);
            Assert.IsTrue(e.IsSlowed);
        }

        [Test]
        public void TickStatus_지속시간이_지나면_원래속도로_복귀한다()
        {
            var e = Enemy(speed: 2f);
            e.ApplySlow(0.5f, duration: 1f);
            e.TickStatus(1.0f); // 지속시간 소진
            Assert.AreEqual(2f, e.EffectiveSpeed, Tolerance);
            Assert.IsFalse(e.IsSlowed);
        }

        [Test]
        public void ApplySlow_더_강한_둔화가_우선한다()
        {
            var e = Enemy(speed: 2f);
            e.ApplySlow(0.6f, 2f);
            e.ApplySlow(0.3f, 1f); // 더 강한 둔화
            Assert.AreEqual(0.6f, e.EffectiveSpeed, Tolerance); // 2 * 0.3
        }

        [Test]
        public void ApplySlow_계수가_범위밖이면_예외()
        {
            var e = Enemy();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => e.ApplySlow(0f, 1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => e.ApplySlow(1.5f, 1f));
        }
    }
}
