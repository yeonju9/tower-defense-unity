using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>번개탑(체인 타워)이 GameLoop 안에서 한 발로 뭉친 무리 적 여럿을 타격하는지 검증.</summary>
    public class ChainTowerTests
    {
        private static GameLoop BuildLoop(int enemyHp, int enemyCount, TowerUnit tower)
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };
            var spawns = new List<SpawnEntry>();
            for (int i = 0; i < enemyCount; i++)
                spawns.Add(new SpawnEntry(0f, new EnemySpec(enemyHp, 0.1f, 1))); // 같은 시각 → 뭉쳐 등장
            var schedule = new WaveSchedule(spawns);
            var config = new GameLoopConfig(waypoints, startingGold: 0, startingLives: 10, schedule);
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0f, 0f), cost: 0, tower);
            return loop;
        }

        [Test]
        public void 번개탑은_한_발로_뭉친_적_여럿을_감쇠_데미지로_타격한다()
        {
            // damage 20, 점프마다 0.5배 → 20,10,5. 죽지 않게 hp 100.
            var lightning = new TowerUnit(range: 5f, fireInterval: 1f, damage: 20,
                chain: new ChainSpec(maxTargets: 3, jumpRange: 1f, falloff: 0.5f));
            var loop = BuildLoop(enemyHp: 100, enemyCount: 3, lightning);

            loop.StartWave();
            loop.Update(0.1f); // 첫 스텝에 발사

            var snap = loop.SnapshotEnemies();
            Assert.AreEqual(3, snap.Count);
            int totalHp = 0;
            foreach (var e in snap)
                totalHp += e.Hp;
            // 300 - (20+10+5) = 265
            Assert.AreEqual(265, totalHp);
        }

        [Test]
        public void 단일타워는_같은_상황에서_한_마리만_타격한다()
        {
            var single = new TowerUnit(range: 5f, fireInterval: 1f, damage: 20);
            var loop = BuildLoop(enemyHp: 100, enemyCount: 3, single);

            loop.StartWave();
            loop.Update(0.1f);

            var snap = loop.SnapshotEnemies();
            int totalHp = 0;
            foreach (var e in snap)
                totalHp += e.Hp;
            Assert.AreEqual(280, totalHp); // 300 - 20
        }
    }
}
