using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>
    /// 카탈로그 정합성 E2E: 실제 StageCatalog × TowerCatalog 데이터로 한 판을 끝까지 굴려
    /// "구조적으로 클리어 가능한가 / 적이 실제 위협인가 / 카탈로그 타워의 상성이 성립하는가"를 못 박는다.
    /// 기존 매치업 테스트가 '임의 수치로 상성 원리'를 증명한다면, 이쪽은 '배포될 실제 데이터'를 검증한다.
    /// </summary>
    public class StageBalanceE2ETests
    {
        private const float Dt = 0.1f;
        private const int MaxSteps = 200000;

        private readonly struct Placement
        {
            public readonly Vec2 Pos;
            public readonly TowerUnit Tower;
            public readonly int Cost;
            public Placement(Vec2 pos, TowerUnit tower, int cost)
            {
                Pos = pos; Tower = tower; Cost = cost;
            }
        }

        /// <summary>스테이지를 타워 배치와 함께 끝까지 시뮬레이션한다(멀티 웨이브는 준비 타이머로 자동 진행).</summary>
        private static GameLoop Play(StageDefinition def, IEnumerable<Placement> placements)
        {
            var loop = new GameLoop(def.ToConfig());
            foreach (var p in placements)
                loop.TryBuildTower(p.Pos, p.Cost, p.Tower);
            loop.StartWave();

            int steps = 0;
            while (!loop.Phase.IsOver && steps < MaxSteps)
            {
                loop.Update(Dt);
                steps++;
            }
            Assert.IsTrue(loop.Phase.IsOver, $"[{def.Name}] 판이 끝나지 않았다(경로/웨이브 구조 의심).");
            return loop;
        }

        /// <summary>사거리·화력이 압도적이라 닿는 모든 적을 즉사시키는 테스트 전용 타워.</summary>
        private static Placement Overwhelming(Vec2 at) =>
            new Placement(at, new TowerUnit(range: 500f, fireInterval: 0.1f, damage: 5000, splashRadius: 500f), 0);

        [Test]
        public void 모든_스테이지는_압도적_화력이면_누수0으로_클리어된다()
        {
            // 데이터 회귀 테스트: 경로가 끊겨 있거나 끝나지 않는 웨이브가 있으면 여기서 깨진다.
            foreach (var def in StageCatalog.All())
            {
                var loop = Play(def, new[] { Overwhelming(def.Waypoints[0]) });
                Assert.AreEqual(global::Game.Core.Phase.Won, loop.Phase.Current, $"[{def.Name}] 승리 실패");
                Assert.AreEqual(0, loop.LeakedCount, $"[{def.Name}] 누수 발생");
            }
        }

        [Test]
        public void 모든_스테이지는_타워가_없으면_적이_새어_라이프가_깎인다()
        {
            // sanity: 적이 실제로 경로를 완주해 위협이 된다(스폰·이동·누수 파이프라인 점검).
            foreach (var def in StageCatalog.All())
            {
                var loop = Play(def, new Placement[0]);
                Assert.Greater(loop.LeakedCount, 0, $"[{def.Name}] 적이 새지 않았다");
                Assert.Less(loop.Life.Lives, def.StartingLives, $"[{def.Name}] 라이프가 안 깎였다");
            }
        }

        [Test]
        public void 스테이지1은_기본_화살탑만으로_클리어_가능하다()
        {
            // 튜토리얼 보장: 첫 스테이지는 가장 싼 기본기(화살탑)로 시작 골드 안에서 클리어돼야 한다.
            var def = StageCatalog.Stage1();
            var arrow = TowerCatalog.Arrow;
            var loop = Play(def, new[]
            {
                new Placement(new Vec2(5f, 0f), arrow.CreateUnit(), arrow.Cost),
                new Placement(new Vec2(13f, 0f), arrow.CreateUnit(), arrow.Cost),
            });

            Assert.AreEqual(global::Game.Core.Phase.Won, loop.Phase.Current);
        }

        [Test]
        public void 무리_스테이지에서_번개탑이_화살탑보다_덜_흘린다()
        {
            // 카탈로그 실제 타워의 상성: 군집 벌레가 줄지어 오는 스테이지7에서
            // 같은 위치·같은 수라면 체인(번개탑)이 단일(화살탑)보다 우월해야 한다.
            var def = StageCatalog.Stage7();
            var at = new Vec2(10f, 3f); // S자 세로 구간을 사거리에 두는 지점

            var lightningRun = Play(def, new[]
            {
                new Placement(at, TowerCatalog.Lightning.CreateUnit(), 0),
            });
            var archerRun = Play(def, new[]
            {
                new Placement(at, TowerCatalog.Arrow.CreateUnit(), 0),
            });

            Assert.LessOrEqual(lightningRun.LeakedCount, archerRun.LeakedCount,
                "번개탑이 화살탑보다 무리 적을 덜 흘려야 한다");
            Assert.GreaterOrEqual(lightningRun.KilledCount, archerRun.KilledCount,
                "번개탑이 화살탑보다 무리 적을 더(또는 같게) 잡아야 한다");
        }
    }
}
