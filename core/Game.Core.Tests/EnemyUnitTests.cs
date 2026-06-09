using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class EnemyUnitTests
    {
        [Test]
        public void TakeDamage_체력이_감소한다()
        {
            var enemy = new EnemyUnit(maxHp: 30, speed: 1.5f, goldReward: 8);
            enemy.TakeDamage(10);
            Assert.AreEqual(20, enemy.Hp);
        }

        [Test]
        public void TakeDamage_체력은_0미만으로_안내려간다()
        {
            var enemy = new EnemyUnit(maxHp: 30, speed: 1.5f, goldReward: 8);
            enemy.TakeDamage(100);
            Assert.AreEqual(0, enemy.Hp);
        }

        [Test]
        public void IsDead_체력0이면_true()
        {
            var enemy = new EnemyUnit(maxHp: 10, speed: 1f, goldReward: 5);
            Assert.IsFalse(enemy.IsDead);
            enemy.TakeDamage(10);
            Assert.IsTrue(enemy.IsDead);
        }

        [Test]
        public void TakeDamage_이미죽은적은_상태불변()
        {
            var enemy = new EnemyUnit(maxHp: 10, speed: 1f, goldReward: 5);
            enemy.TakeDamage(10);   // 사망
            enemy.TakeDamage(10);   // 추가 피해는 무시되어야 함(중복 처치 골드 방지의 근거)
            Assert.AreEqual(0, enemy.Hp);
            Assert.IsTrue(enemy.IsDead);
        }

        [Test]
        public void EnemySpec로_생성하면_스탯이_그대로_반영된다()
        {
            var spec = new EnemySpec(maxHp: 30, speed: 1.5f, goldReward: 8);
            var enemy = new EnemyUnit(spec);
            Assert.AreEqual(30, enemy.Hp);
            Assert.AreEqual(1.5f, enemy.Speed, 1e-4f);
            Assert.AreEqual(8, enemy.GoldReward);
        }
    }
}
