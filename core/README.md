# Game.Core — 순수 C# 게임 로직 (Unity 비의존)

이 폴더는 Unity 설치 전에 **게임 규칙을 TDD로 먼저 완성**하기 위한 standalone .NET 프로젝트다.
여기 있는 `Game.Core`의 모든 `.cs`는 Unity의 어떤 타입도 참조하지 않으므로, 나중에 Unity 프로젝트의
`Assets/Scripts/Core/`로 **그대로 복사**해 쓸 수 있다.

설계 근거: `docs/stage1-vertical-slice.md`

## 구성

```
core/
├── Game.Core/          순수 C# 라이브러리 (netstandard2.1 — Unity와 동일 타깃)
│   ├── Vec2.cs                좌표·거리 (Unity Vector 대체)
│   ├── Economy.cs             골드
│   ├── LifeSystem.cs          라이프
│   ├── EnemyUnit.cs           적 체력·스탯·상태이상(둔화/지속피해)·보스플래그 (+ EnemySpec)
│   ├── TowerUnit.cs           타워 발사 쿨다운·수치·업그레이드·범위반경·부가효과·체인(+ ChainSpec)
│   ├── TowerEffect.cs         명중 시 부가효과(둔화/지속피해/처치보너스골드) 정의
│   ├── PathTracker.cs         경로 전진·도달
│   ├── TargetSelector.cs      타깃 선정 (Select / SelectCandidate) + 타게팅 모드(선두/가까운/강한)
│   ├── SplashResolver.cs      범위 피해(대포탑·운석) 반경 내 다중 적 산출
│   ├── ChainResolver.cs       체인 피해(번개탑) 인접 적으로 순차 점프 + 데미지 감쇠
│   ├── WaveSchedule.cs        스폰 스케줄 (+ SpawnEntry, 미리보기 PeekRemaining)
│   ├── EnemyCatalog.cs        적 6종 스탯 정의(보병·돌격병·군집벌레·중장갑병·질주기병·미니보스)
│   ├── TowerCatalog.cs        타워 8종 스탯·효과·비용 정의(+ TowerSpec, CreateUnit)
│   ├── Stage.cs               여러 WaveSchedule의 시퀀스(멀티 웨이브)
│   ├── StageCatalog.cs        스테이지 1~10 데이터(경로·웨이브) + StageDefinition
│   ├── StageProgress.cs       스테이지 해금 규칙(SaveStore 기반, 이전 클리어 시 다음 해금)
│   ├── WaveStartTimer.cs      수동 웨이브 시작 + 조기 시작 보너스
│   ├── Cooldown.cs            스킬 쿨다운 타이머
│   ├── SkillSettings.cs       스킬 3종 쿨다운·효과 수치(vision 기본값)
│   ├── StarRating.cs          남은 라이프 → 별점(1~3)
│   ├── SaveStore.cs           스테이지 클리어·최고별 저장(PlayerPrefs 비의존, 로드 검증)
│   ├── ViewSnapshots.cs       화면 렌더용 읽기전용 스냅샷(EnemyView: 보스 / TowerView: 체인·타게팅 포함)
│   ├── GamePhase.cs           Ready→Playing→Won|Lost 상태기
│   └── GameLoop.cs            위를 묶은 한 판 시뮬레이션 오케스트레이터
└── Game.Core.Tests/    NUnit 테스트 (net10.0, NUnit 3.x — Unity Test Framework와 동일 계열)
```

추가 기능(vision.md를 Core 로직으로 선구현):
- 타워 판매/환불, 타워 업그레이드, 웨이브 미리보기
- **상성 3축**(통합 테스트로 카운터 관계 증명):
  - 광역(대포탑) vs 무리(군집 벌레)
  - 둔화(빙결탑) → 빠른 적(돌격병)을 묶어 화살탑이 더 잡게
  - 지속피해(독탑) → 튼튼한 적(중장갑병)을 저격탑과 함께 깎아냄
- **멀티 웨이브**: `Stage`로 여러 웨이브 순차 진행, 마지막 웨이브 클리어 시 승리
- **조기 웨이브 보너스**: 웨이브 사이 일찍 시작하면 남은 시간만큼 보너스 골드
- **스킬 3종**: 운석 낙하(즉시 광역) / 시간 정지(적 정지) / 골드 러시(처치 골드 배수), 쿨다운 기반
- **세이브**: 스테이지 클리어·별점 저장(순수 로직, 직렬화 문자열을 Unity가 PlayerPrefs로 영속화)
- **번개탑(체인 타격)**: 한 발이 인접 적으로 순차 점프하며 점프마다 데미지 감쇠(일렬 무리 적 카운터)
- **골드탑(처치 보너스)**: 막타로 적을 죽이면 처치 보상에 더해 추가 골드 지급(경제 전략)
- **적 6종 + 보스**: `EnemyCatalog`로 위협 유형별 스탯 정의, 미니 보스는 `BossActive`로 등장 감지
- **타워 8종**: `TowerCatalog`로 8종 스탯·효과·비용을 데이터화(딜러/광역/유틸, `CreateUnit`로 전투 인스턴스)
- **타게팅 모드**: 타워별 조준 정책(선두/가장가까운/가장강한)을 `SetTowerTargetingAt`로 전환
- **멀티 스테이지 데이터**: `StageCatalog`로 스테이지 1~10 경로·웨이브를 데이터화(`ToConfig`로 바로 플레이)
- **스테이지 진행/해금**: `StageProgress`가 이전 스테이지 클리어 시 다음을 해금(스테이지 선택 화면용)

## 테스트 실행

```bash
cd core
dotnet test
```

현재 188개 테스트 통과(슬라이스 코어 + 판매·업그레이드·미리보기·보너스 + 상성 3축 + 멀티웨이브 + 스킬 3종 + 세이브 + 뷰 스냅샷(보스·체인·타게팅 포함) + 번개탑·골드탑 + 적 6종·보스 + 타워 8종 + 타게팅 모드 + 스테이지 1~10 데이터·해금 + 카탈로그 정합성 E2E).

## Unity로 이식할 때

1. Unity 6 LTS 프로젝트 생성 후 `Assets/Scripts/Core/` 폴더 생성.
2. `Game.Core/*.cs`를 그 폴더로 복사(코드 수정 불필요).
3. `Game.Core.Tests/*.cs`는 Unity의 EditMode 테스트 폴더(`Assets/Tests/EditMode/`)로 복사.
   - Unity Test Framework도 NUnit 3.x 기반이라 `Assert.AreEqual` 등이 그대로 동작한다.
4. 이후 `docs/stage1-vertical-slice.md` §5·§6대로 MonoBehaviour 매니저/뷰와 씬을 얹는다.
   - MonoBehaviour는 입력·타이밍을 받아 이 Core에 위임하는 얇은 껍데기로만 작성한다.
