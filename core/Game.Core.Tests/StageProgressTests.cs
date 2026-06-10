using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>스테이지 해금 규칙: 0번은 항상 열려 있고, N번은 N-1을 클리어해야 열린다.</summary>
    public class StageProgressTests
    {
        [Test]
        public void 첫_스테이지는_항상_해금이다()
        {
            var progress = new StageProgress(new SaveStore());
            Assert.IsTrue(progress.IsUnlocked(0));
        }

        [Test]
        public void 이전_스테이지를_클리어해야_다음이_해금된다()
        {
            var store = new SaveStore();
            var progress = new StageProgress(store);

            Assert.IsFalse(progress.IsUnlocked(1)); // 0번 미클리어 → 1번 잠김

            store.RecordClear(0, 3);
            Assert.IsTrue(progress.IsUnlocked(1)); // 0번 클리어 → 1번 해금
            Assert.IsFalse(progress.IsUnlocked(2)); // 1번은 아직
        }

        [Test]
        public void 클리어가_이어지면_해금도_이어진다()
        {
            var store = new SaveStore();
            store.RecordClear(0, 1);
            store.RecordClear(1, 2);
            var progress = new StageProgress(store);

            Assert.IsTrue(progress.IsUnlocked(2));
            Assert.IsFalse(progress.IsUnlocked(3));
        }

        [Test]
        public void NextStageToPlay는_해금됐지만_아직_클리어하지_않은_첫_스테이지다()
        {
            var store = new SaveStore();
            var progress = new StageProgress(store);
            Assert.AreEqual(0, progress.NextStageToPlay(totalStages: 3)); // 아무것도 안 함

            store.RecordClear(0, 3);
            Assert.AreEqual(1, progress.NextStageToPlay(3));

            store.RecordClear(1, 3);
            Assert.AreEqual(2, progress.NextStageToPlay(3));
        }

        [Test]
        public void 모두_클리어하면_NextStageToPlay는_마지막_스테이지를_가리킨다()
        {
            var store = new SaveStore();
            store.RecordClear(0, 3);
            store.RecordClear(1, 3);
            store.RecordClear(2, 3);
            var progress = new StageProgress(store);

            Assert.AreEqual(2, progress.NextStageToPlay(totalStages: 3)); // 더 갈 곳 없음 → 마지막
        }

        [Test]
        public void 잘못된_인자는_예외()
        {
            var progress = new StageProgress(new SaveStore());
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.IsUnlocked(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.NextStageToPlay(0));
            Assert.Throws<ArgumentNullException>(() => new StageProgress(null));
        }
    }
}
