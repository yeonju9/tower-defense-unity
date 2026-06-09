using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    /// <summary>스킬 3종(운석/시간정지/골드러시)의 GameLoop 연동 검증.</summary>
    public class SkillTests
    {
        private const float Dt = 0.1f;
        private const int MaxSteps = 100000;

        private static List<Vec2> Path(float length) =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(length, 0f) };

        private static EnemySpec Bug(int hp = 10, float speed = 0.5f, int reward = 5) =>
            new EnemySpec(hp, speed, reward);

        // --- 운석 낙하 ---

        [Test]
        public void 운석은_지정범위_적을_즉시_처치하고_쿨다운에_들어간다()
        {
            var wave = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, Bug()), new SpawnEntry(0.1f, Bug()), new SpawnEntry(0.2f, Bug()),
            });
            var loop = new GameLoop(new GameLoopConfig(Path(10f), 0, 20, wave));
            loop.StartWave();
            loop.Update(0.1f);
            loop.Update(0.1f); // 적 3마리가 시작점 근처에 뭉침

            int killedBefore = loop.KilledCount;
            int activeBefore = loop.ActiveEnemyCount;
            Assert.GreaterOrEqual(activeBefore, 1);

            bool cast = loop.TryCastMeteor(new Vec2(0.1f, 0f)); // 기본 반경 1.5

            Assert.IsTrue(cast);
            Assert.AreEqual(killedBefore + activeBefore, loop.KilledCount); // 범위 내 적 전멸
            Assert.AreEqual(0, loop.ActiveEnemyCount);
            Assert.IsFalse(loop.MeteorReady);                  // 쿨다운 진입
            Assert.IsFalse(loop.TryCastMeteor(new Vec2(0f, 0f))); // 쿨다운 중 재사용 불가
        }

        [Test]
        public void 운석은_웨이브_시작전에는_쓸수_없다()
        {
            var wave = new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug()) });
            var loop = new GameLoop(new GameLoopConfig(Path(10f), 0, 20, wave));
            Assert.IsFalse(loop.MeteorReady);                  // 아직 Ready 단계
            Assert.IsFalse(loop.TryCastMeteor(new Vec2(0f, 0f)));
        }

        // --- 시간 정지 ---

        [Test]
        public void 시간정지중에는_적이_이동하지_않아_누수가_지연된다()
        {
            // 짧은 경로 + 라이프 1: 정지 없으면 곧 새어나가 패배.
            var wave = new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug(hp: 10, speed: 1.5f)) });
            var loop = new GameLoop(new GameLoopConfig(Path(3f), 0, 1, wave)); // 길이 3 → 약 2초면 누수
            loop.StartWave();
            loop.Update(0.1f); // 적 등장

            Assert.IsTrue(loop.TryCastTimeStop());
            Assert.IsTrue(loop.IsTimeStopped);

            // 정지 지속(기본 3초) 동안: 정지 없었다면 누수됐을 시간만큼 돌려도 누수가 없어야 한다.
            for (int i = 0; i < 20; i++) // 2초
                loop.Update(0.1f);

            Assert.AreEqual(0, loop.LeakedCount);              // 정지 덕에 아직 안 샘
            Assert.AreEqual(Phase.Playing, loop.Phase.Current);

            // 정지가 풀리고 충분히 지나면 결국 새어나가 패배.
            int steps = 0;
            while (!loop.Phase.IsOver && steps < MaxSteps) { loop.Update(Dt); steps++; }
            Assert.AreEqual(Phase.Lost, loop.Phase.Current);
            Assert.AreEqual(1, loop.LeakedCount);
        }

        // --- 골드 러시 ---

        [Test]
        public void 골드러시중_처치는_보상이_배수로_들어온다()
        {
            var wave = new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug(hp: 10, reward: 5)) });
            var loop = new GameLoop(new GameLoopConfig(Path(10f), 0, 20, wave));
            loop.TryBuildTower(new Vec2(0.5f, 0.3f), 0, new TowerUnit(range: 5f, fireInterval: 0.2f, damage: 10));
            loop.StartWave();

            Assert.IsTrue(loop.TryCastGoldRush());
            Assert.IsTrue(loop.IsGoldRushActive);
            Assert.IsFalse(loop.TryCastGoldRush()); // 쿨다운 중 재사용 불가

            int steps = 0;
            while (!loop.Phase.IsOver && steps < MaxSteps) { loop.Update(Dt); steps++; }

            Assert.AreEqual(Phase.Won, loop.Phase.Current);
            Assert.AreEqual(1, loop.KilledCount);
            Assert.AreEqual(10, loop.Economy.Gold);            // 보상 5 × 2배
        }
    }
}
