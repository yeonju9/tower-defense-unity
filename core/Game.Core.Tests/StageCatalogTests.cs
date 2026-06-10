using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>멀티 스테이지 데이터(1~3)가 vision 난이도 곡선대로 구성됐고 실제로 플레이 가능한지 검증.</summary>
    public class StageCatalogTests
    {
        [Test]
        public void 스테이지1은_직선_단일웨이브_튜토리얼이다()
        {
            var s = StageCatalog.Stage1();
            Assert.AreEqual(2, s.Waypoints.Count);   // 직선 = 점 2개
            Assert.AreEqual(1, s.Stage.WaveCount);   // 단일 웨이브
        }

        [Test]
        public void 후반_스테이지일수록_웨이브가_늘어난다()
        {
            Assert.AreEqual(1, StageCatalog.Stage1().Stage.WaveCount);
            Assert.AreEqual(2, StageCatalog.Stage2().Stage.WaveCount);
            Assert.AreEqual(2, StageCatalog.Stage3().Stage.WaveCount);
        }

        [Test]
        public void 경로가_꺾일수록_지점이_늘어난다()
        {
            Assert.AreEqual(2, StageCatalog.Stage1().Waypoints.Count); // 직선
            Assert.AreEqual(3, StageCatalog.Stage2().Waypoints.Count); // 1번 꺾임
            Assert.AreEqual(3, StageCatalog.Stage3().Waypoints.Count); // L자
        }

        [Test]
        public void All은_구현된_스테이지를_순서대로_반환한다()
        {
            var all = StageCatalog.All();
            Assert.AreEqual(3, all.Count);
            Assert.AreEqual("1. 직선 길", all[0].Name);
            Assert.AreEqual("3. L자 길", all[2].Name);
        }

        [Test]
        public void ToConfig로_GameLoop를_만들_수_있다()
        {
            var loop = new GameLoop(StageCatalog.Stage1().ToConfig());
            Assert.AreEqual(100, loop.Economy.Gold);
            Assert.AreEqual(1, loop.CurrentWaveNumber);
            Assert.AreEqual(global::Game.Core.Phase.Ready, loop.Phase.Current);
        }

        [Test]
        public void 잘못된_정의는_예외()
        {
            var stage = Stage.SingleWave(new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
            }));
            var oneWaypoint = new List<Vec2> { new Vec2(0f, 0f) };

            Assert.Throws<System.ArgumentException>(() =>
                new StageDefinition("x", oneWaypoint, stage, 100, 20)); // 지점 1개
            Assert.Throws<System.ArgumentException>(() =>
                new StageDefinition("", new List<Vec2> { new Vec2(0f, 0f), new Vec2(1f, 0f) }, stage, 100, 20)); // 이름 빈값
        }

        [Test]
        public void 스테이지1을_강력한_타워로_끝까지_클리어하면_승리한다()
        {
            var loop = new GameLoop(StageCatalog.Stage1().ToConfig());
            // 경로 시작점에 보병을 한 방에 잡는 타워 배치.
            loop.TryBuildTower(new Vec2(0f, 0f), cost: 0,
                new TowerUnit(range: 30f, fireInterval: 0.2f, damage: 100));

            loop.StartWave();
            for (int i = 0; i < 200 && loop.Phase.Current == global::Game.Core.Phase.Playing; i++)
                loop.Update(0.1f);

            Assert.AreEqual(global::Game.Core.Phase.Won, loop.Phase.Current);
            Assert.AreEqual(0, loop.LeakedCount); // 한 마리도 새지 않음
        }
    }
}
