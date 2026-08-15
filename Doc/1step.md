# 작업 요청: 1단계 프로토타입 코어 — 슬라임 이동 + 타워 타겟팅 + 발사체 + 오브젝트 풀링

## 프로젝트 컨텍스트
- Unity 6000.3.16f1 (URP 17.3), 3D 캐주얼 타워 디펜스 "슬라임 TD"
- DI: VContainer 1.17.0 / 이벤트: MessagePipe / 비동기: UniTask / Reactive: UniRx
- **네임스페이스 미사용** (전역), asmdef 없음(Assembly-CSharp), 필드 `_camelCase`, 한국어 주석, Allman 중괄호
- 개별 오브젝트는 `Update()` 직접 쓰지 않고 `UpdateSubscriptionService`(기존 코드, 건드리지 말 것)에 구독
- 씬 구조: `Bootstrap`(ProjectLifetimeScope, 상시 유지) → `Lobby`/`Game`(Additive) — 이미 구축 완료, 이번 작업은 `Game` 씬 안에서만 진행
- `Game` 씬에는 `GameSceneLifetimeScope`가 이미 있고 Parent가 `ProjectLifetimeScope`로 연결되어 있음 (빈 상태, 이번에 여기 등록 추가)
- 실제 캐릭터 리소스 아직 없음 — 슬라임/타워/발사체 전부 프리미티브(Cube/Sphere)로 대체, 나중에 아트 리소스로 교체 예정이니 비주얼과 로직 분리해서 작성할 것

## 이번 작업 목표 (1단계 체크포인트)
슬라임 1종 + 타워 1종으로 "쏘면 죽는다"가 동작해야 함.

## 확정된 설계 결정 (반드시 따를 것)

### 1. 슬라임 이동 — Unity Splines
- `SplineContainer` + `SplineAnimate` 사용 (이미 손으로 테스트해봄)
- `SplineAnimate.ObjectUpdateMethod = ObjectUpdateMethod.Speed` (Time 모드 아님 — 곡선 구간에서 속도 불균일해지는 문제 확인됨)
- `SplineAnimate.LoopMode = Once`
- 경로 끝 도달 감지는 `SplineAnimate.Completed` 이벤트 사용 (라이프 차감은 이번 작업 범위 아님 — 지금은 도달 시 그냥 풀로 반환만 하면 됨)

### 2. 타워 타겟팅 — Physics.OverlapSphere
- SlimeRegistry 같은 중앙 관리 서비스는 만들지 않는다. 처치 수 집계나 웨이브 클리어 판정 같은 건 나중에 MessagePipe 이벤트(`SlimeKilledEvent`) 구독 + 단순 카운터로 처리할 예정이라 이번 범위에서 제외.
- 타워는 일정 주기(공격속도)마다 `Physics.OverlapSphere(position, range, slimeLayerMask)`로 사거리 안 슬라임 콜라이더를 찾고, 그중 가장 가까운 대상 하나를 선택해서 발사.
- 슬라임 오브젝트에는 감지용 Collider(Trigger 아님, 일반 콜라이더도 무방) + 전용 LayerMask("Slime") 부여할 것.
- 타워의 공격 주기(쿨다운) 갱신도 `Update()` 대신 `UpdateSubscriptionService` 구독으로 처리.

### 3. 발사체 — Bloons TD 참고, 논호밍 직선 투사체
- 발사 시점에 타겟의 **현재 위치**를 향해 방향을 계산하고, 이후에는 타겟을 추적하지 않고 그 방향으로 직진한다 (호밍 금지 — Bloons TD 다트도 발사 시점 방향으로만 날아가고 이후 추적하지 않음, 도중에 다른 슬라임과 충돌하면 그걸 맞출 수 있는 구조를 그대로 따른다).
- 발사체 이동도 `UpdateSubscriptionService`에 구독해서 위치 갱신 (Rigidbody 물리 이동이 아니라 Transform 직접 이동, 고정 속도).
- 충돌 감지: 발사체에 작은 Trigger Collider, `OnTriggerEnter`로 슬라임 레이어와 충돌 시 데미지 적용 후 발사체는 풀로 반환.
- 사거리(타워 range) 또는 일정 수명(예: 3초) 초과 시에도 자동으로 풀 반환 (허공에 날아가다 안 사라지는 문제 방지).

### 4. 오브젝트 풀링 — 지금부터 적용
- `UnityEngine.Pool.ObjectPool<T>`를 감싼 공용 풀 서비스를 만든다 (슬라임 풀, 발사체 풀 각각 하나씩, 혹은 제네릭 하나로 둘 다 처리 — 구현 시 판단해서 진행하되 어떤 방식으로 했는지 완료 후 설명할 것)
- 풀 서비스도 `GameSceneLifetimeScope`에 DI 등록해서 슬라임 스포너/타워가 주입받아 쓰도록 구성
- 슬라임/발사체 프리팹은 `OnDisable` 시점에 내부 상태(체력, 이동 진행도 등) 초기화해서 재사용 시 이전 상태가 남지 않도록 처리

## MVP 패턴 적용 범위
- 지금 단계는 UI가 없으므로 Presenter/View 풀 세트를 억지로 만들지 않는다.
- 슬라임의 체력 관리는 `UniRx ReactiveProperty<int>`로 선언 (3단계 분열 로직에서 임계치 감지에 그대로 재사용할 예정이므로 미리 이 형태로 만들어둘 것). 0 이하가 되면:
  1. `SlimeKilledEvent`(MessagePipe) 발행
  2. 풀로 반환
- 타워의 사거리/공격력/공격속도 같은 스탯은 지금은 `[SerializeField]`로 Inspector 노출 (ScriptableObject 데이터화는 4단계 업그레이드 작업에서 진행 예정, 이번 범위 아님)

## 만들어야 할 것 (파일 목록, 네이밍은 자유롭게 판단하되 아래 역할은 모두 포함)
1. 슬라임 이동 컴포넌트 (SplineAnimate 래핑, Completed 시 풀 반환)
2. 슬라임 체력/상태 컴포넌트 (ReactiveProperty<int> Health, 데미지 적용 메서드, 0 이하 시 SlimeKilledEvent 발행 + 풀 반환)
3. 타워 컴포넌트 (OverlapSphere 타겟 탐색 + 쿨다운 관리, UpdateSubscriptionService 구독)
4. 발사체 컴포넌트 (직선 이동, Trigger 충돌 처리, 수명/사거리 초과 시 자동 반환)
5. 오브젝트 풀 서비스 (슬라임/발사체 풀링, GameSceneLifetimeScope에 DI 등록)
6. 테스트용 슬라임 스포너 (일정 주기로 슬라임 풀에서 꺼내 Spline 시작점에 배치 — 정식 웨이브 시스템은 2단계 작업이므로 지금은 최소한의 반복 스폰만)

## 제약 조건
- 네임스페이스 사용 금지 (전역 네임스페이스)
- 슬라임/타워/발사체 전부 프리미티브 Mesh(Cube/Sphere)로 테스트 가능하게, 비주얼 관련 코드와 로직 코드는 분리 (나중에 아트 리소스 교체 쉽게)
- 씬 이름 등 문자열은 기존 `SceneNames.cs` 컨벤션처럼 상수화할 부분 있으면 상수화
- 각 클래스 상단에 한국어로 역할 설명 주석

## 완료 후 알려줘야 할 것
- `Game` 씬에 실제로 어떤 GameObject를 만들고 어떤 컴포넌트를 붙여야 하는지, Inspector 필드 연결 순서 (Spline 경로 포함)
- 풀 서비스를 슬라임/발사체 각각 별도로 만들었는지, 제네릭 하나로 처리했는지와 그 이유
- 타워의 공격 주기 관리를 `UpdateSubscriptionService`에 어떻게 구독시켰는지 (인터페이스 시그니처가 어떻게 생겼길래 그렇게 구현했는지 — 이 서비스는 기존 코드라 실제 인터페이스를 보고 맞춰야 하니, 만약 시그니처를 확인할 수 없다면 가정한 인터페이스 형태를 알려줄 것)
- 체크포인트("쏘면 죽는다") 테스트 시 확인해야 할 순서