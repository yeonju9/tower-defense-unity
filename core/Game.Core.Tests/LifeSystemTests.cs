using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class LifeSystemTests
    {
        [Test]
        public void LoseLife_라이프가_감소한다()
        {
            var life = new LifeSystem(10);
            life.LoseLife();
            Assert.AreEqual(9, life.Lives);
        }

        [Test]
        public void LoseLife_0이하로는_안내려간다()
        {
            var life = new LifeSystem(2);
            life.LoseLife(5);
            Assert.AreEqual(0, life.Lives);
        }

        [Test]
        public void IsDepleted_라이프0이면_true()
        {
            var life = new LifeSystem(1);
            Assert.IsFalse(life.IsDepleted);
            life.LoseLife();
            Assert.IsTrue(life.IsDepleted);
        }

        [Test]
        public void 생성자_시작라이프가_0이하면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LifeSystem(0));
        }
    }
}
