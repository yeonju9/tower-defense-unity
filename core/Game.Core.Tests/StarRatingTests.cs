using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class StarRatingTests
    {
        [Test]
        public void 라이프를_모두_지키면_3성()
        {
            Assert.AreEqual(3, StarRating.FromLives(livesRemaining: 10, startingLives: 10));
        }

        [Test]
        public void 라이프를_조금만_잃어도_3성은_아니다()
        {
            Assert.AreEqual(2, StarRating.FromLives(9, 10));
        }

        [Test]
        public void 절반_이상_지키면_2성()
        {
            Assert.AreEqual(2, StarRating.FromLives(5, 10)); // 정확히 절반
        }

        [Test]
        public void 절반_미만이면_1성()
        {
            Assert.AreEqual(1, StarRating.FromLives(4, 10));
        }

        [Test]
        public void 시작라이프가_0이하면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => StarRating.FromLives(1, 0));
        }
    }
}
