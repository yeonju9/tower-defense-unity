using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>한 판을 시작하는 데 필요한 설정 묶음.</summary>
    public class GameLoopConfig
    {
        public IReadOnlyList<Vec2> Waypoints { get; }
        public int StartingGold { get; }
        public int StartingLives { get; }
        public Stage Stage { get; }

        /// <summary>타워 판매 시 지불 비용 중 환불되는 비율(0~1). 기본 0.6.</summary>
        public float RefundRatio { get; }

        /// <summary>웨이브 간 준비 시간(초). 0이면 준비 단계·조기 보너스 없음.</summary>
        public float WavePrepDuration { get; }

        /// <summary>조기 웨이브 시작 시 남은 준비시간 1초당 보너스 골드. WavePrepDuration이 0이면 무의미.</summary>
        public int EarlyBonusPerSecond { get; }

        /// <summary>스킬 3종 설정. null이면 기본값(vision 기준).</summary>
        public SkillSettings Skills { get; }

        /// <summary>단일 웨이브 설정(기존 호환). 내부적으로 1개짜리 Stage로 감싼다.</summary>
        public GameLoopConfig(IReadOnlyList<Vec2> waypoints, int startingGold, int startingLives, WaveSchedule schedule,
            float refundRatio = 0.6f)
            : this(waypoints, startingGold, startingLives,
                   Stage.SingleWave(schedule ?? throw new ArgumentNullException(nameof(schedule))), refundRatio)
        {
        }

        /// <summary>멀티 웨이브 설정.</summary>
        public GameLoopConfig(IReadOnlyList<Vec2> waypoints, int startingGold, int startingLives, Stage stage,
            float refundRatio = 0.6f, float wavePrepDuration = 0f, int earlyBonusPerSecond = 0,
            SkillSettings skills = null)
        {
            Waypoints = waypoints ?? throw new ArgumentNullException(nameof(waypoints));
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            if (refundRatio < 0f || refundRatio > 1f)
                throw new ArgumentOutOfRangeException(nameof(refundRatio), "환불 비율은 0~1 범위여야 합니다.");
            if (wavePrepDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(wavePrepDuration), "준비 시간은 음수일 수 없습니다.");
            if (earlyBonusPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(earlyBonusPerSecond), "초당 보너스는 음수일 수 없습니다.");
            StartingGold = startingGold;
            StartingLives = startingLives;
            RefundRatio = refundRatio;
            WavePrepDuration = wavePrepDuration;
            EarlyBonusPerSecond = earlyBonusPerSecond;
            Skills = skills ?? SkillSettings.Default;
        }
    }

    /// <summary>
    /// Core 조각(Economy/LifeSystem/GamePhase/WaveSchedule/PathTracker/TowerUnit/EnemyUnit/TargetSelector)을
    /// 한 판 단위로 묶어 Unity 없이 결정론적으로 시뮬레이션한다.
    ///
    /// 타워 발사는 Core에서는 '즉시 명중'으로 처리한다(총알의 비행 시간은 View 레이어의 연출일 뿐
    /// 게임 규칙이 아니다). 이 단순화 덕에 한 판 전체를 빠르고 재현 가능하게 테스트할 수 있다.
    /// </summary>
    public class GameLoop
    {
        private sealed class ActiveEnemy
        {
            public int Id;
            public EnemyUnit Unit;
            public PathTracker Tracker;
        }

        private sealed class PlacedTower
        {
            public Vec2 Position;
            public TowerUnit Unit;
            public int CostPaid;
        }

        private readonly IReadOnlyList<Vec2> waypoints;
        private readonly IReadOnlyList<WaveSchedule> waves;
        private readonly float refundRatio;
        private readonly float wavePrepDuration;
        private readonly int earlyBonusPerSecond;
        private readonly TargetSelector selector = new TargetSelector();
        private readonly SplashResolver splashResolver = new SplashResolver();
        private readonly List<ActiveEnemy> enemies = new List<ActiveEnemy>();
        private readonly List<PlacedTower> towers = new List<PlacedTower>();

        private int currentWaveIndex;
        private bool waveInProgress;
        private float waveElapsed;
        private int nextEnemyId; // 적 스폰마다 1씩 증가하는 안정적 식별자 발급기
        private WaveStartTimer prepTimer; // 웨이브 간 준비 카운트다운(조기 보너스). 사용 안 하면 null.

        // --- 스킬 3종 ---
        private readonly SkillSettings skills;
        private readonly Cooldown meteorCd;
        private readonly Cooldown timeStopCd;
        private readonly Cooldown goldRushCd;
        private float freezeRemaining;    // 시간 정지 잔여(>0이면 적 정지)
        private float goldRushRemaining;  // 골드 러시 잔여(>0이면 처치 골드 배수)

        public Economy Economy { get; }
        public LifeSystem Life { get; }
        public GamePhase Phase { get; } = new GamePhase();

        public int ActiveEnemyCount => enemies.Count;
        public int TowerCount => towers.Count;
        public int KilledCount { get; private set; }
        public int LeakedCount { get; private set; }

        /// <summary>현재 웨이브 번호(1-based).</summary>
        public int CurrentWaveNumber => currentWaveIndex + 1;
        /// <summary>스테이지의 전체 웨이브 수.</summary>
        public int TotalWaves => waves.Count;
        /// <summary>한 웨이브가 끝났지만 다음 웨이브가 아직 시작되지 않은 대기 상태(이때 빌드·준비 가능).</summary>
        public bool IsBetweenWaves =>
            Phase.Current == global::Game.Core.Phase.Playing && !waveInProgress;
        /// <summary>다음 웨이브 시작까지 남은 준비 시간(준비 단계가 없으면 0).</summary>
        public float PrepRemaining => prepTimer?.Remaining ?? 0f;

        private WaveSchedule CurrentWave => waves[currentWaveIndex];

        /// <summary>현재 웨이브에서 아직 스폰되지 않은 적 미리보기(웨이브 미리보기 UI용).</summary>
        public IReadOnlyList<EnemySpec> UpcomingSpawns => CurrentWave.PeekRemaining();

        /// <summary>현재 살아있는 적들의 렌더용 스냅샷(위치·체력·상태). Id로 프레임 간 매핑.</summary>
        public IReadOnlyList<EnemyView> SnapshotEnemies()
        {
            var views = new List<EnemyView>(enemies.Count);
            foreach (var e in enemies)
                views.Add(new EnemyView(e.Id, e.Tracker.Position, e.Unit.Hp, e.Unit.MaxHp,
                    e.Unit.IsSlowed, e.Unit.IsPoisoned));
            return views;
        }

        /// <summary>배치된 타워들의 렌더용 스냅샷(위치·레벨·사거리).</summary>
        public IReadOnlyList<TowerView> SnapshotTowers()
        {
            var views = new List<TowerView>(towers.Count);
            foreach (var t in towers)
                views.Add(new TowerView(t.Position, t.Unit.Level, t.Unit.Range, t.Unit.IsSplash));
            return views;
        }

        public GameLoop(GameLoopConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            waypoints = config.Waypoints;
            waves = config.Stage.Waves;
            refundRatio = config.RefundRatio;
            wavePrepDuration = config.WavePrepDuration;
            earlyBonusPerSecond = config.EarlyBonusPerSecond;
            Economy = new Economy(config.StartingGold);
            Life = new LifeSystem(config.StartingLives);
            currentWaveIndex = 0;
            waveInProgress = false;
            waveElapsed = 0f;

            skills = config.Skills;
            meteorCd = new Cooldown(skills.MeteorCooldown);
            timeStopCd = new Cooldown(skills.TimeStopCooldown);
            goldRushCd = new Cooldown(skills.GoldRushCooldown);
        }

        // --- 스킬 상태 조회 (UI/테스트용) ---
        public bool MeteorReady => meteorCd.IsReady && Phase.Current == global::Game.Core.Phase.Playing;
        public bool TimeStopReady => timeStopCd.IsReady && Phase.Current == global::Game.Core.Phase.Playing;
        public bool GoldRushReady => goldRushCd.IsReady && Phase.Current == global::Game.Core.Phase.Playing;
        public bool IsTimeStopped => freezeRemaining > 0f;
        public bool IsGoldRushActive => goldRushRemaining > 0f;

        /// <summary>골드를 차감하고 타워를 배치한다. 골드가 부족하면 배치하지 않고 false를 반환한다.</summary>
        public bool TryBuildTower(Vec2 position, int cost, TowerUnit tower)
        {
            if (tower == null)
                throw new ArgumentNullException(nameof(tower));
            try
            {
                Economy.Spend(cost);
            }
            catch (InsufficientGoldException)
            {
                return false;
            }
            towers.Add(new PlacedTower { Position = position, Unit = tower, CostPaid = cost });
            return true;
        }

        /// <summary>
        /// 지정 위치의 타워를 철거하고 지불 비용의 RefundRatio만큼(내림) 환불한다.
        /// 환불액을 반환하며, 해당 위치에 타워가 없으면 -1을 반환한다(아무 변화 없음).
        /// </summary>
        public int SellTowerAt(Vec2 position)
        {
            int index = towers.FindIndex(t => t.Position.Equals(position));
            if (index < 0)
                return -1;

            int refunded = (int)(towers[index].CostPaid * refundRatio);
            towers.RemoveAt(index);
            Economy.Refund(refunded);
            return refunded;
        }

        /// <summary>
        /// 지정 위치의 타워를 업그레이드한다. 골드를 차감하고 데미지·사거리를 올린다.
        /// 해당 위치에 타워가 없거나 골드가 부족하면 false를 반환하고 아무 변화도 없다.
        /// </summary>
        public bool TryUpgradeTower(Vec2 position, int cost, int addedDamage, float addedRange = 0f)
        {
            var tower = towers.Find(t => t.Position.Equals(position));
            if (tower == null)
                return false;
            try
            {
                Economy.Spend(cost);
            }
            catch (InsufficientGoldException)
            {
                return false;
            }
            tower.Unit.Upgrade(addedDamage, addedRange);
            return true;
        }

        /// <summary>
        /// 웨이브를 시작한다.
        /// - 첫 웨이브: Ready→Playing으로 전이하며 시작.
        /// - 웨이브 사이(IsBetweenWaves): 다음 웨이브를 시작하고, 준비 단계가 있으면 남은 시간만큼 보너스 골드 지급.
        /// 그 외 상태에서는 무시한다.
        /// </summary>
        public void StartWave()
        {
            if (Phase.Current == global::Game.Core.Phase.Ready)
            {
                Phase.StartWave();
                BeginCurrentWave(awardEarlyBonus: false); // 첫 웨이브는 보너스 없음(여유롭게 준비)
                return;
            }
            if (IsBetweenWaves)
                BeginCurrentWave(awardEarlyBonus: true);
        }

        private void BeginCurrentWave(bool awardEarlyBonus)
        {
            if (awardEarlyBonus && prepTimer != null)
            {
                int bonus = prepTimer.Start();
                if (bonus > 0)
                    Economy.AddBonus(bonus);
            }
            prepTimer = null;
            waveInProgress = true;
            waveElapsed = 0f;
        }

        /// <summary>운석 낙하: 지정 범위에 즉시 광역 피해. 쿨다운 중이면 false.</summary>
        public bool TryCastMeteor(Vec2 center)
        {
            if (!MeteorReady)
                return false;
            var affected = splashResolver.Resolve(center, skills.MeteorRadius, BuildCandidates());
            foreach (var enemy in affected)
                enemy.TakeDamage(skills.MeteorDamage);
            meteorCd.Trigger();
            CollectDeadAndReward();
            return true;
        }

        /// <summary>시간 정지: 일정 시간 모든 적을 정지시킨다(타워는 계속 발사). 쿨다운 중이면 false.</summary>
        public bool TryCastTimeStop()
        {
            if (!TimeStopReady)
                return false;
            freezeRemaining = skills.TimeStopDuration;
            timeStopCd.Trigger();
            return true;
        }

        /// <summary>골드 러시: 일정 시간 처치 골드를 배수로. 쿨다운 중이면 false.</summary>
        public bool TryCastGoldRush()
        {
            if (!GoldRushReady)
                return false;
            goldRushRemaining = skills.GoldRushDuration;
            goldRushCd.Trigger();
            return true;
        }

        /// <summary>한 스텝 진행. 전투 중이 아니면 아무 일도 하지 않는다.</summary>
        public void Update(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "경과 시간은 음수일 수 없습니다.");
            // 전투 중일 때만 진행한다(Ready/Won/Lost에서는 무시).
            // 'Phase'는 GamePhase 인스턴스 프로퍼티명이라, 열거형 타입은 정규화해 참조한다.
            if (Phase.Current != global::Game.Core.Phase.Playing)
                return;

            TickSkillTimers(deltaTime); // 쿨다운 회복 + 시간정지/골드러시 지속시간 감소(준비 단계에서도 진행)

            if (!waveInProgress)
            {
                // 웨이브 사이 준비 단계: 카운트다운을 진행하고, 다 지나면 자동으로 다음 웨이브 시작(보너스 없음).
                if (prepTimer != null)
                {
                    prepTimer.Tick(deltaTime);
                    if (prepTimer.Expired)
                        BeginCurrentWave(awardEarlyBonus: false);
                }
                return;
            }

            waveElapsed += deltaTime;

            SpawnDueEnemies();
            TickEnemyStatus(deltaTime);     // 독 지속피해 적용·둔화 감소
            CollectDeadAndReward();         // 독으로 죽은 적 정산
            if (!IsTimeStopped)             // 시간 정지 중에는 적이 이동하지 않는다
                MoveEnemiesAndCountLeaks(deltaTime);
            FireTowers(deltaTime);          // 정지 중에도 타워는 발사
            EvaluateEndConditions();
        }

        private void TickSkillTimers(float deltaTime)
        {
            meteorCd.Tick(deltaTime);
            timeStopCd.Tick(deltaTime);
            goldRushCd.Tick(deltaTime);
            if (freezeRemaining > 0f)
                freezeRemaining = Math.Max(0f, freezeRemaining - deltaTime);
            if (goldRushRemaining > 0f)
                goldRushRemaining = Math.Max(0f, goldRushRemaining - deltaTime);
        }

        private List<TargetSelector.Candidate> BuildCandidates()
        {
            var candidates = new List<TargetSelector.Candidate>(enemies.Count);
            foreach (var e in enemies)
                candidates.Add(new TargetSelector.Candidate(e.Unit, e.Tracker.Position, e.Tracker.Progress));
            return candidates;
        }

        private void TickEnemyStatus(float deltaTime)
        {
            foreach (var e in enemies)
                e.Unit.TickStatus(deltaTime);
        }

        /// <summary>죽은 적에게 보상을 지급하고 목록에서 제거한다. 골드 러시 중이면 보상이 배수.</summary>
        private void CollectDeadAndReward()
        {
            int multiplier = IsGoldRushActive ? skills.GoldRushMultiplier : 1;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].Unit.IsDead)
                {
                    Economy.AddKillReward(enemies[i].Unit.GoldReward * multiplier);
                    KilledCount++;
                    enemies.RemoveAt(i);
                }
            }
        }

        private void SpawnDueEnemies()
        {
            foreach (var spec in CurrentWave.Collect(waveElapsed))
            {
                enemies.Add(new ActiveEnemy
                {
                    Id = nextEnemyId++,
                    Unit = new EnemyUnit(spec),
                    Tracker = new PathTracker(waypoints),
                });
            }
        }

        private void MoveEnemiesAndCountLeaks(float deltaTime)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                e.Tracker.Advance(e.Unit.EffectiveSpeed * deltaTime);
                if (e.Tracker.ReachedEnd)
                {
                    Life.LoseLife();
                    LeakedCount++;
                    enemies.RemoveAt(i);
                }
            }
        }

        private void FireTowers(float deltaTime)
        {
            // 타깃 후보는 현재 살아있는 적들의 (본체, 위치, 진행도) 스냅샷.
            var candidates = BuildCandidates();

            foreach (var tower in towers)
            {
                tower.Unit.Tick(deltaTime);
                if (!tower.Unit.CanFire)
                    continue;

                var picked = selector.SelectCandidate(tower.Position, tower.Unit.Range, candidates);
                if (picked == null)
                    continue;

                // 단일 타깃이면 그 적만, 광역(대포탑)이면 착탄점 반경 내 모든 적이 피격 대상.
                IReadOnlyList<EnemyUnit> hitUnits = tower.Unit.IsSplash
                    ? splashResolver.Resolve(picked.Value.Position, tower.Unit.SplashRadius, candidates)
                    : new[] { picked.Value.Unit };

                var effect = tower.Unit.Effect;
                foreach (var enemy in hitUnits)
                {
                    enemy.TakeDamage(tower.Unit.Damage);
                    if (effect.AppliesSlow)
                        enemy.ApplySlow(effect.SlowFactor, effect.SlowDuration);
                    if (effect.AppliesPoison)
                        enemy.ApplyPoison(effect.PoisonDps, effect.PoisonDuration);
                }
                tower.Unit.OnFired();
            }

            CollectDeadAndReward();
        }

        private void EvaluateEndConditions()
        {
            if (Life.IsDepleted)
            {
                Phase.NotifyLifeDepleted();
                return;
            }

            // 현재 웨이브 클리어 조건: 더 스폰할 적이 없고(AllSpawned) 필드에도 살아있는 적이 0.
            if (!(CurrentWave.AllSpawned && enemies.Count == 0))
                return;

            if (currentWaveIndex < waves.Count - 1)
            {
                // 다음 웨이브가 남아있음 → 클리어된 웨이브를 넘긴다.
                currentWaveIndex++;
                if (wavePrepDuration > 0f)
                {
                    // 준비 단계로 들어가 플레이어의 다음 웨이브 시작을 기다린다(조기 시작 시 보너스).
                    waveInProgress = false;
                    prepTimer = new WaveStartTimer(wavePrepDuration, earlyBonusPerSecond);
                }
                else
                {
                    // 준비 단계가 없으면 웨이브를 자동으로 연달아 진행한다.
                    waveElapsed = 0f;
                }
            }
            else
            {
                // 마지막 웨이브까지 클리어 → 승리.
                Phase.NotifyAllCleared();
            }
        }
    }
}
