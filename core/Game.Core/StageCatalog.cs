using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 한 스테이지를 플레이하는 데 필요한 모든 데이터(경로·웨이브·시작 자원·준비 옵션).
    /// ToConfig()로 GameLoopConfig를 만들어 바로 GameLoop에 넣을 수 있다.
    /// </summary>
    public class StageDefinition
    {
        public string Name { get; }
        public IReadOnlyList<Vec2> Waypoints { get; }
        public Stage Stage { get; }
        public int StartingGold { get; }
        public int StartingLives { get; }
        public float WavePrepDuration { get; }
        public int EarlyBonusPerSecond { get; }

        public StageDefinition(string name, IReadOnlyList<Vec2> waypoints, Stage stage,
            int startingGold, int startingLives, float wavePrepDuration = 0f, int earlyBonusPerSecond = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("스테이지 이름은 비어 있을 수 없습니다.", nameof(name));
            if (waypoints == null)
                throw new ArgumentNullException(nameof(waypoints));
            if (waypoints.Count < 2)
                throw new ArgumentException("경로는 최소 2개의 지점이 필요합니다.", nameof(waypoints));
            Name = name;
            Waypoints = new List<Vec2>(waypoints);
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            StartingGold = startingGold;
            StartingLives = startingLives;
            WavePrepDuration = wavePrepDuration;
            EarlyBonusPerSecond = earlyBonusPerSecond;
        }

        /// <summary>이 정의를 GameLoop에 넣을 수 있는 설정으로 변환한다.</summary>
        public GameLoopConfig ToConfig(SkillSettings skills = null) =>
            new GameLoopConfig(Waypoints, StartingGold, StartingLives, Stage,
                refundRatio: 0.6f, wavePrepDuration: WavePrepDuration,
                earlyBonusPerSecond: EarlyBonusPerSecond, skills: skills);
    }

    /// <summary>
    /// vision.md '10개 스테이지 난이도 곡선'을 데이터로 옮긴 카탈로그(현재 1~3 구현).
    /// 난이도는 맵 경로 형태 / 웨이브 구성 / 신규 요소 도입 세 레버로만 조절한다.
    /// </summary>
    public static class StageCatalog
    {
        /// <summary>스테이지 1 — 직선 1경로, 보병만(튜토리얼).</summary>
        public static StageDefinition Stage1()
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(20f, 0f) };
            var wave = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
                new SpawnEntry(1.5f, EnemyCatalog.Infantry),
                new SpawnEntry(3f, EnemyCatalog.Infantry),
                new SpawnEntry(4.5f, EnemyCatalog.Infantry),
                new SpawnEntry(6f, EnemyCatalog.Infantry),
            });
            return new StageDefinition("1. 직선 길", waypoints, Stage.SingleWave(wave),
                startingGold: 100, startingLives: 20);
        }

        /// <summary>스테이지 2 — 1번 꺾인 경로, 군집 벌레 등장(광역의 필요성). 2웨이브.</summary>
        public static StageDefinition Stage2()
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(10f, 0f), new Vec2(10f, 10f) };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
                new SpawnEntry(1f, EnemyCatalog.Infantry),
                new SpawnEntry(2f, EnemyCatalog.Infantry),
            });
            // 2웨이브: 군집 벌레 떼(같은 시각에 뭉쳐 등장 → 광역 카운터 체감).
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.5f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.5f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.5f, EnemyCatalog.SwarmBug),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2 });
            return new StageDefinition("2. 꺾인 길", waypoints, stage,
                startingGold: 150, startingLives: 20, wavePrepDuration: 10f, earlyBonusPerSecond: 2);
        }

        /// <summary>스테이지 3 — L자 경로, 돌격병 등장(둔화+길목). 2웨이브.</summary>
        public static StageDefinition Stage3()
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(15f, 0f), new Vec2(15f, 15f) };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
                new SpawnEntry(1f, EnemyCatalog.Infantry),
                new SpawnEntry(2f, EnemyCatalog.SwarmBug),
                new SpawnEntry(2f, EnemyCatalog.SwarmBug),
            });
            // 2웨이브: 빠른 돌격병 다수 → 둔화 없이는 막기 어렵게.
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Charger),
                new SpawnEntry(0.8f, EnemyCatalog.Charger),
                new SpawnEntry(1.6f, EnemyCatalog.Charger),
                new SpawnEntry(2.4f, EnemyCatalog.Charger),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2 });
            return new StageDefinition("3. L자 길", waypoints, stage,
                startingGold: 175, startingLives: 18, wavePrepDuration: 10f, earlyBonusPerSecond: 2);
        }

        /// <summary>구현된 스테이지를 순서대로 반환(스테이지 선택 화면용).</summary>
        public static IReadOnlyList<StageDefinition> All() =>
            new List<StageDefinition> { Stage1(), Stage2(), Stage3() };
    }
}
