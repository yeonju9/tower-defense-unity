using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>적 6종 카탈로그의 스탯이 vision의 위협 유형(빠름/무리/튼튼/복합/보스)에 맞는지 검증.</summary>
    public class EnemyCatalogTests
    {
        [Test]
        public void 보스만_IsBoss이고_나머지는_아니다()
        {
            Assert.IsTrue(EnemyCatalog.MiniBoss.IsBoss);
            Assert.IsFalse(EnemyCatalog.Infantry.IsBoss);
            Assert.IsFalse(EnemyCatalog.Charger.IsBoss);
            Assert.IsFalse(EnemyCatalog.SwarmBug.IsBoss);
            Assert.IsFalse(EnemyCatalog.HeavyArmor.IsBoss);
            Assert.IsFalse(EnemyCatalog.Cavalry.IsBoss);
        }

        [Test]
        public void 돌격병은_보병보다_빠르고_체력이_낮다()
        {
            Assert.Greater(EnemyCatalog.Charger.Speed, EnemyCatalog.Infantry.Speed);
            Assert.Less(EnemyCatalog.Charger.MaxHp, EnemyCatalog.Infantry.MaxHp);
        }

        [Test]
        public void 군집벌레는_가장_약하다()
        {
            Assert.Less(EnemyCatalog.SwarmBug.MaxHp, EnemyCatalog.Infantry.MaxHp);
            Assert.Less(EnemyCatalog.SwarmBug.MaxHp, EnemyCatalog.Charger.MaxHp);
        }

        [Test]
        public void 중장갑병은_느리고_체력이_매우_높다()
        {
            Assert.Less(EnemyCatalog.HeavyArmor.Speed, EnemyCatalog.Infantry.Speed);
            Assert.Greater(EnemyCatalog.HeavyArmor.MaxHp, EnemyCatalog.Infantry.MaxHp * 3);
        }

        [Test]
        public void 질주기병은_빠르면서_체력도_어느정도다()
        {
            // 돌격병만큼 빠른 편이면서, 돌격병보다 훨씬 튼튼(복합 위협)
            Assert.Greater(EnemyCatalog.Cavalry.Speed, EnemyCatalog.Infantry.Speed);
            Assert.Greater(EnemyCatalog.Cavalry.MaxHp, EnemyCatalog.Charger.MaxHp);
        }

        [Test]
        public void 미니보스는_체력이_압도적이다()
        {
            Assert.Greater(EnemyCatalog.MiniBoss.MaxHp, EnemyCatalog.HeavyArmor.MaxHp);
        }

        private static GameLoop BossLoop(TowerUnit tower = null)
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(100f, 0f) };
            var schedule = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.MiniBoss),
            });
            var config = new GameLoopConfig(waypoints, startingGold: 0, startingLives: 10, schedule);
            var loop = new GameLoop(config);
            if (tower != null)
                loop.TryBuildTower(new Vec2(0f, 0f), cost: 0, tower);
            return loop;
        }

        [Test]
        public void 보스가_필드에_살아있으면_BossActive가_true()
        {
            var loop = BossLoop(); // 타워 없음 → 보스가 죽지 않고 남는다
            Assert.IsFalse(loop.BossActive); // 스폰 전

            loop.StartWave();
            loop.Update(0.01f); // 보스 스폰

            Assert.IsTrue(loop.BossActive);
        }

        [Test]
        public void 보스를_처치하면_BossActive가_false()
        {
            // 보스를 한 방에 죽일 수 있는 강력한 타워.
            var loop = BossLoop(new TowerUnit(range: 5f, fireInterval: 1f, damage: 1000));

            loop.StartWave();
            loop.Update(0.01f); // 보스 스폰 → 즉시 처치

            Assert.IsFalse(loop.BossActive);
            Assert.AreEqual(1, loop.KilledCount);
        }
    }
}
