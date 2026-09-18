# 슬라임 TD 데이터 구조 설계

모든 밸런스 수치는 ScriptableObject로 분리한다. 아래는 `Assets/Scripts/Game/Data/` 기준 실제 구조다.

## 게임 전역

```
GameConfig (SO, 1개)
├─ startingLife      : int = 20
└─ startingGold      : int = 0
```

## 맵 / 그리드

```
GridMapData (SO, 스테이지당 1개)
├─ centerPosition    : Vector3                  // 맵 중심. 변경 시 origin 재계산
├─ origin            : Vector3                  // 좌하단 기준점 (centerPosition에서 파생)
├─ width             : int = 8
├─ height            : int = 15
└─ cellStates        : GridCellState[]          // HideInInspector, 1차원 배열 (y * width + x)

GridCellState (enum)
├─ Placeable = 0                                // 타워 배치 가능
├─ Blocked   = 1                                // 배치 불가
└─ Path      = 2                                // 슬라임 이동 경로
```

셀 크기는 `1f` 고정(`FixedCellSize`)이다. `width`/`height` 변경 시 `ResizePreservingCells`가 기존 셀 상태를 최대한 보존한다.

## 웨이브 / 슬라임

[슬라임TD_웨이브데이터구조.md](슬라임TD_웨이브데이터구조.md) 참고.

```
SlimeRenderGroup (enum)
├─ Normal = 0
└─ Boss   = 1
```

## 타워 / 공격

```
TowerSpawnConfig (SO, 1개)
├─ towerPool         : TowerData[]              // 뽑기 대상
├─ cost              : int = 50
└─ ignoreGoldCost    : bool = true              // 개발용 무료 배치

TowerData (SO, 종류당 1개)
├─ towerName         : string
├─ prefab            : GameObject
├─ attackRange       : float = 5                // 탐지 범위 (발사체 도달거리와 별개)
└─ basicAttack       : AttackBehaviourData

AttackBehaviourData (abstract SO)
├─ damage            : int = 1
├─ cooldown          : float = 1                // 공격 종료 후부터 흐른다
├─ chargeState       : string                   // Animator 트리거 이름
├─ attackStates      : string[]                 // 순환 사용, Animator 트리거 이름
├─ chargeDuration    : float = 0                // 0이면 차징 생략
├─ attackDuration    : float = 0.5
└─ CreateBehaviour() : IAttackBehaviour         // 런타임 인스턴스 생성

MeleeAttackData (AttackBehaviourData 상속)
└─ (추가 필드 없음) -> MeleeAttack

ProjectileAttackData (AttackBehaviourData 상속)
├─ projectilePrefab  : GameObject
├─ poolCapacity      : int = 10
├─ poolMaxSize       : int = 50
└─ -> ProjectileAttack
```

`attackStates`/`chargeState`의 문자열은 Animator Controller의 **Trigger 파라미터 이름**과 일치해야 한다. 없는 이름이면 조용히 무시되고 애니메이션이 재생되지 않는다.

`AttackBehaviourData`는 여러 타워가 공유하는 에셋이므로 `attackStates` 순환 인덱스를 SO가 갖지 않는다. 인덱스는 `CreateBehaviour()`로 만든 런타임 인스턴스가 보유한다.

## 런타임 상태

SO는 기준값만 담고, 변화하는 상태는 아래 클래스가 `ReactiveProperty`로 노출한다.

```
TowerStats
└─ AttackRange       : ReactiveProperty<float>  // TowerRangeIndicator가 구독. 인스턴스 교체 금지

SlimeStats
├─ CurrentHealth     : ReactiveProperty<int>
└─ IsDead            : bool                     // CurrentHealth <= 0

TargetInfo (readonly struct)
├─ Transform         : Transform
├─ Slime             : ISlime
├─ SqrDistance       : float
└─ IsValid           : bool                     // null/비활성/사망 여부. 풀 반환 판별에 사용
```

`SlimeStats.Reset(0)`이 풀 반환 시 호출되므로, `IsDead`는 "죽었거나 풀에 반환된 상태"를 뜻한다. `TargetInfo.IsValid`가 이를 검사해 이미 처치된 슬라임에 데미지가 들어가는 것을 막는다.
