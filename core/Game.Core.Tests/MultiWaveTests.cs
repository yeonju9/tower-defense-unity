using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class MultiWaveTests
    {
        private const float Dt = 0.2f;
        private const int MaxSteps = 100000;

        private static List<Vec2> ShortPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(6f, 0f) };

        private static EnemySpec Bug() => new EnemySpec(maxHp: 10, speed: 1.5f, goldReward: 3);

        private static WaveSchedule OneBug() =>
            new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug()) });

        // 즉시·확실하게 잡는 강한 화살탑(웨이브 진행 검증이 목적이라 처치를 결정론적으로).
        private static TowerUnit StrongArcher() => new TowerUnit(range: 6f, fireInterval: 0.2f, damage: 10);

        private static void Step(GameLoop loop, Func<GameLoop, bool> until)
        {
            int steps = 0;
            while (!until(loop) && steps < MaxSteps)
            {
                loop.Update(Dt);
                steps++;
            }
            Assert.Less(steps, MaxSteps, "조건이 제한 스텝 안에 충족되지 않았다.");
        }

        // --- Stage 검증 ---

        [Test]
        public void Stage_웨이브가_없으면_예외()
        {
            Assert.Throws<ArgumentException>(() => new Stage(new List<WaveSchedule>()));
        }

        [Test]
        public void Stage_SingleWave_편의생성자()
        {
            var stage = Stage.SingleWave(OneBug());
            Assert.AreEqual(1, stage.WaveCount);
        }

        // --- 멀티 웨이브 진행 ---

        [Test]
        public void 준비단계없으면_웨이브가_자동으로_연달아_진행되고_마지막까지_클리어하면_승리()
        {
            var stage = new Stage(new List<WaveSchedule> { OneBug(), OneBug() });
            var loop = new GameLoop(new GameLoopConfig(ShortPath(), startingGold: 0, startingLives: 10, stage));
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, StrongArcher());

            Assert.AreEqual(2, loop.TotalWaves);
            loop.StartWave();
            Step(loop, l => l.Phase.IsOver);

            Assert.AreEqual(Phase.Won, loop.Phase.Current);
            Assert.AreEqual(2, loop.KilledCount);          // 두 웨이브의 적 모두 처치
            Assert.AreEqual(2, loop.CurrentWaveNumber);    // 마지막 웨이브까지 도달
        }

        [Test]
        public void 첫_웨이브_클리어만으로는_승리하지_않고_다음_웨이브_준비상태가_된다()
        {
            // 준비 단계(prep)를 둬서 웨이브 사이 상태를 결정론적으로 관찰한다.
            var stage = new Stage(new List<WaveSchedule> { OneBug(), OneBug() });
            var config = new GameLoopConfig(ShortPath(), 0, 10, stage, refundRatio: 0.6f,
                wavePrepDuration: 5f, earlyBonusPerSecond: 2);
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, StrongArcher());

            loop.StartWave(); // 1웨이브 시작
            Step(loop, l => l.IsBetweenWaves);

            // 1웨이브는 끝났지만 아직 승리가 아니며, 2웨이브 준비 상태여야 한다.
            Assert.AreEqual(Phase.Playing, loop.Phase.Current);
            Assert.IsTrue(loop.IsBetweenWaves);
            Assert.AreEqual(2, loop.CurrentWaveNumber);

            // 2웨이브를 시작해 끝까지 가면 승리.
            loop.StartWave();
            Step(loop, l => l.Phase.IsOver);
            Assert.AreEqual(Phase.Won, loop.Phase.Current);
        }

        // --- 조기 시작 보너스 (task 15) ---

        [Test]
        public void 준비단계에서_즉시_다음웨이브를_시작하면_최대보너스를_받는다()
        {
            var stage = new Stage(new List<WaveSchedule> { OneBug(), OneBug() });
            var config = new GameLoopConfig(ShortPath(), 0, 10, stage,
                wavePrepDuration: 5f, earlyBonusPerSecond: 2);
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, StrongArcher());

            loop.StartWave();
            Step(loop, l => l.IsBetweenWaves);

            int before = loop.Economy.Gold;
            loop.StartWave();                 // 준비 시간 5초 전부 남은 채 즉시 시작
            int gained = loop.Economy.Gold - before;

            Assert.AreEqual(10, gained);      // 5초 × 2골드
        }

        [Test]
        public void 준비시간을_더_쓰고_시작하면_보너스가_줄어든다()
        {
            var stage = new Stage(new List<WaveSchedule> { OneBug(), OneBug() });
            var config = new GameLoopConfig(ShortPath(), 0, 10, stage,
                wavePrepDuration: 5f, earlyBonusPerSecond: 2);
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, StrongArcher());

            loop.StartWave();
            Step(loop, l => l.IsBetweenWaves);

            loop.Update(1f);                  // 준비 시간 1초 소비
            loop.Update(1f);                  // 또 1초 (남은 3초)
            Assert.AreEqual(3f, loop.PrepRemaining, 1e-4f);

            int before = loop.Economy.Gold;
            loop.StartWave();
            int gained = loop.Economy.Gold - before;

            Assert.AreEqual(6, gained);       // 남은 3초 × 2골드
        }
    }
}
