using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>골드탑(처치 보너스 골드): 막타로 적을 죽이면 처치 보상에 더해 추가 골드를 준다.</summary>
    public class GoldTowerTests
    {
        [Test]
        public void GoldBonus_효과는_보너스가_양수일_때만_적용된다()
        {
            var effect = TowerEffect.GoldBonus(50);
            Assert.AreEqual(50, effect.KillBonusGold);
            Assert.IsTrue(effect.AppliesKillBonus);
            Assert.IsFalse(default(TowerEffect).AppliesKillBonus);
        }

        private static GameLoop BuildLoop(int enemyHp, int towerDamage, TowerEffect effect)
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };
            var schedule = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, new EnemySpec(enemyHp, 0.1f, goldReward: 1)),
            });
            var config = new GameLoopConfig(waypoints, startingGold: 0, startingLives: 10, schedule);
            var loop = new GameLoop(config);
            var tower = new TowerUnit(range: 5f, fireInterval: 1f, damage: towerDamage, effect: effect);
            loop.TryBuildTower(new Vec2(0f, 0f), cost: 0, tower);
            return loop;
        }

        [Test]
        public void 골드탑이_막타로_죽이면_처치보상과_보너스골드를_모두_받는다()
        {
            var loop = BuildLoop(enemyHp: 30, towerDamage: 100, TowerEffect.GoldBonus(50));

            loop.StartWave();
            loop.Update(0.1f); // 한 발에 처치

            Assert.AreEqual(0, loop.ActiveEnemyCount);
            Assert.AreEqual(1, loop.KilledCount);
            Assert.AreEqual(51, loop.Economy.Gold); // 처치보상 1 + 보너스 50
        }

        [Test]
        public void 막타로_못_죽이면_보너스골드는_없다()
        {
            var loop = BuildLoop(enemyHp: 100, towerDamage: 20, TowerEffect.GoldBonus(50));

            loop.StartWave();
            loop.Update(0.1f); // 데미지만 입고 생존

            Assert.AreEqual(1, loop.ActiveEnemyCount);
            Assert.AreEqual(0, loop.Economy.Gold); // 보너스 없음
        }

        [Test]
        public void 보너스가_없는_일반탑은_처치보상만_받는다()
        {
            var loop = BuildLoop(enemyHp: 30, towerDamage: 100, default);

            loop.StartWave();
            loop.Update(0.1f);

            Assert.AreEqual(1, loop.Economy.Gold); // 처치보상 1만
        }
    }
}
