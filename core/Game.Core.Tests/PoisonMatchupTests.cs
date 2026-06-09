using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// '셋째 상성' 검증: 튼튼한 적(중장갑병)에 대해 독탑(지속 피해)을 곁들이면,
    /// 저격탑 단독보다 두꺼운 체력을 더 잘 깎아낸다(독은 즉발이 아니지만 누적 화력으로 보조).
    /// </summary>
    public class PoisonMatchupTests
    {
        private const float Dt = 0.1f;
        private const int MaxSteps = 100000;

        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(12f, 0f) };

        // 중장갑병: 매우 높은 체력(160), 느림(0.8).
        private static EnemySpec Heavy() => new EnemySpec(maxHp: 160, speed: 0.8f, goldReward: 20);

        private static WaveSchedule HeavyWave()
        {
            var entries = new List<SpawnEntry>();
            for (int i = 0; i < 3; i++)
                entries.Add(new SpawnEntry(i * 1.5f, Heavy()));
            return new WaveSchedule(entries);
        }

        private static GameLoop RunWith(Action<GameLoop> placeTowers)
        {
            var config = new GameLoopConfig(StraightPath(), startingGold: 0, startingLives: 20, HeavyWave());
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

        // 저격탑: 긴 사거리·고데미지·느린 발사(튼튼한 적의 정석 카운터, 단일 타깃).
        private static TowerUnit Sniper() => new TowerUnit(range: 6f, fireInterval: 1.2f, damage: 30);

        // 독탑: 데미지는 낮지만 지속 피해 부여(중첩).
        private static TowerUnit PoisonTower() =>
            new TowerUnit(range: 6f, fireInterval: 1.0f, damage: 2,
                effect: TowerEffect.Poison(dps: 14, duration: 3f));

        [Test]
        public void 독탑을_곁들이면_튼튼한_적을_저격탑_단독보다_더_잡고_덜_흘린다()
        {
            var sniperOnly = RunWith(loop =>
                loop.TryBuildTower(new Vec2(1f, 0.6f), 0, Sniper()));

            var sniperPlusPoison = RunWith(loop =>
            {
                loop.TryBuildTower(new Vec2(1f, 0.6f), 0, Sniper());
                loop.TryBuildTower(new Vec2(0.6f, 0.6f), 0, PoisonTower());
            });

            Assert.Greater(sniperPlusPoison.KilledCount, sniperOnly.KilledCount,
                "독탑을 곁들이면 튼튼한 적을 더 많이 잡아야 한다(셋째 상성).");
            Assert.LessOrEqual(sniperPlusPoison.LeakedCount, sniperOnly.LeakedCount,
                "독탑을 곁들이면 튼튼한 적을 덜(또는 같게) 흘려야 한다.");
        }
    }
}
