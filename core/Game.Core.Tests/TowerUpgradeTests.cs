using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class TowerUpgradeTests
    {
        private const float Tolerance = 1e-4f;

        // --- TowerUnit 단위 ---

        [Test]
        public void 초기레벨은_1()
        {
            var tower = new TowerUnit(2.5f, 0.8f, 10);
            Assert.AreEqual(1, tower.Level);
        }

        [Test]
        public void Upgrade_데미지와_사거리가_오르고_레벨이_증가한다()
        {
            var tower = new TowerUnit(2.5f, 0.8f, 10);
            tower.Upgrade(addedDamage: 5, addedRange: 0.5f);
            Assert.AreEqual(15, tower.Damage);
            Assert.AreEqual(3.0f, tower.Range, Tolerance);
            Assert.AreEqual(2, tower.Level);
        }

        [Test]
        public void Upgrade_음수증가는_예외()
        {
            var tower = new TowerUnit(2.5f, 0.8f, 10);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => tower.Upgrade(-1));
        }

        // --- GameLoop 연동 ---

        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };

        private static GameLoop MakeLoop(int gold) =>
            new GameLoop(new GameLoopConfig(StraightPath(), gold, 10, new WaveSchedule(new List<SpawnEntry>())));

        [Test]
        public void TryUpgradeTower_골드차감하고_스탯이_오른다()
        {
            var loop = MakeLoop(gold: 100);
            var pos = new Vec2(1f, 1f);
            var tower = new TowerUnit(2.5f, 0.8f, 10);
            loop.TryBuildTower(pos, cost: 40, tower); // 100 → 60

            bool ok = loop.TryUpgradeTower(pos, cost: 30, addedDamage: 5);

            Assert.IsTrue(ok);
            Assert.AreEqual(30, loop.Economy.Gold); // 60 → 30
            Assert.AreEqual(15, tower.Damage);
            Assert.AreEqual(2, tower.Level);
        }

        [Test]
        public void TryUpgradeTower_골드부족이면_거부하고_불변()
        {
            var loop = MakeLoop(gold: 50);
            var pos = new Vec2(1f, 1f);
            var tower = new TowerUnit(2.5f, 0.8f, 10);
            loop.TryBuildTower(pos, cost: 40, tower); // 50 → 10

            bool ok = loop.TryUpgradeTower(pos, cost: 30, addedDamage: 5); // 10골드로 30 불가

            Assert.IsFalse(ok);
            Assert.AreEqual(10, loop.Economy.Gold);
            Assert.AreEqual(10, tower.Damage); // 불변
            Assert.AreEqual(1, tower.Level);
        }

        [Test]
        public void TryUpgradeTower_타워없는_위치면_false()
        {
            var loop = MakeLoop(gold: 100);
            bool ok = loop.TryUpgradeTower(new Vec2(9f, 9f), cost: 30, addedDamage: 5);
            Assert.IsFalse(ok);
            Assert.AreEqual(100, loop.Economy.Gold); // 차감 없음
        }
    }
}
