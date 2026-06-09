using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Core 조각이 한 판으로 맞물려 도는지 검증하는 통합 시뮬레이션 테스트.
    /// Unity 없이 결정론적으로 '승리/패배/정산'을 확인한다.
    /// </summary>
    public class GameLoopTests
    {
        private const float Dt = 0.1f;
        private const int MaxSteps = 100000; // 무한 루프 방지용 안전장치

        // 길이 10의 직선 경로.
        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };

        private static EnemySpec Infantry(int hp = 30) => new EnemySpec(hp, speed: 1.5f, goldReward: 8);

        private static WaveSchedule OneEnemyAtStart(int hp = 30) =>
            new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Infantry(hp)) });

        private static TowerUnit Archer(float range = 3f, float interval = 0.5f, int damage = 10) =>
            new TowerUnit(range, interval, damage);

        // 판이 끝날 때까지 시뮬레이션을 돌린다.
        private static void RunUntilOver(GameLoop loop)
        {
            int steps = 0;
            while (!loop.Phase.IsOver && steps < MaxSteps)
            {
                loop.Update(Dt);
                steps++;
            }
            Assert.IsTrue(loop.Phase.IsOver, "판이 제한 스텝 안에 끝나지 않았다(무한 루프 의심).");
        }

        [Test]
        public void 타워가_적을_처치하면_승리하고_보상이_지급된다()
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 10, OneEnemyAtStart(hp: 20));
            var loop = new GameLoop(config);
            // 경로 시작점 근처에 타워 → 적이 한동안 사거리 안에 머문다.
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, Archer());
            loop.StartWave();

            RunUntilOver(loop);

            Assert.AreEqual(Phase.Won, loop.Phase.Current);
            Assert.AreEqual(1, loop.KilledCount);
            Assert.AreEqual(0, loop.LeakedCount);
            Assert.AreEqual(10, loop.Life.Lives);        // 라이프 손실 없음
            Assert.AreEqual(8, loop.Economy.Gold);       // 처치 보상 8골드
        }

        [Test]
        public void 타워가_없으면_적이_새어나가_라이프가_소진되고_패배한다()
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 1, OneEnemyAtStart());
            var loop = new GameLoop(config);
            loop.StartWave();

            RunUntilOver(loop);

            Assert.AreEqual(Phase.Lost, loop.Phase.Current);
            Assert.AreEqual(0, loop.KilledCount);
            Assert.AreEqual(1, loop.LeakedCount);
            Assert.AreEqual(0, loop.Life.Lives);
        }

        [Test]
        public void 처치와_누수가_섞여도_정산이_일관된다()
        {
            // 보병 5마리, 0.6초 간격. 약한 타워라 일부만 잡고 일부는 샌다.
            var entries = new List<SpawnEntry>();
            for (int i = 0; i < 5; i++)
                entries.Add(new SpawnEntry(i * 0.6f, Infantry(hp: 30)));
            var schedule = new WaveSchedule(entries);

            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 10, schedule);
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 0, Archer(range: 2.5f, interval: 0.8f, damage: 10));
            loop.StartWave();

            RunUntilOver(loop);

            // 5마리는 전부 '처치 또는 누수'로 정산되어야 한다.
            Assert.AreEqual(5, loop.KilledCount + loop.LeakedCount, "모든 적은 처치되거나 새어나가야 한다.");
            // 골드 = 처치 수 × 보상(8), 라이프 = 시작(10) − 누수 수.
            Assert.AreEqual(loop.KilledCount * 8, loop.Economy.Gold);
            Assert.AreEqual(10 - loop.LeakedCount, loop.Life.Lives);
            Assert.AreEqual(0, loop.ActiveEnemyCount, "판이 끝나면 필드에 적이 없어야 한다.");
        }

        [Test]
        public void 골드가_부족하면_타워를_지을_수_없다()
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 30, startingLives: 10, OneEnemyAtStart());
            var loop = new GameLoop(config);

            bool built = loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 40, Archer());

            Assert.IsFalse(built);
            Assert.AreEqual(0, loop.TowerCount);
            Assert.AreEqual(30, loop.Economy.Gold); // 실패한 구매는 잔액 불변
        }

        [Test]
        public void 타워를_사면_골드가_차감되고_배치된다()
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 100, startingLives: 10, OneEnemyAtStart());
            var loop = new GameLoop(config);

            bool built = loop.TryBuildTower(new Vec2(0.5f, 0.5f), cost: 40, Archer());

            Assert.IsTrue(built);
            Assert.AreEqual(1, loop.TowerCount);
            Assert.AreEqual(60, loop.Economy.Gold);
        }

        [Test]
        public void 웨이브_시작전에는_Update가_아무것도_하지_않는다()
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 10, OneEnemyAtStart());
            var loop = new GameLoop(config);

            loop.Update(5f); // StartWave 호출 안 함

            Assert.AreEqual(Phase.Ready, loop.Phase.Current);
            Assert.AreEqual(0, loop.ActiveEnemyCount);
            Assert.AreEqual(10, loop.Life.Lives);
        }
    }
}
