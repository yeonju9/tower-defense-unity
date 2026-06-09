using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class EconomyTests
    {
        [Test]
        public void AddKillReward_적처치시_골드가_보상만큼_증가한다()
        {
            var economy = new Economy(startingGold: 100);
            economy.AddKillReward(25);
            Assert.AreEqual(125, economy.Gold);
        }

        [Test]
        public void Spend_충분하면_골드가_차감된다()
        {
            var economy = new Economy(startingGold: 100);
            economy.Spend(40);
            Assert.AreEqual(60, economy.Gold);
        }

        [Test]
        public void Spend_골드부족시_예외를_던지고_잔액은_그대로다()
        {
            var economy = new Economy(startingGold: 30);
            Assert.Throws<InsufficientGoldException>(() => economy.Spend(50));
            Assert.AreEqual(30, economy.Gold);
        }

        [Test]
        public void 생성자_시작골드가_음수면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Economy(-1));
        }
    }
}
