using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class WaveStartTimerTests
    {
        private const float Tolerance = 1e-4f;

        // 준비 10초, 초당 보너스 2골드
        private static WaveStartTimer Make() => new WaveStartTimer(prepDuration: 10f, bonusPerSecond: 2);

        [Test]
        public void 초기_Remaining은_준비시간_전체()
        {
            var timer = Make();
            Assert.AreEqual(10f, timer.Remaining, Tolerance);
            Assert.IsFalse(timer.Expired);
            Assert.IsFalse(timer.Started);
        }

        [Test]
        public void Tick_경과만큼_Remaining이_준다()
        {
            var timer = Make();
            timer.Tick(3f);
            Assert.AreEqual(7f, timer.Remaining, Tolerance);
        }

        [Test]
        public void 즉시시작하면_최대보너스()
        {
            var timer = Make();
            int bonus = timer.Start(); // 남은 10초 × 2
            Assert.AreEqual(20, bonus);
            Assert.IsTrue(timer.Started);
        }

        [Test]
        public void 일부경과후_남은시간에_비례한_보너스()
        {
            var timer = Make();
            timer.Tick(6f);            // 남은 4초
            int bonus = timer.Start(); // 4 × 2
            Assert.AreEqual(8, bonus);
        }

        [Test]
        public void 준비시간_만료후_시작하면_보너스0()
        {
            var timer = Make();
            timer.Tick(100f);
            Assert.IsTrue(timer.Expired);
            Assert.AreEqual(0, timer.Start());
        }

        [Test]
        public void 두번째_Start는_보너스0()
        {
            var timer = Make();
            Assert.AreEqual(20, timer.Start());
            Assert.AreEqual(0, timer.Start()); // 중복 시작 방지
        }

        [Test]
        public void 시작후에는_Tick이_경과를_누적하지_않는다()
        {
            var timer = Make();
            timer.Start();
            timer.Tick(5f);
            Assert.AreEqual(0f, timer.Elapsed, Tolerance);
        }

        [Test]
        public void 생성자_준비시간이_0이하면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new WaveStartTimer(0f, 2));
        }
    }
}
