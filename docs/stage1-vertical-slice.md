# Stage 1 수직 슬라이스 설계서

> **수직 슬라이스(Vertical Slice)** = 가장 얇지만 처음부터 끝까지 실제로 플레이되는 한 판.
> 화면이 화려하거나 콘텐츠가 많을 필요는 없다. "타워를 깔고 → 적이 길을 따라 오고 → 타워가 쏘고 → 적이 죽거나 새고 → 승패가 결정된다"가 **끊김 없이** 돌아가면 성공이다.
> 이 문서는 그 한 판을 만들기 위해 필요한 **모든 조각**을 클래스·이벤트·씬 단위로 확정한다. 이후 구현은 이 문서를 그대로 따른다.

이 슬라이스는 비전 문서 `vision.md`의 **구현 우선순위 1번**("1스테이지 + 기본 화살탑 + 보병만으로 한 판이 끝까지 돌아가게")에 해당한다. 상성·다중 타워·스킬·세이브는 이번 범위가 아니다(§10).

---

## 1. 목표와 완료 정의 (Definition of Done)

이 슬라이스는 아래가 **전부 동작**하면 완료다.

- [ ] 게임플레이 씬을 실행하면 직선 경로, 골드/라이프/웨이브 HUD가 보인다.
- [ ] 플레이어가 빌드 지점을 눌러 **기본 화살탑**을 구매·배치할 수 있다(골드 차감).
- [ ] 골드가 부족하면 배치가 거부되고 골드는 그대로다.
- [ ] "웨이브 시작" 버튼을 누르면 **보병**이 정해진 간격으로 스폰돼 경로를 따라 이동한다.
- [ ] 화살탑은 사거리 안의 적을 자동 조준해 발사 간격마다 발사하고, 명중 시 적 체력이 깎인다.
- [ ] 적 체력이 0이 되면 죽고 골드 보상이 들어온다.
- [ ] 적이 경로 끝(목표 지점)에 도달하면 라이프가 1 줄고 적은 사라진다.
- [ ] 모든 웨이브의 모든 적이 처리되면 **승리**, 라이프가 0이 되면 **패배** UI가 뜬다.
- [ ] 전체 한 판 동안 크래시·널 예외가 0건이다.
- [ ] `Game.Core`의 모든 로직 클래스가 EditMode 단위 테스트로 검증된다(아래 §4 테스트 케이스).

> **수치 목표 없음**: 별 평가·세이브·다중 스테이지·여러 타워/적은 의도적으로 제외(§10).

---

## 2. 한 판의 흐름 (시퀀스)

```
[씬 로드]
   │  GameManager.State = Ready
   ▼
[준비 단계]  플레이어가 골드로 타워 배치 (웨이브 시작 전/중 모두 가능)
   │  "웨이브 시작" 버튼 클릭
   ▼
[전투 단계]  GameManager.State = Playing
   │  WaveManager가 스폰 스케줄대로 보병 생성
   │  ┌──────────────────────────────────────────────┐
   │  │ 매 프레임:                                     │
   │  │  - 적: 경로 waypoint를 따라 전진               │
   │  │  - 타워: 사거리 내 타깃 탐색 → 간격마다 발사    │
   │  │  - 총알: 타깃으로 이동 → 명중 시 데미지         │
   │  │  - 적 HP 0  → 죽음 + 골드 보상 (OnEnemyKilled) │
   │  │  - 적 목표 도달 → 라이프 -1   (OnEnemyLeaked)  │
   │  └──────────────────────────────────────────────┘
   ▼
[종료 판정]
   │  라이프 == 0            → State = Lost  → 패배 UI
   │  모든 웨이브 소진 &&     → State = Won   → 승리 UI
   │  살아있는 적 0
```

핵심: **종료 판정은 두 가지 사건이 합쳐질 때만 일어난다.** 패배는 라이프 소진 즉시. 승리는 "더 스폰할 적이 없다 + 필드에 적이 0"이 **동시에** 성립할 때만(마지막 적이 죽거나 샐 때 검사).

---

## 3. 3-레이어 구조

`tech-env.md` 원칙에 따라 **게임 규칙은 순수 C#(`Game.Core`)로 분리**해 Unity 없이 테스트한다. MonoBehaviour는 입력·렌더·타이밍만 담당하는 얇은 껍데기다.

```
┌─────────────────────────────────────────────────────────────┐
│ Scene 레이어 (Unity 에디터에서 제작 — 코드 아님)              │
│   GameplayScene: 경로 waypoint, 빌드 지점, 카메라, Canvas    │
├─────────────────────────────────────────────────────────────┤
│ View/Manager 레이어 (MonoBehaviour — 얇게)                   │
│   GameManager  WaveManager  EconomyManager  TowerManager     │
│   Enemy(View)  Tower(View)  Projectile  + HUD 위젯           │
│        │  입력/타이밍을 Core에 위임, 결과를 이벤트로 통지     │
├─────────────────────────────────────────────────────────────┤
│ Game.Core 레이어 (순수 C# — TDD 대상, Unity 의존 0)          │
│   Economy  LifeSystem  WaveSchedule  PathTracker             │
│   EnemyUnit  TowerUnit  TargetSelector  GamePhase            │
└─────────────────────────────────────────────────────────────┘
```

**의존 방향은 위→아래 단방향.** Core는 Unity의 어떤 타입(`Vector3`, `MonoBehaviour`, `Time`)도 참조하지 않는다. 위치는 Core 자체의 `Vec2` 값 타입(또는 `(float x, float y)`)으로 다룬다.

> 왜 이렇게까지 분리하나? Unity 에디터를 켜지 않고 `dotnet test`만으로 게임 규칙 전체를 1초 안에 검증할 수 있기 때문. 첫 게임에서 "내 전투 계산이 맞나?"를 클릭 플레이로 확인하면 너무 느리고 불안정하다.

---

## 4. `Game.Core` 클래스별 상세 (TDD 대상)

각 클래스는 **순수 C#**이며, 표는 `책임 / 공개 API / 발행 신호 / 테스트 케이스` 순. 테스트 케이스가 곧 구현 전에 작성할 실패 테스트다(Red→Green→Refactor).

### 4.1 `Vec2` (값 타입)
- **책임**: Unity 비의존 2D 좌표·거리 계산.
- **API**: `Vec2(float x, float y)`, `float DistanceTo(Vec2 other)`, `Vec2 MoveTowards(Vec2 target, float maxDelta)`, 연산자 `+ - *`.
- **테스트**:
  - `DistanceTo_두점거리를_정확히계산한다` ((0,0)→(3,4)=5)
  - `MoveTowards_목표까지_거리보다_큰스텝이면_목표에_정확히_도달한다`
  - `MoveTowards_스텝이_작으면_그만큼만_이동한다`

### 4.2 `Economy` (이미 tech-env.md에 표준 패턴 존재)
- **책임**: 골드 보유·적립·차감.
- **API**: `int Gold`, `Economy(int starting)`, `void AddKillReward(int)`, `void Spend(int)` (부족 시 `InsufficientGoldException`).
- **테스트**:
  - `AddKillReward_적처치시_골드가_보상만큼_증가한다`
  - `Spend_충분하면_골드가_차감된다`
  - `Spend_골드부족시_예외를_던지고_잔액은_그대로다`
  - `생성자_시작골드가_음수면_예외`

### 4.3 `LifeSystem`
- **책임**: 라이프 보유·감소·소진 판정.
- **API**: `int Lives`, `bool IsDepleted`, `LifeSystem(int starting)`, `void LoseLife(int amount = 1)`.
- **테스트**:
  - `LoseLife_라이프가_감소한다`
  - `LoseLife_0이하로는_안내려간다` (음수 방지)
  - `IsDepleted_라이프0이면_true`
  - `생성자_시작라이프가_0이하면_예외`

### 4.4 `PathTracker`
- **책임**: waypoint 배열 위에서 한 유닛의 진행 상태 관리. "현재 위치 / 목표 도달 여부".
- **API**:
  - `PathTracker(IReadOnlyList<Vec2> waypoints)`
  - `Vec2 Position { get; }`
  - `bool ReachedEnd { get; }`
  - `void Advance(float distance)` — 주어진 거리만큼 경로를 따라 전진(코너를 넘어가면 다음 구간으로 자연 이어짐).
- **테스트**:
  - `초기위치는_첫_waypoint`
  - `Advance_직선구간에서_거리만큼_전진한다`
  - `Advance_waypoint를_넘어가면_다음구간으로_이어진다`
  - `Advance_마지막_waypoint를_넘으면_ReachedEnd_true이고_끝점에_고정된다`
  - `생성자_waypoint가_2개미만이면_예외`

### 4.5 `EnemyUnit`
- **책임**: 한 적의 전투 상태(체력·속도·보상). 위치/이동은 `PathTracker`가, 체력은 여기가.
- **API**:
  - `EnemyUnit(int maxHp, float speed, int goldReward)`
  - `int Hp`, `bool IsDead`, `float Speed`, `int GoldReward`
  - `void TakeDamage(int amount)` — 0 미만으로 안 내려감.
- **테스트**:
  - `TakeDamage_체력이_감소한다`
  - `TakeDamage_체력은_0미만으로_안내려간다`
  - `IsDead_체력0이면_true`
  - `TakeDamage_이미죽은적은_상태불변` (중복 처치 골드 방지의 근거)

### 4.6 `TargetSelector`
- **책임**: 타워 한 대가 사거리 내 적 중 **누구를 쏠지** 결정. 슬라이스 정책 = **목표에 가장 가까운 적 우선**(가장 위험한 적 먼저). 단순함을 위해 "경로를 가장 많이 전진한 적" = 리스트 인덱스가 아니라 진행도 기준.
- **API**: `EnemyUnit Select(Vec2 towerPos, float range, IReadOnlyList<(EnemyUnit unit, Vec2 pos, float progress)> candidates)` → 없으면 `null`.
- **테스트**:
  - `사거리밖_적은_무시한다`
  - `사거리내_적이없으면_null`
  - `사거리내_여러적중_진행도가_가장높은적을_고른다`
  - `죽은적은_후보에서_제외된다`

### 4.7 `TowerUnit`
- **책임**: 한 타워의 발사 타이밍 관리(쿨다운)와 데미지 값 보유.
- **API**:
  - `TowerUnit(float range, float fireInterval, int damage)`
  - `float Range`, `int Damage`
  - `bool CanFire { get; }` / `void Tick(float deltaTime)` — 쿨다운 누적
  - `void OnFired()` — 발사 후 쿨다운 리셋
- **테스트**:
  - `초기상태는_즉시발사가능` (CanFire == true)
  - `OnFired_직후에는_발사불가`
  - `Tick_발사간격만큼_지나면_다시_발사가능`
  - `Tick_간격미만이면_여전히_발사불가`

### 4.8 `WaveSchedule`
- **책임**: 스테이지 1의 스폰 스케줄(어떤 적을, 몇 마리, 몇 초 간격으로). 시간 누적에 따라 "지금 스폰할 적"을 뱉고, 전부 소진됐는지 알려준다.
- **API**:
  - `WaveSchedule(IReadOnlyList<SpawnEntry> entries)` (`SpawnEntry { float time; EnemySpec spec; }`)
  - `IReadOnlyList<EnemySpec> Collect(float elapsedTime)` — 직전 호출 이후 도래한 스폰들을 반환.
  - `bool AllSpawned { get; }`
- **테스트**:
  - `Collect_도래한_스폰만_반환한다`
  - `Collect_같은스폰을_두번_반환하지않는다`
  - `AllSpawned_모두스폰되면_true`
  - `Collect_시간이_여러스폰을_한번에_지나치면_모두_반환한다`

### 4.9 `GamePhase` (상태기)
- **책임**: `Ready → Playing → Won | Lost` 전이 규칙을 한 곳에 모음. UI/매니저는 이 결과만 읽는다.
- **API**:
  - `enum Phase { Ready, Playing, Won, Lost }`
  - `Phase Current`
  - `void StartWave()` (Ready→Playing)
  - `void NotifyLifeDepleted()` (Playing→Lost)
  - `void NotifyAllCleared()` (Playing→Won; 조건: 스폰 끝 + 필드 적 0 — 호출 측이 보장)
- **테스트**:
  - `StartWave_Ready에서만_Playing으로_전이`
  - `NotifyLifeDepleted_Playing에서_Lost로_전이`
  - `NotifyAllCleared_Playing에서_Won으로_전이`
  - `종료상태에서는_더이상_전이하지_않는다` (Won/Lost는 흡수 상태)

> **`GameLoop` 통합 클래스(선택)**: 위 조각들을 한 판 단위로 묶어 "프레임마다 `Update(dt)` 호출 → 적 이동·타워 발사·판정"을 순수 C#로 돌리는 오케스트레이터를 둘 수 있다. 이게 있으면 **한 판 전체를 Unity 없이 시뮬레이션 테스트**할 수 있어 가장 강력하다. 슬라이스 1차에서는 선택이지만 강력 권장.

---

## 5. View / Manager 레이어 (MonoBehaviour, 얇게)

각 매니저는 **싱글톤**이며 Core 객체를 들고, 입력·타이밍을 위임하고 결과를 `event`로 통지한다(`tech-env.md` 표준 패턴).

| 클래스 | 역할 | 들고 있는 Core | 주요 입력 | 발행 이벤트 |
|--------|------|----------------|-----------|-------------|
| `GameManager` | 판 전체 상태·승패 UI 트리거 | `GamePhase` | 라이프 소진/전멸 통지 | `OnPhaseChanged(Phase)` |
| `EconomyManager` | 골드 보유·구매 | `Economy` | 적 처치 이벤트, 구매 요청 | `OnGoldChanged(int)` |
| `LifeManager` | 라이프 | `LifeSystem` | 적 누수 이벤트 | `OnLivesChanged(int)`, `OnDepleted` |
| `WaveManager` | 스폰·전멸 감지 | `WaveSchedule` | 웨이브 시작 버튼, 매 프레임 시간 | `OnAllEnemiesCleared` |
| `TowerManager` | 빌드 지점 클릭→배치 | (구매는 Economy 위임) | 빌드 지점 클릭 | `OnTowerPlaced` |
| `Enemy`(View) | `EnemyUnit`+`PathTracker` 시각화·이동 | 둘 다 | `Update`의 deltaTime | `OnAnyEnemyKilled(Enemy)`, `OnAnyEnemyLeaked(Enemy)` |
| `Tower`(View) | `TowerUnit`+`TargetSelector` 구동·발사 | 둘 다 | `Update`의 deltaTime | (총알 생성) |
| `Projectile` | 타깃까지 이동→명중 데미지 | — | — | — |

**오브젝트 풀**: `Enemy`, `Projectile`는 `UnityEngine.Pool.ObjectPool<T>`로 재사용(대량 생성 대비). 슬라이스에선 풀 크기 작게.

---

## 6. 씬 오브젝트 트리 (GameplayScene)

```
GameplayScene
├── Main Camera (Orthographic, 2D)
├── --- Systems ---
│   ├── GameManager
│   ├── EconomyManager
│   ├── LifeManager
│   ├── WaveManager      (참조: PathRoot, EnemyPrefab, 스폰 스케줄 SO)
│   └── TowerManager     (참조: TowerPrefab, BuildSpot 목록)
├── --- World ---
│   ├── PathRoot
│   │   ├── Waypoint_0 (Spawn)
│   │   ├── Waypoint_1
│   │   └── Waypoint_2 (Goal)      ← 직선이면 0,1,2 일직선
│   ├── BuildSpots
│   │   ├── BuildSpot_A
│   │   └── BuildSpot_B            ← 경로 옆 배치 가능 지점
│   └── (런타임 생성) Enemies / Projectiles
└── --- UI Canvas (Screen Space) ---
    ├── GoldText
    ├── LivesText
    ├── WaveText
    ├── StartWaveButton
    ├── WinPanel  (비활성, Won 시 활성)
    └── LosePanel (비활성, Lost 시 활성)
```

> Waypoint·BuildSpot은 빈 GameObject의 `transform.position`만 쓴다. `WaveManager`가 시작 시 이 좌표들을 Core의 `Vec2` 리스트로 변환해 `PathTracker`/`WaveSchedule`에 넘긴다(좌표 변환은 이 한 경계에서만).

---

## 7. 이벤트 흐름표

느슨한 결합을 위해 매니저 간 직접 호출 대신 `event`로 연결한다.

| 사건 | 발행자 | 구독자 → 반응 |
|------|--------|---------------|
| 적 처치 | `Enemy.OnAnyEnemyKilled` | `EconomyManager`→골드 보상 / `WaveManager`→전멸 체크 |
| 적 누수(목표 도달) | `Enemy.OnAnyEnemyLeaked` | `LifeManager`→라이프 -1 / `WaveManager`→전멸 체크 |
| 골드 변동 | `EconomyManager.OnGoldChanged` | `GoldText`→갱신 |
| 라이프 변동 | `LifeManager.OnLivesChanged` | `LivesText`→갱신 |
| 라이프 소진 | `LifeManager.OnDepleted` | `GameManager`→`GamePhase.NotifyLifeDepleted()` |
| 전멸 + 스폰종료 | `WaveManager.OnAllEnemiesCleared` | `GameManager`→`GamePhase.NotifyAllCleared()` |
| 상태 변경 | `GameManager.OnPhaseChanged` | `WinPanel`/`LosePanel`→표시 |
| 웨이브 시작 클릭 | `StartWaveButton` | `GameManager`→`StartWave()` / `WaveManager`→스폰 개시 |
| 빌드 지점 클릭 | `BuildSpot` | `TowerManager`→`Economy.TryPurchase` 후 타워 배치 |

**전멸 판정 책임은 `WaveManager` 한 곳**: 적이 죽거나 샐 때마다 `if (schedule.AllSpawned && 살아있는 적 == 0) OnAllEnemiesCleared`.

---

## 8. 스테이지 1 밸런싱 수치 (확정값)

슬라이스라 한 벌만 확정한다. 추후 ScriptableObject로 옮길 값들.

| 항목 | 값 | 비고 |
|------|----|------|
| 시작 골드 | 100 | 화살탑 2대 + 약간 |
| 시작 라이프 | 10 | 보병 10마리면 전부 새도 정확히 패배 |
| 화살탑 비용 | 40 | |
| 화살탑 사거리 | 2.5 (월드 유닛) | |
| 화살탑 발사 간격 | 0.8초 | |
| 화살탑 데미지 | 10 | |
| 보병 체력 | 30 | 화살 3발에 사망 |
| 보병 이동 속도 | 1.5 유닛/초 | |
| 보병 처치 보상 | 8 골드 | |
| 보병 누수 피해 | 라이프 -1 | |
| 웨이브 구성 | 보병 10마리, 1.0초 간격, 1웨이브 | |
| 경로 길이 | 약 12 유닛(직선) | 통과에 약 8초 |

**설계 의도 검증**: 화살탑 1대(0.8초마다 10뎀)는 8초 통과 동안 약 10발 → 100뎀 → 보병 3마리 처치. 10마리를 다 막으려면 최소 화살탑 4대 또는 길목 집중. 100골드로 2대 시작 → 처치 보상으로 증설하는 경제 압박이 자연 발생. (실제 값은 플레이 후 튜닝.)

---

## 9. TDD 구현 순서

`Game.Core`부터 **Red→Green→Refactor**로 쌓고, 그 다음 얇은 MonoBehaviour로 감싼다. 각 단계는 독립적으로 테스트 통과 후 다음으로.

1. `Vec2` — 거리·이동 (가장 바닥, 의존 0)
2. `Economy` — tech-env 표준 패턴 그대로
3. `LifeSystem`
4. `EnemyUnit` — 체력·사망
5. `TowerUnit` — 발사 쿨다운
6. `PathTracker` — 경로 전진·도달
7. `TargetSelector` — 타깃 선정
8. `WaveSchedule` — 스폰 스케줄
9. `GamePhase` — 상태 전이
10. *(선택·강력 권장)* `GameLoop` — 1~9를 묶어 **한 판 전체 시뮬레이션 테스트** ("화살탑 1대로 보병 3마리 죽고 7마리 샌다" 류의 통합 테스트)
11. MonoBehaviour 매니저·뷰 작성(Unity 설치 후) → 씬 조립 → PlayMode 스모크 테스트

> **CLI에서 1~10까지 가능**: 순수 C#라 별도 `dotnet` 테스트 프로젝트(NUnit)로 Unity 없이 전부 작성·실행·검증할 수 있다. Unity 설치 후 이 Core를 그대로 Unity 프로젝트에 복사해 11단계만 진행하면 된다.

---

## 10. 이번 슬라이스 범위 밖 (의도적 제외)

다음 슬라이스로 미룬다. 지금 넣으면 "한 판이 돌아간다"가 늦어진다.

- 추가 타워(대포·빙결·저격 등) 및 **상성** — 슬라이스 2의 핵심
- 추가 적(군집·중장갑 등)
- 스킬 3종, 타워 업그레이드·판매, 사거리 표시, 웨이브 미리보기
- 별 평가, 세이브(PlayerPrefs), 스테이지 선택/메인 메뉴 씬
- 다중 경로·곡선 맵 (직선 1경로만)
- DOTween 연출, 사운드, 파티클

---

## 부록 A. 다음 액션 (Unity 설치 전/후)

- **지금(설치 전, CLI)**: §9의 1~10단계 — `Game.Core` + 단위/통합 테스트를 standalone `dotnet` 프로젝트로 TDD 구현.
- **설치 후**: Unity 6 LTS + Android 모듈 설치 → 이 폴더를 Unity 프로젝트로 구성 → Core 코드 이식 → §5·§6 매니저/씬 조립 → §1 DoD 체크.
