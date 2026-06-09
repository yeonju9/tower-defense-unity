using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    /// <summary>화면 렌더링용 읽기 전용 스냅샷(Task 0) 검증.</summary>
    public class ViewSnapshotTests
    {
        private static List<Vec2> StraightPath() =>
            new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f) };

        private static EnemySpec Bug(int hp = 30) => new EnemySpec(hp, speed: 1.5f, goldReward: 5);

        private static GameLoop LoopWith(WaveSchedule wave, int gold = 100, int lives = 10) =>
            new GameLoop(new GameLoopConfig(StraightPath(), gold, lives, wave));

        // --- 적 스냅샷 ---

        [Test]
        public void SnapshotEnemies_스폰된적의_위치와_체력을_담는다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug(hp: 30)) }));
            loop.StartWave();
            loop.Update(0.1f); // 적 1마리 스폰 + 이동

            var snaps = loop.SnapshotEnemies();
            Assert.AreEqual(1, snaps.Count);
            Assert.AreEqual(30, snaps[0].MaxHp);
            Assert.AreEqual(30, snaps[0].Hp);
            Assert.Greater(snaps[0].Position.X, 0f); // 전진해서 시작점보다 앞
        }

        [Test]
        public void SnapshotEnemies_같은적은_프레임이_지나도_같은_Id를_유지한다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug()) }));
            loop.StartWave();
            loop.Update(0.1f);
            int idFirst = loop.SnapshotEnemies()[0].Id;
            loop.Update(0.1f);
            int idLater = loop.SnapshotEnemies()[0].Id;
            Assert.AreEqual(idFirst, idLater);
        }

        [Test]
        public void SnapshotEnemies_서로_다른적은_다른_Id를_갖는다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, Bug()), new SpawnEntry(0.05f, Bug()),
            }));
            loop.StartWave();
            loop.Update(0.1f); // 둘 다 스폰

            var snaps = loop.SnapshotEnemies();
            Assert.AreEqual(2, snaps.Count);
            Assert.AreNotEqual(snaps[0].Id, snaps[1].Id);
        }

        [Test]
        public void SnapshotEnemies_죽은적은_사라진다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug(hp: 10)) }));
            loop.TryBuildTower(new Vec2(0.5f, 0.3f), 0, new TowerUnit(range: 5f, fireInterval: 0.2f, damage: 100));
            loop.StartWave();
            loop.Update(0.1f); // 타워가 적 처치

            Assert.AreEqual(0, loop.SnapshotEnemies().Count);
        }

        [Test]
        public void SnapshotEnemies_둔화상태가_플래그로_보인다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry> { new SpawnEntry(0f, Bug()) }));
            loop.TryBuildTower(new Vec2(0.5f, 0.3f), 0,
                new TowerUnit(range: 5f, fireInterval: 0.2f, damage: 1,
                    effect: TowerEffect.Slow(0.5f, 3f)));
            loop.StartWave();
            loop.Update(0.1f); // 빙결탑이 적을 둔화

            Assert.IsTrue(loop.SnapshotEnemies()[0].IsSlowed);
        }

        // --- 타워 스냅샷 ---

        [Test]
        public void SnapshotTowers_배치된타워의_위치와_레벨을_담는다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry>()));
            var pos = new Vec2(1f, 1f);
            loop.TryBuildTower(pos, 40, new TowerUnit(range: 2.5f, fireInterval: 0.8f, damage: 10));

            var snaps = loop.SnapshotTowers();
            Assert.AreEqual(1, snaps.Count);
            Assert.AreEqual(1, snaps[0].Level);
            Assert.IsFalse(snaps[0].IsSplash);
            Assert.AreEqual(1f, snaps[0].Position.X, 1e-4f);
        }

        [Test]
        public void SnapshotTowers_업그레이드_후_레벨과_사거리가_갱신된다()
        {
            var loop = LoopWith(new WaveSchedule(new List<SpawnEntry>()));
            var pos = new Vec2(1f, 1f);
            loop.TryBuildTower(pos, 40, new TowerUnit(range: 2.5f, fireInterval: 0.8f, damage: 10));
            loop.TryUpgradeTower(pos, 30, addedDamage: 5, addedRange: 0.5f);

            var snap = loop.SnapshotTowers()[0];
            Assert.AreEqual(2, snap.Level);
            Assert.AreEqual(3.0f, snap.Range, 1e-4f);
        }
    }
}
