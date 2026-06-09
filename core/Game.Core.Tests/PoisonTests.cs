using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class PoisonTests
    {
        private static EnemyUnit Enemy(int hp = 100) =>
            new EnemyUnit(maxHp: hp, speed: 1f, goldReward: 5);

        [Test]
        public void ApplyPoison_TickStatus로_지속피해를_입는다()
        {
            var e = Enemy(hp: 100);
            e.ApplyPoison(dps: 5, duration: 3f);
            Assert.IsTrue(e.IsPoisoned);
            e.TickStatus(1f); // 5 피해
            Assert.AreEqual(95, e.Hp);
        }

        [Test]
        public void ApplyPoison_중첩되면_피해가_합산된다()
        {
            var e = Enemy(hp: 100);
            e.ApplyPoison(5, 3f);
            e.ApplyPoison(5, 3f); // 두 스택 → 10/초
            e.TickStatus(1f);
            Assert.AreEqual(90, e.Hp);
        }

        [Test]
        public void TickStatus_소수피해는_정수단위로_누적되어_적용된다()
        {
            var e = Enemy(hp: 100);
            e.ApplyPoison(dps: 5, duration: 10f); // 0.1초당 0.5 피해
            e.TickStatus(0.1f);                   // 누적 0.5 → 정수 0, 아직 피해 없음
            Assert.AreEqual(100, e.Hp);
            e.TickStatus(0.1f);                   // 누적 1.0 → 1 피해
            Assert.AreEqual(99, e.Hp);
        }

        [Test]
        public void TickStatus_독_지속시간이_지나면_피해가_멈춘다()
        {
            var e = Enemy(hp: 100);
            e.ApplyPoison(dps: 10, duration: 1f);
            e.TickStatus(1f);  // 10 피해 후 만료
            Assert.AreEqual(90, e.Hp);
            Assert.IsFalse(e.IsPoisoned);
            e.TickStatus(1f);  // 더 이상 피해 없음
            Assert.AreEqual(90, e.Hp);
        }

        [Test]
        public void ApplyPoison_dps가_0이하면_예외()
        {
            var e = Enemy();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => e.ApplyPoison(0, 1f));
        }
    }
}
