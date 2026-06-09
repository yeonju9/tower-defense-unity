using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class TowerSellTests
    {
        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };

        private static WaveSchedule EmptyWave() =>
            new WaveSchedule(new List<SpawnEntry>());

        private static TowerUnit Archer() => new TowerUnit(2.5f, 0.8f, 10);

        private static GameLoop MakeLoop(int gold, float refundRatio = 0.6f) =>
            new GameLoop(new GameLoopConfig(StraightPath(), gold, 10, EmptyWave(), refundRatio));

        [Test]
        public void Refund_골드를_되돌려준다()
        {
            var economy = new Economy(0);
            economy.Refund(24);
            Assert.AreEqual(24, economy.Gold);
        }

        [Test]
        public void 타워를_팔면_환불비율만큼_골드가_돌아오고_타워가_사라진다()
        {
            var loop = MakeLoop(gold: 100, refundRatio: 0.6f);
            var pos = new Vec2(0.5f, 0.5f);
            loop.TryBuildTower(pos, cost: 40, Archer()); // 골드 100 → 60
            Assert.AreEqual(1, loop.TowerCount);

            int refunded = loop.SellTowerAt(pos);

            Assert.AreEqual(24, refunded);           // floor(40 * 0.6)
            Assert.AreEqual(0, loop.TowerCount);
            Assert.AreEqual(84, loop.Economy.Gold);  // 60 + 24
        }

        [Test]
        public void 환불은_내림_처리된다()
        {
            var loop = MakeLoop(gold: 100, refundRatio: 0.6f);
            var pos = new Vec2(1f, 1f);
            loop.TryBuildTower(pos, cost: 35, Archer()); // 35 * 0.6 = 21.0 → 21
            int refunded = loop.SellTowerAt(pos);
            Assert.AreEqual(21, refunded);
        }

        [Test]
        public void 타워없는_위치를_팔면_minus1이고_아무변화없다()
        {
            var loop = MakeLoop(gold: 100);
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 40, Archer());

            int refunded = loop.SellTowerAt(new Vec2(9f, 9f)); // 빈 자리

            Assert.AreEqual(-1, refunded);
            Assert.AreEqual(1, loop.TowerCount);
            Assert.AreEqual(60, loop.Economy.Gold);
        }

        [Test]
        public void 환불비율이_범위밖이면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new GameLoopConfig(StraightPath(), 100, 10, EmptyWave(), refundRatio: 1.5f));
        }
    }
}
