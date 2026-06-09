using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class WaveScheduleTests
    {
        private static EnemySpec Infantry() => new EnemySpec(maxHp: 30, speed: 1.5f, goldReward: 8);

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
        public void Collect_도래한_스폰만_반환한다()
        {
            var schedule = ThreeAtOneSecondApart();
            var due = schedule.Collect(0f); // 0초: 첫 스폰만
            Assert.AreEqual(1, due.Count);
            Assert.IsFalse(schedule.AllSpawned);
        }

        [Test]
        public void Collect_같은스폰을_두번_반환하지않는다()
        {
            var schedule = ThreeAtOneSecondApart();
            schedule.Collect(0f);             // 첫 스폰 소비
            var again = schedule.Collect(0f); // 같은 시각 재호출 → 새로 도래한 것 없음
            Assert.AreEqual(0, again.Count);
        }

        [Test]
        public void Collect_시간이_여러스폰을_한번에_지나치면_모두_반환한다()
        {
            var schedule = ThreeAtOneSecondApart();
            var due = schedule.Collect(5f); // 0,1,2초 스폰이 한 번에
            Assert.AreEqual(3, due.Count);
        }

        [Test]
        public void AllSpawned_모두스폰되면_true()
        {
            var schedule = ThreeAtOneSecondApart();
            Assert.IsFalse(schedule.AllSpawned);
            schedule.Collect(10f);
            Assert.IsTrue(schedule.AllSpawned);
        }
    }
}
