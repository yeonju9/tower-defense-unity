using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// '둘째 상성' 검증: 빠른 적(돌격병)에 대해 빙결탑(둔화)을 곁들이면, 둔화로 시간을 벌어
    /// 같은 화살탑이 더 많은 적을 처치한다(둔화탑은 단독으론 약하지만 조합에서 빛난다).
    /// </summary>
    public class SlowMatchupTests
    {
        private const float Dt = 0.1f;
        private const int MaxSteps = 100000;

        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(12f, 0f) };

        // 돌격병: 빠르고(3.0) 체력 낮음(20).
        private static EnemySpec Rusher() => new EnemySpec(maxHp: 20, speed: 3.0f, goldReward: 5);

        private static WaveSchedule RusherWave()
        {
            var entries = new List<SpawnEntry>();
            for (int i = 0; i < 5; i++)
                entries.Add(new SpawnEntry(i * 0.3f, Rusher()));
            return new WaveSchedule(entries);
        }

        private static GameLoop RunWith(System.Action<GameLoop> placeTowers)
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 20, RusherWave());
            var loop = new GameLoop(config);
            placeTowers(loop);
            loop.StartWave();

            int steps = 0;
            while (!loop.Phase.IsOver && steps < MaxSteps)
            {
                loop.Update(Dt);
                steps++;
            }
            Assert.IsTrue(loop.Phase.IsOver);
            return loop;
        }

        [Test]
        public void 빙결탑을_곁들이면_화살탑이_빠른적을_더_잡고_덜_흘린다()
        {
            // 화살탑만.
            var archerOnly = RunWith(loop =>
            {
                loop.TryBuildTower(new Vec2(1f, 0.6f), 0, new TowerUnit(range: 3f, fireInterval: 0.5f, damage: 10));
            });

            // 화살탑 + 빙결탑(데미지는 거의 없지만 강하게 둔화).
            var archerPlusIce = RunWith(loop =>
            {
                loop.TryBuildTower(new Vec2(1f, 0.6f), 0, new TowerUnit(range: 3f, fireInterval: 0.5f, damage: 10));
                loop.TryBuildTower(new Vec2(0.5f, 0.6f), 0,
                    new TowerUnit(range: 3f, fireInterval: 0.4f, damage: 1,
                        effect: TowerEffect.Slow(factor: 0.35f, duration: 2f)));
            });

            Assert.Greater(archerPlusIce.KilledCount, archerOnly.KilledCount,
                "둔화를 곁들이면 화살탑이 더 많은 빠른 적을 잡아야 한다(둘째 상성).");
            Assert.Less(archerPlusIce.LeakedCount, archerOnly.LeakedCount,
                "둔화를 곁들이면 빠른 적을 덜 흘려야 한다.");
        }
    }
}
