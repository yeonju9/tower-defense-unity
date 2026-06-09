using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class TowerUnitTests
    {
        private static TowerUnit MakeArcher() => new TowerUnit(range: 2.5f, fireInterval: 0.8f, damage: 10);

        [Test]
        public void 초기상태는_즉시발사가능()
        {
            var tower = MakeArcher();
            Assert.IsTrue(tower.CanFire);
        }

        [Test]
        public void OnFired_직후에는_발사불가()
        {
            var tower = MakeArcher();
            tower.OnFired();
            Assert.IsFalse(tower.CanFire);
        }

        [Test]
        public void Tick_발사간격만큼_지나면_다시_발사가능()
        {
            var tower = MakeArcher();
            tower.OnFired();
            tower.Tick(0.8f);
            Assert.IsTrue(tower.CanFire);
        }

        [Test]
        public void Tick_간격미만이면_여전히_발사불가()
        {
            var tower = MakeArcher();
            tower.OnFired();
            tower.Tick(0.5f);
            Assert.IsFalse(tower.CanFire);
        }
    }
}
