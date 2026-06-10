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

        /// <summary>스테이지 4 — 갈림길 없는 곡선, 중장갑병 등장(고데미지의 필요성). 2웨이브.</summary>
        public static StageDefinition Stage4()
        {
            // 곡선을 점 여러 개의 완만한 꺾임으로 근사.
            var waypoints = new List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(8f, 2f), new Vec2(14f, 8f), new Vec2(18f, 16f),
            };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
                new SpawnEntry(1f, EnemyCatalog.Infantry),
                new SpawnEntry(2f, EnemyCatalog.SwarmBug),
                new SpawnEntry(2f, EnemyCatalog.SwarmBug),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(2f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(4f, EnemyCatalog.HeavyArmor),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2 });
            return new StageDefinition("4. 곡선 길", waypoints, stage,
                startingGold: 200, startingLives: 18, wavePrepDuration: 12f, earlyBonusPerSecond: 2);
        }

        /// <summary>스테이지 5 — 2갈래 합류, 질주 기병 등장(화력 분산 vs 집중). 3웨이브.</summary>
        public static StageDefinition Stage5()
        {
            var waypoints = new List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(10f, 5f), new Vec2(20f, 5f), new Vec2(28f, 12f),
            };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
                new SpawnEntry(1f, EnemyCatalog.Charger),
                new SpawnEntry(2f, EnemyCatalog.Charger),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.5f, EnemyCatalog.SwarmBug),
                new SpawnEntry(1.5f, EnemyCatalog.HeavyArmor),
            });
            // 질주 기병: 단일 카운터로 안 막혀 둔화+집중을 강제.
            var wave3 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Cavalry),
                new SpawnEntry(1.2f, EnemyCatalog.Cavalry),
                new SpawnEntry(2.4f, EnemyCatalog.Cavalry),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2, wave3 });
            return new StageDefinition("5. 갈래 합류", waypoints, stage,
                startingGold: 225, startingLives: 18, wavePrepDuration: 12f, earlyBonusPerSecond: 3);
        }

        /// <summary>스테이지 6 — 긴 우회 경로, 독탑 활용(지속 피해). 튼튼한 적 다수. 2웨이브.</summary>
        public static StageDefinition Stage6()
        {
            // 긴 우회 = 지점이 많고 총 길이가 길다(독이 누적될 시간을 준다).
            var waypoints = new List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(12f, 0f), new Vec2(12f, 10f),
                new Vec2(2f, 10f), new Vec2(2f, 20f), new Vec2(16f, 20f),
            };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(2f, EnemyCatalog.HeavyArmor),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(2f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(4f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(6f, EnemyCatalog.Infantry),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2 });
            return new StageDefinition("6. 우회로", waypoints, stage,
                startingGold: 250, startingLives: 16, wavePrepDuration: 12f, earlyBonusPerSecond: 3);
        }

        /// <summary>스테이지 7 — S자 좁은 길, 번개탑(체인 위치 선정). 일렬 무리 적. 2웨이브.</summary>
        public static StageDefinition Stage7()
        {
            var waypoints = new List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(10f, 0f), new Vec2(10f, 6f),
                new Vec2(0f, 6f), new Vec2(0f, 12f), new Vec2(10f, 12f),
            };
            // 군집 벌레가 일렬로 길게 늘어서게 → 체인의 가치 극대화.
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.3f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.6f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.9f, EnemyCatalog.SwarmBug),
                new SpawnEntry(1.2f, EnemyCatalog.SwarmBug),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.3f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.6f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.9f, EnemyCatalog.Charger),
                new SpawnEntry(1.5f, EnemyCatalog.Charger),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2 });
            return new StageDefinition("7. S자 길", waypoints, stage,
                startingGold: 275, startingLives: 16, wavePrepDuration: 12f, earlyBonusPerSecond: 3);
        }

        /// <summary>스테이지 8 — 넓은 개활지, 골드탑(경제 전략). 여유 구간 + 압박 혼합. 3웨이브.</summary>
        public static StageDefinition Stage8()
        {
            var waypoints = new List<Vec2> { new Vec2(0f, 0f), new Vec2(25f, 0f), new Vec2(25f, 8f) };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Infantry),
                new SpawnEntry(2f, EnemyCatalog.Infantry),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(1f, EnemyCatalog.Charger),
                new SpawnEntry(2f, EnemyCatalog.HeavyArmor),
            });
            var wave3 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Cavalry),
                new SpawnEntry(1.5f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(3f, EnemyCatalog.Cavalry),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2, wave3 });
            return new StageDefinition("8. 개활지", waypoints, stage,
                startingGold: 300, startingLives: 16, wavePrepDuration: 14f, earlyBonusPerSecond: 4);
        }

        /// <summary>스테이지 9 — 복합 2경로, 조합 웨이브(배운 것 종합). 3웨이브.</summary>
        public static StageDefinition Stage9()
        {
            var waypoints = new List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(8f, 6f), new Vec2(16f, 0f), new Vec2(24f, 6f), new Vec2(30f, 14f),
            };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0f, EnemyCatalog.SwarmBug),
                new SpawnEntry(0.5f, EnemyCatalog.Charger),
                new SpawnEntry(1f, EnemyCatalog.HeavyArmor),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Cavalry),
                new SpawnEntry(1f, EnemyCatalog.Cavalry),
                new SpawnEntry(2f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(2f, EnemyCatalog.SwarmBug),
                new SpawnEntry(2.3f, EnemyCatalog.SwarmBug),
            });
            var wave3 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(1f, EnemyCatalog.Cavalry),
                new SpawnEntry(2f, EnemyCatalog.Cavalry),
                new SpawnEntry(3f, EnemyCatalog.HeavyArmor),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2, wave3 });
            return new StageDefinition("9. 복합 길", waypoints, stage,
                startingGold: 325, startingLives: 14, wavePrepDuration: 14f, earlyBonusPerSecond: 4);
        }

        /// <summary>스테이지 10 — 최종 맵, 강화 미니 보스 다수(모든 시스템 총동원). 3웨이브.</summary>
        public static StageDefinition Stage10()
        {
            var waypoints = new List<Vec2>
            {
                new Vec2(0f, 0f), new Vec2(10f, 4f), new Vec2(6f, 12f),
                new Vec2(16f, 16f), new Vec2(26f, 10f), new Vec2(32f, 20f),
            };
            var wave1 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.Cavalry),
                new SpawnEntry(0.8f, EnemyCatalog.Cavalry),
                new SpawnEntry(1.6f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(2f, EnemyCatalog.SwarmBug),
                new SpawnEntry(2.3f, EnemyCatalog.SwarmBug),
            });
            var wave2 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(1f, EnemyCatalog.HeavyArmor),
                new SpawnEntry(2f, EnemyCatalog.Cavalry),
                new SpawnEntry(3f, EnemyCatalog.MiniBoss),
            });
            // 최종 웨이브: 미니 보스 다수 + 호위.
            var wave3 = new WaveSchedule(new List<SpawnEntry>
            {
                new SpawnEntry(0f, EnemyCatalog.MiniBoss),
                new SpawnEntry(0f, EnemyCatalog.Cavalry),
                new SpawnEntry(2f, EnemyCatalog.MiniBoss),
                new SpawnEntry(4f, EnemyCatalog.MiniBoss),
            });
            var stage = new Stage(new List<WaveSchedule> { wave1, wave2, wave3 });
            return new StageDefinition("10. 최종 결전", waypoints, stage,
                startingGold: 400, startingLives: 12, wavePrepDuration: 15f, earlyBonusPerSecond: 5);
        }

        /// <summary>구현된 스테이지 10종을 순서대로 반환(스테이지 선택 화면용).</summary>
        public static IReadOnlyList<StageDefinition> All() =>
            new List<StageDefinition>
            {
                Stage1(), Stage2(), Stage3(), Stage4(), Stage5(),
                Stage6(), Stage7(), Stage8(), Stage9(), Stage10(),
            };
    }
}
