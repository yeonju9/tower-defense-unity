using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>타워 타게팅 모드(선두/가장가까운/가장강한)가 각각 다른 적을 고르는지 검증.</summary>
    public class TargetingModeTests
    {
        // 타워(0,0) 기준:
        //  - far: 멀지만 가장 전진(선두), 체력 낮음
        //  - near: 가깝지만 덜 전진, 체력 높음
        private static (TargetSelector selector, List<TargetSelector.Candidate> candidates,
            EnemyUnit far, EnemyUnit near) Setup()
        {
            var farUnit = new EnemyUnit(10, 1f, 1);
            var nearUnit = new EnemyUnit(100, 1f, 1);
            var candidates = new List<TargetSelector.Candidate>
            {
                new TargetSelector.Candidate(farUnit, new Vec2(5f, 0f), progress: 0.9f),
                new TargetSelector.Candidate(nearUnit, new Vec2(1f, 0f), progress: 0.1f),
            };
            return (new TargetSelector(), candidates, farUnit, nearUnit);
        }

        [Test]
        public void First는_가장_전진한_적을_고른다()
        {
            var (sel, cands, far, _) = Setup();
            var picked = sel.Select(new Vec2(0f, 0f), range: 10f, cands, TargetingMode.First);
            Assert.AreSame(far, picked);
        }

        [Test]
        public void Nearest는_가장_가까운_적을_고른다()
        {
            var (sel, cands, _, near) = Setup();
            var picked = sel.Select(new Vec2(0f, 0f), range: 10f, cands, TargetingMode.Nearest);
            Assert.AreSame(near, picked);
        }

        [Test]
        public void Strongest는_체력이_가장_높은_적을_고른다()
        {
            var (sel, cands, _, near) = Setup();
            var picked = sel.Select(new Vec2(0f, 0f), range: 10f, cands, TargetingMode.Strongest);
            Assert.AreSame(near, picked); // near가 hp 100으로 더 높음
        }

        [Test]
        public void 기본_모드는_First다()
        {
            var (sel, cands, far, _) = Setup();
            var withoutMode = sel.Select(new Vec2(0f, 0f), range: 10f, cands);
            Assert.AreSame(far, withoutMode); // 모드 생략 시 선두
        }

        [Test]
        public void TowerUnit의_기본_타게팅은_First다()
        {
            var tower = new TowerUnit(range: 3f, fireInterval: 1f, damage: 10);
            Assert.AreEqual(TargetingMode.First, tower.Targeting);
            tower.SetTargeting(TargetingMode.Strongest);
            Assert.AreEqual(TargetingMode.Strongest, tower.Targeting);
        }

        [Test]
        public void GameLoop에서_Strongest로_바꾸면_튼튼한_적부터_때린다()
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(100f, 0f) };
            // 같은 시각에 약한 적 → 강한 적 순으로 스폰(선두는 약한 적이 됨).
            var schedule = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, new EnemySpec(10, 0.01f, 1)),   // 약하지만 선두
                new SpawnEntry(0f, new EnemySpec(100, 0.01f, 1)),  // 튼튼함
            });
            var config = new GameLoopConfig(waypoints, startingGold: 0, startingLives: 10, schedule);
            var loop = new GameLoop(config);
            loop.TryBuildTower(new Vec2(0f, 0f), cost: 0,
                new TowerUnit(range: 50f, fireInterval: 10f, damage: 20));
            loop.SetTowerTargetingAt(new Vec2(0f, 0f), TargetingMode.Strongest);

            loop.StartWave();
            loop.Update(0.01f); // 한 발만 발사(발사간격 10초)

            // 튼튼한 적(hp100)이 데미지를 받아야 한다(100-20=80). 약한 적은 무사(10).
            EnemyView strong = default;
            EnemyView weak = default;
            foreach (var e in loop.SnapshotEnemies())
            {
                if (e.MaxHp == 100) strong = e;
                else weak = e;
            }
            Assert.AreEqual(80, strong.Hp);
            Assert.AreEqual(10, weak.Hp);
        }
    }
}
