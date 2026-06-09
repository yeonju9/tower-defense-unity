using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// '첫 상성' 검증: 무리 적(군집 벌레)에 대해 광역 타워(대포탑)가
    /// 단일 타깃 타워(화살탑)보다 명확히 강하다는 것을 통합 시뮬레이션으로 증명한다.
    /// (vision.md 구현 우선순위 2번 — 게임이 '게임'다워지는 순간)
    /// </summary>
    public class CounterMatchupTests
    {
        private const float Dt = 0.1f;
        private const int MaxSteps = 100000;

        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(12f, 0f) };

        // 군집 벌레: 체력 낮고(10) 떼로 등장.
        private static EnemySpec SwarmBug() => new EnemySpec(maxHp: 10, speed: 1.5f, goldReward: 3);

        // 6마리가 0.1초 간격으로 붙어서 등장 → 서로 가깝게 뭉친 무리.
        private static WaveSchedule BunchedSwarm()
        {
            var entries = new List<SpawnEntry>();
            for (int i = 0; i < 6; i++)
                entries.Add(new SpawnEntry(i * 0.1f, SwarmBug()));
            return new WaveSchedule(entries);
        }

        private static GameLoop RunWith(TowerUnit tower)
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 10, BunchedSwarm());
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, tower);
            loop.StartWave();

            int steps = 0;
            while (!loop.Phase.IsOver && steps < MaxSteps)
            {
                loop.Update(Dt);
                steps++;
            }
            Assert.IsTrue(loop.Phase.IsOver, "판이 끝나지 않았다.");
            return loop;
        }

        [Test]
        public void 광역_대포탑이_무리적을_단일_화살탑보다_더_잡고_덜_흘린다()
        {
            // 두 타워는 사거리·발사간격·데미지가 동일하고 '범위 피해 여부'만 다르다.
            var cannon = new TowerUnit(range: 3f, fireInterval: 1.0f, damage: 12, splashRadius: 1.5f);
            var archer = new TowerUnit(range: 3f, fireInterval: 1.0f, damage: 12);

            var cannonRun = RunWith(cannon);
            var archerRun = RunWith(archer);

            // 핵심 상성: 같은 화력이라도 광역이 무리를 더 많이 처치한다.
            Assert.Greater(cannonRun.KilledCount, archerRun.KilledCount,
                "광역 대포탑이 단일 화살탑보다 무리 적을 더 많이 잡아야 한다(첫 상성).");
            // 그리고 무리를 덜 흘린다.
            Assert.Less(cannonRun.LeakedCount, archerRun.LeakedCount,
                "광역 대포탑이 무리 적을 덜 흘려야 한다.");
        }

        [Test]
        public void 대포탑은_뭉친_군집을_전멸시켜_승리한다()
        {
            var cannon = new TowerUnit(range: 3f, fireInterval: 1.0f, damage: 12, splashRadius: 1.5f);
            var run = RunWith(cannon);

            Assert.AreEqual(Phase.Won, run.Phase.Current);
            Assert.AreEqual(6, run.KilledCount);
            Assert.AreEqual(0, run.LeakedCount);
        }
    }
}
