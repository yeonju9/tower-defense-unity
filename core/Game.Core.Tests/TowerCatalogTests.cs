using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>타워 8종 카탈로그가 vision의 역할군(딜러/광역/유틸)에 맞는 스탯·효과·비용을 갖는지 검증.</summary>
    public class TowerCatalogTests
    {
        [Test]
        public void 모든_타워는_양수_비용을_가진다()
        {
            foreach (var spec in TowerCatalog.All())
                Assert.Greater(spec.Cost, 0, $"{spec.Name} 비용");
        }

        [Test]
        public void CreateUnit은_스펙대로_타워를_만든다()
        {
            var spec = TowerCatalog.Arrow;
            var unit = spec.CreateUnit();
            Assert.AreEqual(spec.Range, unit.Range);
            Assert.AreEqual(spec.Damage, unit.Damage);
        }

        [Test]
        public void 화살탑은_단일타깃_기본기다()
        {
            var u = TowerCatalog.Arrow.CreateUnit();
            Assert.IsFalse(u.IsSplash);
            Assert.IsFalse(u.IsChain);
        }

        [Test]
        public void 대포탑은_광역이다()
        {
            Assert.IsTrue(TowerCatalog.Cannon.CreateUnit().IsSplash);
        }

        [Test]
        public void 빙결탑은_둔화를_걸고_데미지가_낮다()
        {
            var frost = TowerCatalog.Frost.CreateUnit();
            Assert.IsTrue(frost.Effect.AppliesSlow);
            Assert.Less(frost.Damage, TowerCatalog.Arrow.Damage); // 단독으론 약함
        }

        [Test]
        public void 저격탑은_사거리가_길고_데미지가_높으며_느리다()
        {
            var sniper = TowerCatalog.Sniper;
            Assert.Greater(sniper.Range, TowerCatalog.Arrow.Range);
            Assert.Greater(sniper.Damage, TowerCatalog.Arrow.Damage);
            Assert.Greater(sniper.FireInterval, TowerCatalog.Arrow.FireInterval); // 느린 발사
        }

        [Test]
        public void 연사탑은_사거리가_짧고_발사가_매우_빠르다()
        {
            var rapid = TowerCatalog.RapidFire;
            Assert.Less(rapid.Range, TowerCatalog.Arrow.Range);
            Assert.Less(rapid.FireInterval, TowerCatalog.Arrow.FireInterval); // 초고속 연사
        }

        [Test]
        public void 독탑은_지속피해를_건다()
        {
            Assert.IsTrue(TowerCatalog.Poison.CreateUnit().Effect.AppliesPoison);
        }

        [Test]
        public void 번개탑은_체인이다()
        {
            Assert.IsTrue(TowerCatalog.Lightning.CreateUnit().IsChain);
        }

        [Test]
        public void 골드탑은_처치_보너스를_준다()
        {
            Assert.IsTrue(TowerCatalog.Gold.CreateUnit().Effect.AppliesKillBonus);
        }

        [Test]
        public void All은_타워_8종을_반환한다()
        {
            Assert.AreEqual(8, TowerCatalog.All().Count);
        }
    }
}
