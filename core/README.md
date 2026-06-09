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
│   ├── EnemyUnit.cs           적 체력·스탯·상태이상(둔화/지속피해) (+ EnemySpec)
│   ├── TowerUnit.cs           타워 발사 쿨다운·수치·업그레이드·범위반경·부가효과
│   ├── TowerEffect.cs         명중 시 부가효과(둔화/지속피해) 정의
│   ├── PathTracker.cs         경로 전진·도달
│   ├── TargetSelector.cs      타깃 선정 (Select / SelectCandidate)
│   ├── SplashResolver.cs      범위 피해(대포탑·운석) 반경 내 다중 적 산출
│   ├── WaveSchedule.cs        스폰 스케줄 (+ SpawnEntry, 미리보기 PeekRemaining)
│   ├── Stage.cs               여러 WaveSchedule의 시퀀스(멀티 웨이브)
│   ├── WaveStartTimer.cs      수동 웨이브 시작 + 조기 시작 보너스
│   ├── Cooldown.cs            스킬 쿨다운 타이머
│   ├── SkillSettings.cs       스킬 3종 쿨다운·효과 수치(vision 기본값)
│   ├── StarRating.cs          남은 라이프 → 별점(1~3)
│   ├── SaveStore.cs           스테이지 클리어·최고별 저장(PlayerPrefs 비의존, 로드 검증)
│   ├── ViewSnapshots.cs       화면 렌더용 읽기전용 스냅샷(EnemyView/TowerView)
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

## 테스트 실행

```bash
cd core
dotnet test
```

현재 127개 테스트 통과(슬라이스 코어 + 판매·업그레이드·미리보기·보너스 + 상성 3축 + 멀티웨이브 + 스킬 3종 + 세이브 + 뷰 스냅샷).

## Unity로 이식할 때

1. Unity 6 LTS 프로젝트 생성 후 `Assets/Scripts/Core/` 폴더 생성.
2. `Game.Core/*.cs`를 그 폴더로 복사(코드 수정 불필요).
3. `Game.Core.Tests/*.cs`는 Unity의 EditMode 테스트 폴더(`Assets/Tests/EditMode/`)로 복사.
   - Unity Test Framework도 NUnit 3.x 기반이라 `Assert.AreEqual` 등이 그대로 동작한다.
4. 이후 `docs/stage1-vertical-slice.md` §5·§6대로 MonoBehaviour 매니저/뷰와 씬을 얹는다.
   - MonoBehaviour는 입력·타이밍을 받아 이 Core에 위임하는 얇은 껍데기로만 작성한다.
