# Technical Environment: First Mobile Game (타워 디펜스)

## Project Technical Summary

- **Project Name**: First Mobile Game (타워 디펜스)
- **Project Type**: Greenfield
- **Primary Runtime Environment**: Local only (단일 안드로이드 기기, 오프라인)
- **Cloud Provider**: None (MVP 기준. 고도화 시 Phase 3에서 도입 예정)
- **Target Deployment Model**: N/A (모바일 앱 빌드 — MVP는 APK 직접 빌드, 이후 Google Play AAB)
- **One-line Summary**: Unity 6 기반의 안드로이드 단일 기기 2D 타워 디펜스 게임으로, 순수 C# 도메인 로직과 싱글톤·이벤트 기반 매니저 구조로 구성된다.

## Languages

### Required Languages

| Language | Version | Purpose | Rationale |
|----------|---------|---------|-----------|
| C# | 9.0 (.NET Standard 2.1) | 게임 전체 로직 및 Unity 스크립트 | Unity 6 LTS 기본 언어/런타임. 별도 설정 없이 자동 결정됨 |

### Prohibited Languages

| Language | Reason |
|----------|--------|
| 서버/백엔드 언어 (Node.js, Python 등) | 단일 기기 오프라인 게임이라 서버 코드가 불필요. 온라인 기능은 Phase 3로 분리 |
| 임베디드 스크립트 언어 (Lua 등) | 첫 프로젝트에 불필요한 복잡도. 모든 게임 규칙은 순수 C#로 작성 |

## Frameworks and Libraries

### Package Manager

- **UPM (Unity Package Manager)** — 모든 의존성 관리에 사용. `Packages/manifest.json`을 Git에 커밋해 버전을 고정한다.

### Required Frameworks and Libraries

| Framework/Library | Version | Domain | Rationale |
|-------------------|---------|--------|-----------|
| Unity | 6 LTS (6000.0 LTS) | 게임 엔진 | 최신 LTS로 장기 지원·안드로이드 빌드 안정성·풍부한 자료 |
| URP (Universal Render Pipeline) | Unity 6 동봉 버전 | 렌더링 | 가벼운 2D 게임에 적합, 모바일 성능 최적화 |
| uGUI (Unity UI, Canvas) | Unity 6 내장 | UI / HUD | 런타임 게임 UI 예제·자료가 가장 풍부, 첫 프로젝트에 안전 |
| DOTween | 무료(최신 안정) | 애니메이션 / 연출 | 타워 발사·UI 트윈을 코드 몇 줄로. 업계 표준 |
| JsonUtility | Unity 6 내장 | 세이브 직렬화 | 별도 설치 불필요, PlayerPrefs와 조합해 가벼운 저장 |
| ObjectPool&lt;T&gt; | Unity 6 내장 (`UnityEngine.Pool`) | 오브젝트 풀링 | 적·총알 대량 생성 시 성능 확보 |
| Unity Test Framework | Unity 6 내장 (NUnit 기반) | 테스트 | EditMode/PlayMode 표준 테스트 |

### Prohibited Libraries

| Library | Reason | Use Instead |
|---------|--------|-------------|
| ECS / DOTS | 첫 프로젝트엔 학습 곡선이 가파르고 과한 복잡도 | 기본 MonoBehaviour + ObjectPool&lt;T&gt; |
| 유료 세이브 플러그인 (Easy Save 등) | 이 규모엔 불필요한 비용·복잡도 | JsonUtility + PlayerPrefs |
| 멀티플레이/네트워킹 (Netcode for GameObjects 등) | 온라인 기능은 Phase 3, MVP는 단일 기기 | 없음 (로컬 전용) |

## Cloud Environment

이 프로젝트의 MVP는 **클라우드를 사용하지 않는다.** 게임은 단일 안드로이드 기기에서 완전히 오프라인으로 동작하며, 모든 데이터(스테이지 클리어 여부, 최고 별 개수)는 기기 로컬의 PlayerPrefs에 저장된다. 배포 산출물은 MVP 단계에서는 **APK 직접 빌드**로 기기에 설치하고, 이후 출시 단계에서 **Google Play 스토어용 AAB** 빌드로 전환한다. 랭킹·클라우드 세이브 등 클라우드 기반 기능은 비전 문서의 Phase 3로 분리되어 있으며, 그 도입 범위는 Open Questions에서 다룬다.

## Architecture and Patterns

### API Style

- **Style**: N/A (네트워크 API 없음 — 단일 기기 오프라인 게임)
- **Versioning**: N/A
- **Error Format**: N/A (도메인 오류는 C# 예외로 처리 — 아래 Example Code Patterns 참고)

### Data

- **Primary Data Store**: PlayerPrefs (로컬 키-값 저장 — 스테이지별 클리어 여부, 최고 별 개수)
- **Secondary Stores**: ScriptableObject (타워 8종·적 6종·스테이지 밸런싱 수치를 코드와 분리해 정의)
- **Data Ownership**: 세이브 데이터는 단일 기기 로컬 소유. 판 안의 골드·타워 배치 상태는 저장하지 않으며 한 판은 항상 처음부터 시작

### Messaging

- **Synchronous**: N/A (단일 프로세스 게임)
- **Asynchronous**: C# `event` 기반 인게임 이벤트 (예: `OnEnemyKilled`, `OnGoldChanged`). 외부 메시징 큐 없음

### Project Structure

- **Style**: Modular monolith (단일 Unity 프로젝트 내 매니저별 책임 분리)
- **Notes**: 싱글톤 매니저(`GameManager` / `WaveManager` / `EconomyManager` / `TowerManager`)가 핵심 흐름을 담당하고, 매니저 간 통신은 C# 이벤트로 느슨하게 연결한다. 게임 규칙은 MonoBehaviour가 아닌 순수 C# 클래스(`Game.Core`)로 분리해 EditMode 테스트가 가능하도록 한다. 씬은 메인메뉴 / 스테이지선택 / 게임플레이 3개로 구성

## Security Basics

- **Authentication**: 없음 (단일 사용자 로컬 게임, 로그인 불필요. 클라우드 도입 시 Phase 3에서 재검토)
- **Authorization**: N/A (MVP)
- **Encryption in Transit**: N/A (네트워크 통신 없음. Phase 3 클라우드 도입 시 TLS 1.2+ 적용 예정)
- **Encryption at Rest**: N/A (로컬 PlayerPrefs. 민감 정보를 저장하지 않음)
- **Input Validation**: 세이브 데이터 무결성 검증 — 로드 시 저장값(클리어 여부, 별 개수 0~3 범위)의 형식·범위를 검증해 손상·변조에 대비
- **Secrets Storage**: 없음 (MVP에 API 키 등 비밀값 없음. Phase 3 클라우드 도입 시 별도 시크릿 관리 도입). 소스·설정·빌드 환경변수에 비밀값을 두지 않는다
- **Security Compliance Framework**: OWASP MASVS (Mobile Application Security Verification Standard)
  - **Rationale**: 모바일 앱에 특화된 보안 표준. MVP는 로컬 데이터 보호(세이브 변조 방지) 수준만 적용하고, 클라우드 단계에서 본격 적용한다

## Testing

| Test Type | Required | Coverage Target | Tooling |
|-----------|----------|----------------|---------|
| Unit (EditMode) | Yes | 핵심 로직 클래스(경제·전투·웨이브·세이브) 80% line | Unity Test Framework (NUnit) |
| Integration (PlayMode) | Yes (핵심만) | 실제 씬에서 핵심 동작 검증(예: 타워가 적 실제 처치) — 수치 목표 없음, 주요 시나리오 위주 | Unity Test Framework (PlayMode) |

> UI·연출·MonoBehaviour는 커버리지 목표에서 제외한다. TDD가 성립하도록 게임 규칙은 순수 C# 클래스로 분리해 EditMode에서 빠르게 검증한다.

### Code Quality

- **Linter / Formatter**: `.editorconfig` + Roslyn 분석기 (C# 표준 — 4-space 들여쓰기, PascalCase 메서드)
- **Type Checker**: N/A (C#는 컴파일러가 정적 타입 검사 수행)

### CI Gates

| Pipeline Stage | Required Checks | Failure Action |
|---------------|----------------|----------------|
| Pre-commit | EditMode 단위 테스트 통과, `.editorconfig` 포맷 준수 | Block commit |
| Pull Request | N/A (MVP는 로컬 위주, 별도 CI 파이프라인 없음 — 향후 GameCI 도입 검토) | N/A |

## Example Code Patterns

이것이 표준 패턴이다. 코드 생성 시 다른 형태를 만들지 말고 이 형태를 따른다.

### Endpoint / Handler

게임에는 네트워크 엔드포인트가 없으므로, 그에 대응하는 표준 패턴으로 **싱글톤 매니저(MonoBehaviour)**를 제시한다. 입력(이벤트) 수신, 도메인 로직 위임, 결과 통지(이벤트), 오류 처리를 보여준다.

```csharp
// EconomyManager.cs — 적 처치 이벤트 구독, Economy 로직 위임
using System;
using UnityEngine;

namespace Game.Managers
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private int startingGold = 100;

        private Game.Core.Economy economy;

        public event Action<int> OnGoldChanged;  // 골드 변동 시 UI가 구독
        public int Gold => economy.Gold;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            economy = new Game.Core.Economy(startingGold);
        }

        private void OnEnable()  => Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
        private void OnDisable() => Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;

        private void HandleEnemyKilled(Enemy enemy)
        {
            economy.AddKillReward(enemy.GoldReward);
            OnGoldChanged?.Invoke(economy.Gold);
        }

        public bool TryPurchase(int cost)  // 구매 시도. 실패는 예외 대신 false로
        {
            try
            {
                economy.Spend(cost);
                OnGoldChanged?.Invoke(economy.Gold);
                return true;
            }
            catch (Game.Core.InsufficientGoldException)
            {
                return false;
            }
        }
    }
}
```

### Business Logic Function

```csharp
// Economy.cs — 순수 도메인 로직. Unity 실행 없이 테스트 가능
using System;

namespace Game.Core
{
    public class InsufficientGoldException : Exception
    {
        public InsufficientGoldException(int required, int available)
            : base($"골드 부족: 필요 {required}, 보유 {available}") { }
    }

    public class Economy
    {
        public int Gold { get; private set; }

        public Economy(int startingGold)
        {
            if (startingGold < 0)
                throw new ArgumentOutOfRangeException(nameof(startingGold), "시작 골드는 음수일 수 없습니다.");
            Gold = startingGold;
        }

        public void AddKillReward(int reward)  // 적 처치 보상 적립
        {
            if (reward < 0)
                throw new ArgumentOutOfRangeException(nameof(reward), "보상은 음수일 수 없습니다.");
            Gold += reward;
        }

        public void Spend(int cost)  // 타워 구매. 골드 부족 시 도메인 예외
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "비용은 음수일 수 없습니다.");
            if (cost > Gold)
                throw new InsufficientGoldException(cost, Gold);
            Gold -= cost;
        }
    }
}
```

### Test

```csharp
// EconomyTests.cs — EditMode 단위 테스트(NUnit), Unity 실행 불필요. Arrange-Act-Assert
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class EconomyTests
    {
        [Test]
        public void AddKillReward_적처치시_골드가_보상만큼_증가한다()
        {
            var economy = new Economy(startingGold: 100);   // Arrange
            economy.AddKillReward(25);                       // Act
            Assert.AreEqual(125, economy.Gold);             // Assert
        }

        [Test]
        public void Spend_골드부족시_예외를_던지고_잔액은_그대로다()
        {
            var economy = new Economy(startingGold: 30);
            Assert.Throws<InsufficientGoldException>(() => economy.Spend(50));
            Assert.AreEqual(30, economy.Gold);  // 실패한 구매는 잔액 불변
        }
    }
}
```

## Open Questions

- **성능 목표 미정**: 목표 프레임레이트(예: 60fps)와 동시 등장 적/총알의 최대 수가 정해지지 않았다. 이는 ObjectPool 초기 크기 산정과 후반 스테이지(군집 적) 부하 대응에 직접 영향을 준다.
- **화면 비율 대응 미정**: 다양한 안드로이드 화면 비율(16:9, 19.5:9, 20:9 등)에서 UI와 플레이 영역을 어떻게 맞출지(Canvas Scaler 설정, 안전 영역 처리 등)가 정해지지 않았다.
