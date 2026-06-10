# Unity 포팅 가이드

> 이 문서는 `core/`의 검증된 순수 C# 로직(`Game.Core`, 188개 테스트 통과)을 **Unity 6 프로젝트로 옮겨 화면에 띄우는** 작업의 청사진이다.
> 다른 PC(Unity 설치된)에서 이 저장소를 `git clone`한 뒤, 이 문서만 보고 순서대로 따라가면 된다.
> 설계 근거: `docs/vision.md`, `docs/tech-env.md`, `docs/stage1-vertical-slice.md`, `core/README.md`

---

## 0. 전제 & 핵심 원칙

- **Core는 거의 안 고친다.** `Game.Core/*.cs`는 UnityEngine 의존이 0이라 그대로 복사하면 컴파일된다(netstandard2.1 / C# 9 = Unity 6와 호환).
- **MonoBehaviour는 얇게.** 입력·렌더·타이밍만 담당하고, 게임 규칙은 전부 Core에 위임한다.
- **뷰 스냅샷 API는 이미 추가됨(Task 0 완료)**: 화면이 적·타워를 그리려면 위치·체력을 읽어야 해서 `GameLoop`에 읽기 전용 투영을 더해 두었다(§3). 그래서 Unity 쪽은 **복붙 + 뷰 작업**만 남는다.

### 채택할 통합 모델 — "GameLoop 단일 소스(Model B)"

`docs/stage1-vertical-slice.md` §5는 각 Enemy/Tower MonoBehaviour가 자기 Core 객체를 들고 스스로 움직이는 모델(Model A)을 제안했었다. 하지만 그 문서 이후 `GameLoop`이 **멀티 웨이브·상성·스킬·경제·번개탑·골드탑·타게팅 모드까지 전부 통합·테스트**되었으므로, 이제는:

> **`GameLoop`이 모든 시뮬레이션의 단일 소스. MonoBehaviour는 (1) 매 프레임 `loop.Update(dt)` 호출, (2) 스냅샷을 읽어 GameObject를 배치/갱신, (3) 입력을 `loop`의 메서드로 전달.**

이게 더 낫다 — 188개로 검증된 오케스트레이션을 **재구현하지 않고 그대로 재사용**한다. Model A로 가면 그 로직을 MonoBehaviour에 다시 짜야 하고, 그건 테스트 밖이다.

---

## 1. 이식 순서 체크리스트

- [x] **Task 0** — Core에 뷰 스냅샷 API 추가 (§3). ✅ **완료**(이 PC에서 TDD로 구현). 이후 Core는 188 테스트까지 확장됨(타워 8종·적 6종·스테이지 1~10·해금·번개탑·골드탑·타게팅 모드·E2E).
- [ ] **Task 1** — Unity 6 LTS + Android 모듈 설치, 2D(URP) 프로젝트 생성.
- [ ] **Task 2** — `Game.Core/*.cs`를 `Assets/Scripts/Core/`로 복사, asmdef 구성 (§2).
- [ ] **Task 3** — EditMode 테스트 이식 (§7). Unity Test Runner에서 188개 녹색 확인.
- [ ] **Task 4** — `GameDirector`(단일 매니저)로 `GameLoop` 구동 + 좌표/시간 어댑터 (§4, §5).
- [ ] **Task 5** — 적/타워/총알 뷰 풀링 렌더 + HUD 바인딩 (§4). 보스·체인·타게팅은 스냅샷에 포함됨(§3.1).
- [ ] **Task 6** — 입력(빌드/판매/업그레이드/스킬/타게팅 토글) 배선 (§4).
- [ ] **Task 7** — 밸런싱 데이터 연결: 1차엔 코드 카탈로그(`StageCatalog`/`TowerCatalog`/`EnemyCatalog`) 그대로 사용, 이후 ScriptableObject로 (§6).
- [ ] **Task 8** — 세이브·스테이지 선택/해금 연결 (`SaveStore`/`StageProgress`/`StarRating` ↔ PlayerPrefs) (§8).
- [ ] **Task 9** — DOTween 연출·사운드·폴리시 (마지막).

---

## 2. 폴더 & asmdef 구조

```
Assets/
├── Scripts/
│   ├── Core/                  ← Game.Core/*.cs 그대로 복사
│   │   └── Game.Core.asmdef       (name: Game.Core, 플랫폼 제한 없음)
│   ├── Game/                  ← MonoBehaviour 어댑터/뷰 (신규 작성)
│   │   └── Game.asmdef             (참조: Game.Core)
│   └── Tests/
│       └── EditMode/          ← Game.Core.Tests/*.cs 복사
│           └── Game.Core.Tests.EditMode.asmdef
│               (참조: Game.Core, UnityEngine.TestRunner, NUnit; includePlatforms: 비움 또는 Editor)
├── Prefabs/   (Enemy, Tower, Projectile, HUD ...)
├── Scenes/    (Gameplay.unity 등)
└── Data/      (ScriptableObject 밸런싱 에셋)
```

- Core를 **별도 asmdef**로 두면 컴파일 경계가 생겨, View 코드가 실수로 Core에 침투하는 걸 막는다.
- asmdef의 `Allow 'unsafe' Code`·플랫폼 설정은 기본값이면 충분.

---

## 3. 뷰 스냅샷 API (✅ Task 0 완료 — 이미 Core에 있음)

화면이 매 프레임 각 적/타워를 그리려면 위치·체력·상태를 읽어야 한다. `GameLoop`에 **읽기 전용 투영**을 이미 추가해 두었다(additive, 현재 188 테스트). Unity 쪽은 아래 API를 **그대로 호출**하면 된다.

**이미 구현된 것:**
- `EnemyUnit.MaxHp` (체력바 분모)
- 적마다 안정적 `Id` (프레임 간 GameObject 매핑용)
- `Game.Core/ViewSnapshots.cs` 의 `EnemyView` / `TowerView` 구조체
- `GameLoop.SnapshotEnemies()` / `GameLoop.SnapshotTowers()`

```csharp
// ViewSnapshots.cs (이미 존재)
public readonly struct EnemyView   // Id, Position(Vec2), Hp, MaxHp, IsSlowed, IsPoisoned, IsBoss
public readonly struct TowerView   // Position(Vec2), Level, Range, IsSplash, IsChain, Targeting

// GameLoop (이미 존재)
IReadOnlyList<EnemyView> SnapshotEnemies();   // 살아있는 적들
IReadOnlyList<TowerView> SnapshotTowers();    // 배치된 타워들
```

§4.2에서 이 스냅샷으로 GameObject를 동기화한다.

> **발사 연출 이벤트(선택, 아직 없음)**: 총알/타격 이펙트를 정밀히 그리려면 "이 타워가 이 적을 쐈다"가 필요하다. 1차엔 생략하고 뷰에서 흉내내도 되며, 필요하면 `GameLoop`에 `event Action<Vec2 from, Vec2 to> OnTowerFired`를 additive로 추가하면 된다.

### 3.1 보스·체인·타게팅도 스냅샷에 포함됨 (✅ 추가 완료)

초안엔 보스·체인·타게팅이 스냅샷에 빠져 있었으나, 이제 모두 노출된다(`ViewSnapshotTests`로 검증).

| 상태 | 스냅샷 필드 | 뷰 활용 |
|------|------|------|
| **적 보스 여부** (`EnemyUnit.IsBoss`) | `EnemyView.IsBoss` | 보스 스프라이트/크기 구분, 등장 연출 |
| **타워 체인 여부** (`TowerUnit.IsChain`) | `TowerView.IsChain` | 번개탑 스프라이트·체인 이펙트 |
| **타워 타게팅 모드** (`TowerUnit.Targeting`) | `TowerView.Targeting` | 선택 패널에 현재 조준 정책 표시 |

> 개별 적의 보스 여부는 `EnemyView.IsBoss`로, 판 전체의 "보스 등장 경고"는 `GameLoop.BossActive`로 — 둘 다 쓸 수 있다(전자는 렌더, 후자는 HUD 배너/BGM).
>
> 아직 노출 안 된 것: 타워의 **효과 종류 상세**(둔화/독/골드보너스 구분). `TowerView`엔 `IsSplash`/`IsChain`만 있다. 타워별 스프라이트를 효과까지 세분하려면 `TowerView`에 필드를 additive로 더하면 된다(기존 188 테스트 안 깨짐).

---

## 4. Task 4~6 — MonoBehaviour 어댑터 (`GameDirector`)

씬에 단일 매니저 `GameDirector` 하나로 시작한다(슬라이스 단계엔 매니저를 잘게 쪼갤 필요 없음).

```csharp
public class GameDirector : MonoBehaviour
{
    [SerializeField] Transform pathRoot;          // 자식 Waypoint들의 위치를 경로로
    [SerializeField] EnemyView enemyPrefab;       // (MonoBehaviour 뷰)
    [SerializeField] TowerView towerPrefab;
    [SerializeField] StageData stageData;         // ScriptableObject (§6)
    [SerializeField] HudBinder hud;

    GameLoop loop;
    readonly Dictionary<int, EnemyGO> enemyGOs = new();   // Id → GameObject

    void Start()
    {
        var waypoints = ReadWaypoints(pathRoot);          // Transform → List<Vec2>
        var config = stageData.BuildConfig(waypoints);    // GameLoopConfig 생성
        loop = new GameLoop(config);
    }

    void Update()
    {
        StepSimulation(Time.deltaTime);   // §5 고정 스텝
        SyncEnemies();                    // 스냅샷 → GameObject 배치/생성/파괴
        SyncTowers();
        hud.Render(loop);                 // 골드/라이프/웨이브/스킬 쿨다운
    }
    // 입력 핸들러 → loop.TryBuildTower / SellTowerAt / TryUpgradeTower / TryCast*
}
```

### 4.1 좌표 변환은 **한 곳에서만**

Core는 `Vec2`, Unity는 `Vector3`. 변환 함수를 한 군데(static 유틸)로 모은다. 2D면 보통 `Vec2(x,y) ↔ new Vector3(x, y, 0)`.

```csharp
public static class CoreConvert
{
    public static Vector3 ToWorld(Vec2 v) => new Vector3(v.X, v.Y, 0f);
    public static Vec2 ToCore(Vector3 p) => new Vec2(p.x, p.y);
}
```

경로 waypoint, 타워 배치 위치(탭한 화면 좌표 → 월드 → `Vec2`), 스냅샷 위치 모두 이 경계만 거친다.

### 4.2 적/타워 뷰 동기화 (풀링)

```csharp
void SyncEnemies()
{
    var live = new HashSet<int>();
    foreach (var e in loop.SnapshotEnemies())
    {
        live.Add(e.Id);
        if (!enemyGOs.TryGetValue(e.Id, out var go))
            enemyGOs[e.Id] = go = SpawnEnemyGO();        // ObjectPool<T>에서
        go.transform.position = CoreConvert.ToWorld(e.Position);
        go.SetHpBar((float)e.Hp / e.MaxHp);
        go.SetStatus(e.IsSlowed, e.IsPoisoned);          // 색/아이콘
    }
    // 스냅샷에 없는 Id = 죽거나 샌 적 → 풀로 반환
    RemoveMissing(live);
}
```

`UnityEngine.Pool.ObjectPool<T>`로 적·총알을 재사용(대량 스폰 대비).

### 4.3 입력 배선

| 입력 | 호출 |
|------|------|
| 빌드 지점 탭 | `var spec = TowerCatalog.Arrow; loop.TryBuildTower(corePos, spec.Cost, spec.CreateUnit())` → false면 "골드 부족" 피드백 |
| 타워 탭 → 판매 | `int refund = loop.SellTowerAt(corePos)` (-1이면 없음) |
| 타워 탭 → 업그레이드 | `loop.TryUpgradeTower(corePos, cost, +dmg, +range)` |
| 타워 탭 → 타게팅 변경 | `loop.SetTowerTargetingAt(corePos, TargetingMode.Strongest)` (선두/가까운/강한 순환) |
| 웨이브 시작 버튼 | `loop.StartWave()` |
| 스킬 버튼 3종 | `loop.TryCastMeteor(targetPos)` / `loop.TryCastTimeStop()` / `loop.TryCastGoldRush()` |

> 타워 식별은 **위치(Vec2) 기준**이다(`SellTowerAt`/`TryUpgradeTower`/`SetTowerTargetingAt`가 위치로 찾음). 빌드 지점 좌표를 그대로 키로 쓰면 깔끔하다.
>
> **타워 종류 = `TowerCatalog`의 8종**(`Arrow`/`Cannon`/`Frost`/`Sniper`/`RapidFire`/`Poison`/`Lightning`/`Gold`). 각 `TowerSpec`이 `Cost`와 `CreateUnit()`(스탯·효과·체인 포함)을 주므로 별도 `MakeTower` 팩토리가 필요 없다. 빌드 메뉴는 `TowerCatalog.All()`로 채운다.

### 4.4 HUD 바인딩(폴링)

매 프레임 `loop`에서 읽어 갱신: `loop.Economy.Gold`, `loop.Life.Lives`, `loop.Phase.Current`, `loop.CurrentWaveNumber`/`TotalWaves`, `loop.UpcomingSpawns`(미리보기), `loop.MeteorReady` 등, `loop.IsBetweenWaves`/`PrepRemaining`, `loop.BossActive`(보스 등장 경고 배너/BGM 전환). 이벤트 기반이 필요하면 나중에 도입.

승패 패널: `loop.Phase.Current == Phase.Won | Lost`.

---

## 5. Task 4 — 시간(dt) 처리: **고정 스텝 권장**

`GameLoop.Update(dt)`는 **타워를 한 호출당 최대 1발** 발사한다. 가변 `Time.deltaTime`을 그대로 넣으면 프레임레이트에 따라 발사 타이밍·결과가 달라질 수 있고, 테스트(고정 dt로 검증)와도 어긋난다.

→ **고정 스텝 누적기**로 Core를 일정 간격(예: 0.05s 또는 0.02s)으로만 전진시킨다:

```csharp
const float STEP = 0.05f;
float acc;
void StepSimulation(float frameDt)
{
    acc += frameDt;
    while (acc >= STEP) { loop.Update(STEP); acc -= STEP; }
}
```

이러면 60fps든 30fps든 시뮬레이션 결과가 동일하고(결정론 유지), EditMode 테스트의 dt와 일관된다. 화면 움직임은 GameObject 보간으로 부드럽게.

---

## 6. Task 7 — 밸런싱 데이터: 카탈로그 → (나중에) ScriptableObject

> **중요한 변경**: 이 가이드 초안 이후, 밸런싱 데이터가 **이미 Core 안에 코드 카탈로그로 정리**되었다. 더 이상 흩어져 있지 않다.
>
> - `EnemyCatalog` — 적 6종 스탯(`EnemySpec`, 보스 플래그 포함)
> - `TowerCatalog` — 타워 8종 스탯·효과·비용(`TowerSpec` + `CreateUnit()`)
> - `StageCatalog` — 스테이지 1~10 경로·웨이브(`StageDefinition` + `ToConfig()`)

### 6.1 1차: 카탈로그를 그대로 쓴다 (SO 불필요)

수직 슬라이스 단계에선 **SO 변환을 건너뛰고** 코드 카탈로그를 직접 호출하는 게 가장 빠르다. `GameDirector.Start()`가 한 줄로 끝난다:

```csharp
// 경로조차 StageDefinition이 들고 있으므로 ToConfig() 하나면 GameLoop 준비 완료.
StageDefinition def = StageCatalog.All()[selectedStageIndex];
loop = new GameLoop(def.ToConfig());
// 경로 렌더용 좌표: def.Waypoints (Vec2) → CoreConvert.ToWorld 로 라인/배경 그리기
```

타워 빌드 메뉴도 `TowerCatalog.All()`을 그대로 순회해 버튼을 만들고, 탭 시 `spec.CreateUnit()`을 넘기면 된다(§4.3). **이 단계에선 인스펙터에서 수치를 못 바꾸지만, 그 대가로 통합이 즉시 된다.**

### 6.2 2차(선택): SO로 노출해 디자이너가 인스펙터에서 튜닝

수치를 코드 빌드 없이 만지고 싶어지면, **카탈로그를 SO로 감싼다**(값은 그대로, 그릇만 바뀜). 핵심은 *SO → Core 타입 변환 메서드*다:

```csharp
[CreateAssetMenu(menuName = "TD/Tower")]
public class TowerData : ScriptableObject
{
    public string displayName;
    public int cost;
    public float range, fireInterval;
    public int damage;
    public float splashRadius;
    // 효과·체인 파라미터도 필드로 노출
    public TowerSpec ToSpec() => new TowerSpec(displayName, cost, range, fireInterval, damage, splashRadius, /* effect, chain */);
}

[CreateAssetMenu(menuName = "TD/Stage")]
public class StageData : ScriptableObject
{
    public int startingGold, startingLives;
    public float wavePrepDuration;
    public int earlyBonusPerSecond;
    public WaveData[] waves;   // 각 웨이브의 스폰 목록(EnemyData 참조)

    public StageDefinition ToDefinition(string name, IReadOnlyList<Vec2> waypoints) =>
        new StageDefinition(name, waypoints,
            new Stage(waves.Select(w => w.ToSchedule()).ToList()),
            startingGold, startingLives, wavePrepDuration, earlyBonusPerSecond);
}
```

처음 SO 에셋의 **초기값은 카탈로그 수치를 그대로 베껴** 넣는다(이미 E2E로 클리어 가능성이 검증된 값이므로 안전한 출발점). **값은 바뀌지 않고 담는 그릇만 바뀐다 — Core 로직·테스트는 그대로.**

---

## 7. Task 3 — 테스트 이식

`Game.Core.Tests/*.cs`를 `Assets/Scripts/Tests/EditMode/`로 복사. Unity Test Framework도 **NUnit 3.x 기반**이라 `Assert.AreEqual` 등이 그대로 동작한다(이 점 때문에 standalone도 NUnit 3.x로 맞춰둠).

- EditMode 테스트 asmdef가 `Game.Core`를 참조하게.
- Test Runner(Window → General → Test Runner)에서 **188개 녹색** 확인 → Core 이식이 성공했다는 첫 증거.
- PlayMode 스모크 테스트는 §4 완성 후 "타워가 실제로 적을 처치" 정도만 추가(슬라이스 범위).

---

## 8. Task 8 — 세이브 + 스테이지 선택/해금 연결

`SaveStore`는 PlayerPrefs에 의존하지 않는다(순수). Unity 레이어가 **직렬화 문자열만 PlayerPrefs에 영속화**한다.

```csharp
const string KEY = "save.v1";
SaveStore LoadSave() => SaveStore.Deserialize(PlayerPrefs.GetString(KEY, ""));
void PersistSave(SaveStore s) { PlayerPrefs.SetString(KEY, s.Serialize()); PlayerPrefs.Save(); }

// 스테이지 클리어 시(stageId = 0-based 인덱스):
int stars = StarRating.FromLives(loop.Life.Lives, def.StartingLives);
var save = LoadSave();
save.RecordClear(stageId, stars);
PersistSave(save);
```

`SaveStore.Deserialize`는 손상·범위 밖 값을 무시하도록 이미 검증돼 있어 변조·구버전에 안전하다.

### 8.1 스테이지 선택 화면 (StageProgress)

해금 규칙은 `StageProgress`(Core)가 이미 들고 있다 — 직접 판정 로직을 짜지 말고 그대로 쓴다.

```csharp
var save = LoadSave();
var progress = new StageProgress(save);
var stages = StageCatalog.All();          // 10개 StageDefinition

for (int i = 0; i < stages.Count; i++)
{
    bool unlocked = progress.IsUnlocked(i);   // 0번은 항상, N번은 N-1 클리어 시
    int stars = save.GetStars(i);             // 0~3 (별 아이콘)
    // 버튼: stages[i].Name 표시, unlocked면 활성+별, 아니면 자물쇠
}

int focus = progress.NextStageToPlay(stages.Count); // "이어서 도전" 버튼이 가리킬 스테이지
```

> 클리어 → 세이브 기록 → 선택 화면 복귀 시 다음 스테이지가 자동 해금되는 흐름이 이 한 묶음으로 성립한다. 별점·해금 규칙은 Core에서 이미 테스트됨(`SaveStoreTests`/`StarRatingTests`/`StageProgressTests`).

---

## 9. 확정 설계 결정 (바꾸면 Core 수정)

| 결정 | 현재 | 바꾸려면 |
|------|------|----------|
| **즉시 명중** | 타워 발사 = 발사 순간 즉시 데미지. 총알은 **연출일 뿐**(빗맞힘 없음) | `GameLoop.FireTowers`를 총알 비행·명중 판정 모델로 교체 = Core 전투 로직 변경. 첫 게임엔 즉시 명중 권장(대부분 TD가 이렇게) |
| **타워 = 위치로 식별** | 같은 위치 중복 배치 가정 안 함 | 위치 충돌 검사를 View(빌드 지점)에서 막거나 Core에 추가 |
| **첫 웨이브 보너스 없음** | 조기 시작 보너스는 2웨이브부터 | 1웨이브에도 주려면 Ready 단계 준비 타이머 추가 |
| **시간 정지 중 타워는 발사** | freeze는 적 이동만 멈춤 | 타워도 멈추려면 `FireTowers`도 freeze 가드 |
| **체인은 즉시 점프** | 번개탑도 즉시 명중처럼 한 프레임에 인접 적으로 점프·감쇠 | 점프 간 지연/연출은 View에서만(규칙은 즉시) |
| **처치 보너스 골드 ≠ 골드러시 배수** | 골드탑 보너스는 고정, 골드러시 배수는 처치보상에만 적용 | 보너스에도 배수 주려면 `HitEnemy`에서 곱 |
| **타게팅 기본 First(선두)** | 새 타워는 선두 우선 | 종류별 기본값을 다르게 하려면 `TowerSpec`에 모드 추가 |

이 결정들은 의도된 단순화다. 화면에서 손맛을 보고 바꾸고 싶으면 **Core에서 TDD로** 바꾼 뒤 push → 다른 PC에서 pull.

---

## 10. 자주 막히는 지점

- **`Vec2`와 `Vector2/3` 혼동**: Core는 절대 `UnityEngine.Vector*`를 import하지 않는다. 변환은 §4.1 한 곳에서만.
- **스냅샷 ID 없이 인덱스로 매핑**: 적이 죽으며 리스트가 흔들려 엉뚱한 GameObject가 움직인다 → 반드시 `Id` 기반 매핑(§3.2).
- **가변 dt로 Update**: 프레임레이트에 결과가 흔들림 → 고정 스텝(§5).
- **asmdef 참조 누락**: View가 `Game.Core`를 못 찾으면 asmdef References 확인.
- **NUnit 버전**: Unity 내장(3.x) 사용. 별도 NuGet NUnit 넣지 말 것.
- **체력바 분모**: `EnemyView.MaxHp` 사용(이미 제공됨).

---

## 11. 첫 목표 (수직 슬라이스, 화면판)

Core는 이미 풍부하지만, **화면에서는 `docs/stage1-vertical-slice.md`의 DoD부터** 되살리는 걸 권한다: 직선 1경로 + 화살탑 + 보병 한 판이 끝까지 도는 것. 그게 되면 대포탑/빙결탑/독탑·스킬·멀티웨이브는 이미 Core에 있으니 **데이터(SO)와 뷰만 붙이면** 빠르게 확장된다.

> 순서: Task 0~3(Core 이식·검증) → Task 4~6(직선 한 판 렌더·플레이) → 거기서부터 콘텐츠/연출.
