using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class WavePreviewTests
    {
        private static EnemySpec Infantry() => new EnemySpec(30, 1.5f, 8);

        private static WaveSchedule ThreeAtOneSecondApart()
        {
            var spec = Infantry();
            return new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, spec),
                new SpawnEntry(1f, spec),
                new SpawnEntry(2f, spec),
            });
        }

        [Test]
        public void PeekRemaining_소비전에는_전체를_보여준다()
        {
            var schedule = ThreeAtOneSecondApart();
            Assert.AreEqual(3, schedule.PeekRemaining().Count);
        }

        [Test]
        public void PeekRemaining_은_소비하지_않는다()
        {
            var schedule = ThreeAtOneSecondApart();
            schedule.PeekRemaining();
            schedule.PeekRemaining();
            // 미리보기를 여러 번 봐도 실제 스폰은 그대로 남아있어야 한다.
            Assert.AreEqual(3, schedule.Collect(10f).Count);
        }

        [Test]
        public void PeekRemaining_일부_스폰후에는_남은것만_보여준다()
        {
            var schedule = ThreeAtOneSecondApart();
            schedule.Collect(1f); // 0초,1초 스폰 소비 → 1개 남음
            Assert.AreEqual(1, schedule.PeekRemaining().Count);
        }

        [Test]
        public void PeekRemaining_모두소비후에는_빈목록()
        {
            var schedule = ThreeAtOneSecondApart();
            schedule.Collect(10f);
            Assert.AreEqual(0, schedule.PeekRemaining().Count);
        }

        [Test]
        public void GameLoop_UpcomingSpawns로_미리보기를_노출한다()
        {
            var path = new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };
            var loop = new GameLoop(new GameLoopConfig(path, 100, 10, ThreeAtOneSecondApart()));
            Assert.AreEqual(3, loop.UpcomingSpawns.Count);
        }
    }
}
