# SlimeTD

3D 캐주얼 타워 디펜스. Bloons TD 구조를 차용하되, "풍선이 터지는" 대신 **"슬라임이 말랑하게 튕기는"** 손맛을 핵심 차별점으로 삼는다.

- 기획서: [Doc/슬라임TD_게임기획서.md](Doc/슬라임TD_게임기획서.md)
- 타워 로스터: [Doc/슬라임TD_타워로스터.md](Doc/슬라임TD_타워로스터.md)
- 타워 스킬 설계: [Doc/슬라임TD_타워스킬설계.md](Doc/슬라임TD_타워스킬설계.md)
- 데이터 구조: [Doc/슬라임TD_데이터구조설계.md](Doc/슬라임TD_데이터구조설계.md)
- 웨이브 기획: [Doc/슬라임TD_웨이브기획.md](Doc/슬라임TD_웨이브기획.md)
- 웨이브 데이터: [Doc/슬라임TD_웨이브데이터구조.md](Doc/슬라임TD_웨이브데이터구조.md)
- 슬라임 렌더링: [Doc/슬라임TD_슬라임렌더링.md](Doc/슬라임TD_슬라임렌더링.md)
- 로비 UI: [Doc/슬라임TD_로비UI.md](Doc/슬라임TD_로비UI.md)
- 팝업 시스템: [Doc/슬라임TD_팝업시스템.md](Doc/슬라임TD_팝업시스템.md)

**화면**: 모바일 **세로(Portrait) 고정**. UI/카메라/맵 관련 작업은 항상 세로 비율을 기준으로 한다. 자세한 내용은 아래 [5. 세로 화면(Portrait) 기준](#5-세로-화면portrait-기준) 참고.

---

## 1. 기술 스택

| 역할 | 사용 기술 |
|---|---|
| 엔진 | Unity 6000.3.6f1 (URP 17.3) |
| DI | VContainer 1.17.0 |
| UI 패턴 | MVP (Model = Service, View, Presenter) |
| 이벤트 | MessagePipe (+ VContainer Integration) |
| 비동기 | UniTask 2.1.0 |
| Reactive | UniRx (`ReactiveProperty`, `CompositeDisposable`) |
| 프레임 업데이트 | UpdateSubscriptionService (중앙 집중 Update 관리) |
| 경로 이동 | Unity Splines (곡선 경로 생성/이동) |
| 트윈/연출 | DOTween (피격 반응, 처치 연출 등 "손맛" 구현) |
| Safe Area | NotchSolution (노치/펀치홀 기기 대응) |

Unity Object Pool과 ScriptableObject는 엔진 기본 기능이라 스택으로 명시하지 않고 설계 패턴으로만 활용한다.

---

## 2. 아키텍처

### 씬 구조 (Additive 전용)

Bootstrap이 상주하고 그 위에 Lobby / Game을 Additive로 얹는다. **`LoadSceneMode.Single`은 사용하지 않는다** — Bootstrap이 언로드되면 `ProjectLifetimeScope`가 함께 파괴되어 전역 DI가 끊긴다.

```
Bootstrap (상주, ProjectLifetimeScope + UpdateSubscriptionService)
   └─ LobbyScene / GameScene (Additive, 각자 LifetimeScope 보유)
```

씬 전환은 [SceneLoader.cs](Assets/Scripts/Services/SceneService/SceneLoader.cs)의 `TransitionAsync`만 사용한다. 로드 → 언로드 순서를 지키는 이유는 활성 씬을 먼저 언로드하면 Unity가 임의 씬을 활성 씬으로 잡기 때문이다. 씬 이름은 [SceneNames.cs](Assets/Scripts/Constants/SceneNames.cs) 상수를 쓰고 문자열 리터럴을 직접 쓰지 않는다.

### DI (VContainer)

- **`ProjectLifetimeScope`**: 전역 싱글턴 서비스 (`ISceneLoader`, `IUpdateSubscriptionService` 등)
- **`LobbyLifetimeScope` / `GameLifetimeScope`**: 씬 단위 서비스와 씬 내 컴포넌트

VContainer는 씬 전체를 자동 주입하지 않는다. `[Inject]`를 받는 MonoBehaviour는 해당 씬 LifetimeScope에 `RegisterComponentInHierarchy<T>()`로 반드시 등록한다. 주입은 필드 어트리뷰트가 아니라 `[Inject] public void Construct(...)` 메서드 주입을 쓴다.

팝업 Presenter는 `PopupService`가 만드는 자식 스코프에서 해결된다. Presenter가 읽는 씬 서비스는 `Lifetime.Scoped`가 아니라 `Lifetime.Singleton`으로 등록한다. Scoped면 자식 스코프가 별도 인스턴스를 새로 만들어 게임과 다른 빈 상태를 읽는다 (`GameplayService`가 그랬다).

### MVP

| 계층 | 역할 |
|---|---|
| Service (Model) | 게임 로직/상태. MonoBehaviour 아님. 상태는 `ReactiveProperty`로 노출 |
| Presenter | Service ↔ View 중재. Service 구독 → View 갱신, View 입력 → Service 호출 |
| View (MonoBehaviour) | 표현만 담당. 게임 로직을 갖지 않는다 |

UI가 작을 때는 View 하나로 시작하고, 커지면 3계층으로 승격한다 (예: [LobbyHeader.cs](Assets/Scripts/Lobby/LobbyHeader.cs)).

### 이벤트 (MessagePipe)

씬/시스템 경계를 넘는 통지는 MessagePipe로 발행한다. 네이밍은 `<주어><과거분사>Event` (`GameProgressEvent`, `SlimeKilledEvent`, `SlimeReachedEndEvent`).

단일 객체 내부 상태 변화는 이벤트 대신 `ReactiveProperty`를 쓴다. 이벤트는 "다른 시스템이 알아야 하는 사실"에만 쓴다.

### 프레임 업데이트

개별 MonoBehaviour에 `Update()`를 두지 않는다. [IUpdateSubscriptionService](Assets/Scripts/Services/UpdateService/IUpdateSubscriptionService.cs)에 `IUpdatable` / `ILateUpdatable` / `IFixedUpdatable` / `IPeriodicUpdatable`을 구독한다.

- 매 프레임 필요 없는 로직(타워 타겟 재탐색 등)은 `RegisterPeriodicUpdatable(this, interval)`로 주기 실행한다
- 등록한 곳에서 반드시 해제한다 (`OnDestroy` / `Dispose`)
- Update 목록은 역순 순회 + 인덱스 보정으로 순회 중 등록/해제가 안전하다. 이 인덱스 보정 로직은 건드리지 않는다

### 비동기 (UniTask)

씬 전환, 웨이브 시작 딜레이, 리소스 로딩에 사용한다. 코루틴은 쓰지 않는다.

`CancellationToken`은 취소 주체와 수명이 일치할 때만 붙인다. 씬 전환처럼 **호출자 자신이 파괴되는** 작업에 `GetCancellationTokenOnDestroy()`를 붙이면 자기 전환을 스스로 취소한다.

### 구독 해제

`ReactiveProperty` / MessagePipe 구독은 `CompositeDisposable`에 담고 `OnDestroy`(View) 또는 `Dispose`(Service)에서 해제한다. 해제 누락은 씬 전환 시 메모리 릭과 중복 이벤트의 주된 원인이다.

---

## 3. 코딩 컨벤션

- private 필드: `_camelCase`, `[SerializeField] private`로 인스펙터 노출 (public 필드 금지)
- 인터페이스는 `I` 접두사, 구현체와 파일 분리
- 오브젝트, 프리팹, 에셋 파일 이름은 영문으로 짓는다 (화면에 보이는 텍스트만 한글). 에디터 빌더로 만드는 UI 계층도 같다
- 로그 태그: `[클래스명] 메시지` (예: `Debug.Log($"[SceneLoader] 씬 로드 실패: {sceneName}")`)
- MonoBehaviour 참조는 `Start()`에서 null 체크 후 조기 반환하고, 실패 시 `Debug.Log(..., this)`로 대상 오브젝트를 함께 넘긴다
- 주석과 로그 메시지는 한국어로 작성한다

### 주석

**주석은 최소화한다. 꼭 필요하고 중요도가 높은 내용만 남긴다.**

- 남길 것: 함정, 순서 의존성, 엔진 동작 회피책 등 코드만으로 이해하기 어려운 핵심 정보
  - 예: `// 활성 씬을 먼저 언로드하면 Unity가 임의의 씬을 활성 씬으로 잡는다`
- 남기지 않을 것: 코드를 그대로 읽는 주석, "왜" 설명, 클래스 헤더, 파라미터 나열, 구획 장식용 주석

미완성 지점은 `// TODO:` 한 줄로 남긴다.

---

## 4. 구현 시 주의점

- **슬라임 이동**: `SplineContainer` + `SplineAnimate`로 경로를 따르고, 갱신은 UpdateSubscriptionService에 붙인다. 타겟 탐색 로직은 Service 계층에 두고 View는 표현만 한다
- **오브젝트 풀링**: 슬라임/발사체는 스폰 빈도가 높다. Unity `ObjectPool<T>`를 쓰고, 반환 시 `ReactiveProperty`와 구독 상태를 반드시 초기화한다
- **밸런스 수치**: 체력/데미지/젤리/라이프 차감량은 ScriptableObject로 분리해 코드에 하드코딩하지 않는다
- **타워 데이터 파이프라인**: 타워 숫자는 Assets/Datas/Sheets/의 CSV(Grades.csv, Towers.csv)가 원천이다. 메뉴 SlimeTD > Data > Import Tower Sheets가 TowerData 에셋과 TowerCatalog를 채우며, 에셋의 숫자 필드를 인스펙터에서 직접 고치지 않는다. 프리팹/투사체/썸네일 연결은 인스펙터에서 한다. 타워 스킬은 Skills.csv 한 파일이 원천이고 메뉴 SlimeTD > Data > Import Skill Sheets가 SkillCatalog 에셋 하나에 스킬 데이터를 직접 채운다 (스킬별 에셋 파일은 없다. 스킬은 타워 한 개에 귀속되고 TowerData는 스킬을 모른다. 타워 생성 시 SkillCatalog.GetSkills(towerId)로 찾는다). 상세는 [Doc/슬라임TD_데이터구조설계.md](Doc/슬라임TD_데이터구조설계.md) 참고
- **로비 웨이브 맵**: 전투 탭은 맵 선택 대신 세로 스크롤 웨이브 진행도 맵이다. 프리팹은 메뉴 `SlimeTD > Wave > Build Wave Map Forest`(`WaveMapBuilder`)가 만들며 실행마다 소품 배치가 바뀌고 `WaveForest.prefab`을 덮어쓴다. 상세는 [Doc/슬라임TD_로비UI.md](Doc/슬라임TD_로비UI.md) 참고
- **데이터 ID 규칙**: 모든 데이터(타워, 스킬 등)의 ID는 종류와 상관없이 서로 겹치지 않아야 한다. 새 시트나 행을 추가할 때 기존 시트의 ID와 비교해 중복을 확인한다. 대역: 타워 101~(3자리), 스킬 타워ID×100+순번(5자리. 궁수 10101~, 일병 10301~). 한 번 정한 ID는 바꾸지 않고, 추가는 그 타워의 다음 빈 번호를 쓴다. 자릿수가 다르면 구조적으로 겹치지 않으니 새 종류의 데이터는 자릿수가 다른 대역을 잡는다
- **데이터 스크립트 위치**: ScriptableObject 정의는 Assets/Scripts/Data/에 둔다 (Game/ 아래에 두지 않는다)
- **플레이어 데이터(메타)**: 골드/보석/헌터 카드/헌터 레벨/최고 웨이브는 IPlayerDataService(전역)가 들고 IPlayerDataStore로 저장한다. 최고 웨이브는 GameplayService가 `ReportWaveReached`로 기록하고 로비 웨이브 맵이 구독한다. 저장 위치는 구현체 교체로 서버 전환이 가능해야 한다. 로컬 저장을 지워 새 계정으로 시작하려면 메뉴 `SlimeTD > Data > Reset Player Data`를 쓴다. 타워 레벨은 로비에서 정하고 인게임에서는 오르지 않는다
- **"손맛"이 이 프로젝트의 최우선 품질 기준**이다. 피격/처치 연출은 비용을 아끼지 않는다
- **그리드 배치**: 맵은 `GridMapData`(ScriptableObject)로 셀 단위 관리한다. 배치 가능/불가/경로 상태를 셀마다 지정하고, 에디터 도구(`GridMapDataInspector`, `GridSceneViewDrawer`, `GridPaintMode`)로 편집한다
- **슬라임 렌더 순서**: `SlimeRenderOrderService`가 슬라임 간 깊이(Z-order)를 관리한다. 상세 설계는 [Doc/슬라임TD_슬라임렌더링.md](Doc/슬라임TD_슬라임렌더링.md) 참고

---

## 5. 세로 화면(Portrait) 기준

이 게임은 **모바일 세로 고정**이다. 레퍼런스인 Bloons TD는 가로 화면이라, 맵/UI 레이아웃은 그대로 가져올 수 없고 세로에 맞게 재설계해야 한다.

### 기준 해상도

- 기준 해상도: **1080 x 1920** (9:16)
- 대응 범위: 9:16 ~ 9:21 (노치/펀치홀 기기 포함). 화면이 길어질수록 세로 여백이 늘어나는 방향으로 처리한다

### CanvasScaler

모든 Canvas는 아래 설정으로 통일한다.

| 항목 | 값 |
|---|---|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1080 x 1920 |
| Screen Match Mode | Match Width Or Height |
| Match | **0 (Width 기준)** |

Match를 0으로 두는 이유는 세로 게임에서 **가로 폭이 UI 레이아웃의 제약**이기 때문이다. 높이에 맞추면 긴 화면 기기에서 UI가 가로로 잘린다.

### 레이아웃 원칙

세로는 가로 폭이 좁고 세로가 길다. 이 특성에 맞춰 화면을 세 영역으로 나눈다.

```
┌─────────────────┐
│  상단 HUD       │  젤리 / 라이프 / 웨이브 (얇게)
├─────────────────┤
│                 │
│   맵 / 플레이   │  세로로 긴 경로, 화면 대부분 차지
│                 │
├─────────────────┤
│  하단 조작 UI   │  타워 선택 / 배치 / 업그레이드 (엄지 도달 범위)
└─────────────────┘
```

- **하단 조작 UI**: 타워 선택·배치·업그레이드 등 자주 쓰는 조작은 화면 하단에 둔다 (한 손 엄지 도달 범위)
- **상단 HUD**: 젤리/라이프/웨이브 표시는 상단에 얇게 배치하고, 조작 요소를 두지 않는다
- **타워 업그레이드 패널**: 가로 화면처럼 옆에 띄울 공간이 없다. 하단에서 올라오는 **바텀시트** 형태로 처리한다
- **Safe Area**: 노치/홈 인디케이터 대응을 위해 상·하단 UI 루트에 Safe Area 적용을 전제로 설계한다

### 카메라 / 맵

- 맵 경로(Spline)는 **세로로 길게** 설계한다. 가로로 넓은 경로는 세로 화면에서 잘리거나 과도하게 축소된다
- 카메라는 세로 비율 기준으로 프레이밍한다. 가로 FOV/orthographic size를 기준으로 잡으면 세로 화면에서 맵이 화면을 못 채운다
- 맵은 한 화면에 다 들어오는 것을 기본으로 하되, 스크롤/줌이 필요하면 **세로 스크롤**만 허용한다

### 화면 방향 설정

`ProjectSettings > Player > Resolution and Presentation`의 Default Orientation은 **Portrait**(`defaultScreenOrientation: 0`)로 고정했고, 가로·거꾸로 세로 회전 허용 플래그는 껐다.

---

## 6. 정리 대상

- [SlimeMover.cs](Assets/Scripts/SlimeMover.cs), `SampleScene.unity`는 프로토타입 잔재다. 정식 구현으로 대체되면 삭제한다
- 어셈블리 정의(asmdef)가 아직 없다. 스크립트가 늘어나면 컴파일 시간을 위해 도입을 고려한다

---

## 7. Claude 응답 방식

- 구조/설계를 논의할 때는 코드를 바로 작성하지 않는다. 역할 분리, 클래스/필드 구성, 데이터 흐름까지만 텍스트로 제시한다
- 실제 코드는 사용자가 명시적으로 요청하거나, 구현 단계로 넘어가 실제로 필요한 시점에만 작성한다
