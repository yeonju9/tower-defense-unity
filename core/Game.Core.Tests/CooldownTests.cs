using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class CooldownTests
    {
        [Test]
        public void 초기상태는_사용가능()
        {
            var cd = new Cooldown(30f);
            Assert.IsTrue(cd.IsReady);
        }

        [Test]
        public void Trigger_직후에는_사용불가이고_Remaining이_가득찬다()
        {
            var cd = new Cooldown(30f);
            cd.Trigger();
            Assert.IsFalse(cd.IsReady);
            Assert.AreEqual(30f, cd.Remaining, 1e-4f);
        }

        [Test]
        public void Tick_쿨다운만큼_지나면_다시_사용가능()
        {
            var cd = new Cooldown(30f);
            cd.Trigger();
            cd.Tick(30f);
            Assert.IsTrue(cd.IsReady);
        }

        [Test]
        public void Tick_쿨다운_미만이면_여전히_사용불가()
        {
            var cd = new Cooldown(30f);
            cd.Trigger();
            cd.Tick(10f);
            Assert.IsFalse(cd.IsReady);
            Assert.AreEqual(20f, cd.Remaining, 1e-4f);
        }

        [Test]
        public void 생성자_쿨다운이_0이하면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Cooldown(0f));
        }
    }
}
