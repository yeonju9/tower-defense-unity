using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>
    /// 타워 해금 시점(vision Open Question 1 표)과 업그레이드 비용·효과 곡선의 데이터화 검증.
    /// </summary>
    public class TowerProgressionTests
    {
        // --- 해금 시점 (0-based 스테이지 인덱스) ---

        [Test]
        public void 해금_시점이_vision_표와_일치한다()
        {
            Assert.AreEqual(0, TowerCatalog.Arrow.UnlockStageIndex);     // 처음부터
            Assert.AreEqual(1, TowerCatalog.Cannon.UnlockStageIndex);    // 스테이지 2
            Assert.AreEqual(2, TowerCatalog.Frost.UnlockStageIndex);     // 스테이지 3
            Assert.AreEqual(3, TowerCatalog.Sniper.UnlockStageIndex);    // 스테이지 4
            Assert.AreEqual(4, TowerCatalog.RapidFire.UnlockStageIndex); // 스테이지 5
            Assert.AreEqual(5, TowerCatalog.Poison.UnlockStageIndex);    // 스테이지 6
            Assert.AreEqual(6, TowerCatalog.Lightning.UnlockStageIndex); // 스테이지 7
            Assert.AreEqual(7, TowerCatalog.Gold.UnlockStageIndex);      // 스테이지 8
        }

        [Test]
        public void IsUnlockedAt은_해금_스테이지_이상에서_참이다()
        {
            Assert.IsTrue(TowerCatalog.Arrow.IsUnlockedAt(0));   // 화살탑은 처음부터
            Assert.IsFalse(TowerCatalog.Cannon.IsUnlockedAt(0)); // 대포탑은 아직
            Assert.IsTrue(TowerCatalog.Cannon.IsUnlockedAt(1));  // 스테이지2(인덱스1)부터
            Assert.IsTrue(TowerCatalog.Cannon.IsUnlockedAt(5));  // 이후 스테이지도 당연히 해금
        }

        [Test]
        public void UnlockedAt은_그_스테이지까지_열린_타워만_준다()
        {
            Assert.AreEqual(1, TowerCatalog.UnlockedAt(0).Count); // 화살탑만
            Assert.AreEqual(2, TowerCatalog.UnlockedAt(1).Count); // 화살+대포
            Assert.AreEqual(8, TowerCatalog.UnlockedAt(7).Count); // 8종 전부
            Assert.AreEqual(8, TowerCatalog.UnlockedAt(99).Count); // 그 이후도 전부
        }

        [Test]
        public void UnlockedAt_음수는_예외()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TowerCatalog.UnlockedAt(-1));
        }

        // --- 업그레이드 곡선 ---

        [Test]
        public void 업그레이드_비용은_레벨이_오를수록_커진다()
        {
            var spec = TowerCatalog.Arrow;
            var step1 = spec.UpgradeFrom(1).Value; // 1→2
            var step2 = spec.UpgradeFrom(2).Value; // 2→3
            Assert.Greater(step2.Cost, step1.Cost, "상위 레벨 업그레이드가 더 비싸야 한다");
        }

        [Test]
        public void 업그레이드는_데미지를_올린다()
        {
            var spec = TowerCatalog.Sniper;
            var step = spec.UpgradeFrom(1).Value;
            Assert.Greater(step.AddedDamage, 0);
        }

        [Test]
        public void 최대_레벨에서는_더_업그레이드할_수_없다()
        {
            var spec = TowerCatalog.Arrow;
            Assert.IsNotNull(spec.UpgradeFrom(TowerSpec.MaxLevel - 1)); // 마지막 업그레이드는 가능
            Assert.IsNull(spec.UpgradeFrom(TowerSpec.MaxLevel));        // 그 이상은 불가
        }

        [Test]
        public void UpgradeFrom_잘못된_레벨은_예외()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TowerCatalog.Arrow.UpgradeFrom(0));
        }

        [Test]
        public void 곡선대로_GameLoop에서_실제로_업그레이드된다()
        {
            var waypoints = new System.Collections.Generic.List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(10f, 0f),
            };
            var schedule = new WaveSchedule(new System.Collections.Generic.List<SpawnEntry>());
            var spec = TowerCatalog.Arrow;
            var loop = new GameLoop(new GameLoopConfig(waypoints, startingGold: 1000, startingLives: 10, schedule));
            var pos = new Vec2(1f, 1f);
            loop.TryBuildTower(pos, spec.Cost, spec.CreateUnit());

            var plan = spec.UpgradeFrom(1).Value;
            bool ok = loop.TryUpgradeTower(pos, plan.Cost, plan.AddedDamage, plan.AddedRange);

            Assert.IsTrue(ok);
            Assert.AreEqual(2, loop.SnapshotTowers()[0].Level);
        }
    }
}
