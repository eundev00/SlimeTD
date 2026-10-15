# 슬라임 TD 데이터 구조 설계

모든 밸런스 수치는 ScriptableObject로 분리한다. 아래는 `Assets/Scripts/Data/` 기준 실제 구조다.

## 게임 전역

```
GameConfig (SO, 1개)
├─ startingLife      : int = 20                 // 에셋 값 10
├─ startingJelly     : int = 0                  // 에셋 값 50 (인게임 재화)
├─ summonBaseCost    : int = 50                 // 에셋 값 10. 소환 비용 = 기본 + 증가 × 소환 횟수
├─ summonCostIncrease: int = 10                 // 에셋 값 2
└─ summonGradeId     : int = 1                  // 소환 등급 (일반)
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
TowerCatalog (SO, 1개)
└─ towers            : TowerData[]              // 전체 타워 목록. 임포터가 시트 기준으로 채움
   런타임 인덱스: ID -> TowerData, 등급 -> 뽑기 후보(활성 + 프리팹 있음)
   TryDraw(등급), TryDrawNextGrade(소스) — 소환과 합성 결과에 사용

TowerData (SO, 종류당 1개)
├─ id                : int                      // 시트 ID. 등급 대역을 가짐
├─ gradeId           : int                      // 시트 등급. 등급의 유일한 원천
├─ enabled           : bool                     // false면 뽑기 후보에서 제외
├─ towerName         : string
├─ prefab            : GameObject
├─ attackRange       : float = 5                // 탐지 범위 (발사체 도달거리와 별개)
├─ attackType        : Melee / Projectile       // 근접 / 원거리(투사체)
└─ projectilePrefab  : GameObject               // Projectile일 때. 풀 크기는 코드 상수

AttackBehaviourFactory.Create(towerData) -> MeleeAttack / ProjectileAttack
```

공격 애니메이션은 모든 타워가 Animator의 `Attack` 트리거 하나를 쓴다(`AttackBehaviourBase`의 상수). 타워 프리팹의 Animator Controller에 `Attack` Trigger 파라미터가 없으면 조용히 무시되고 애니메이션이 재생되지 않는다.

## 타워 스킬 (설계안, 미구현)

스킬은 타워에 귀속되고, 발동 조건/확률/쿨타임/효과 종류 같은 부품은 공통으로 쓴다. 규칙과 용어는 [슬라임TD_타워스킬설계.md](슬라임TD_타워스킬설계.md) 참고.

```
TowerData (확장)
├─ id                : int                        // 등급 대역을 가진 고유 ID (예: 101, 202)
├─ basicAttack       : AttackBehaviourData        // 연출/발사체 참조만
├─ baseAttack        : float                      // 등급 시트의 기준공격력 (임포터가 채움)
├─ attackGrowthRate  : float                      // 레벨당 증가율(%) (임포터가 채움)
├─ attackCooldown    : float
├─ critChance        : float = 10                 // %
├─ critBonus         : float = 100                // %
└─ skills            : TowerSkillEntry[]          // 개수 제한 없음

공격력(레벨) = baseAttack × (1 + attackGrowthRate / 100 × (레벨 - 1))
공격속도는 레벨로 오르지 않는다. 기본 100%이고 스킬이 가산한다.

TowerSkillEntry
├─ skill             : TowerSkillData             // 이 타워 전용 정의
└─ unlockLevel       : int = 1                    // 기본 장착이면 1

TowerSkillData (타워별 SO, 공통 부품을 조합)
├─ trigger           : Always / OnAttack / OnHit / OnKill / EveryNthAttack
├─ nth               : int                        // EveryNthAttack일 때
├─ chance            : float                      // 0이면 확률 판정 없음
├─ cooldown          : float                      // 0이면 쿨타임 없음
├─ target            : 기본 타겟 / 보스 우선 / 범위 내 전체 / 아군 전체
└─ effects           : 효과 목록 + 수치
```

- **타워 레벨**은 로비에서 올리는 영구 성장이며 최대 15다. 타워 종류별로 저장하며 합성과 무관하다. 인게임에서는 오르지 않고, 게임 시작 시 정해진 값을 타워 생성 시점에 스냅샷으로 받는다 (`ITowerLevelService`, 전역 스코프)
- **타워 강화**(전투 중 성장)는 런타임 상태로만 존재하고 전투가 끝나면 초기화된다. 후순위
- 치명타 확률과 보너스(기본 10%, 100%)는 타워 데이터가 가지며 스킬이 가산한다. 판정 로직과 확률 판정은 공통 부품이다
- 효과 종류(이감, 공격력 업 등)가 합산 방식과 하드캡(없으면 무제한)을 가진다. 스킬은 종류와 수치만 넘긴다
- 상시 합과 지속 합(수치 + 공유 타이머)을 효과 종류마다 따로 보관하고, 최종 값 = 상시 합 + 지속 합에 캡을 적용한다
- 위 구조는 설계안이다. 필드 이름과 분할 단위는 구현 시 조정한다

## 플레이어 데이터 (메타)

```
IPlayerDataStore                                // 저장 위치를 숨기는 경계
├─ LoadAsync() / SaveAsync(PlayerData)
└─ LocalPlayerDataStore (persistentDataPath/player.json) — 서버 전환 시 구현체만 교체

PlayerData (저장 대상)
├─ gold, gem, cards : int                      // 헌터 카드는 공용 재화 하나
└─ hunters          : HunterRecord[] { towerId, level, unlocked }

IPlayerDataService (Project 스코프)
├─ Gold, Gem, Cards : ReactiveProperty<int>
├─ Hunters          : HunterState[] { Level, Unlocked : ReactiveProperty }
└─ AddGold / AddCards / UnlockHunter / TryLevelUp / IsUnlocked / IsAvailable
```

- 레벨업 비용은 `TowerData.levelUpCost`(등급 시트 값을 임포터가 채움)에서 읽는다. `Grades.csv`의 `startUnlocked`, `cardBase`, `cardPerLevel`, `goldBase`, `goldPerLevel`이 원천이다
- 로드와 레벨업 때 `ITowerLevelService`에 레벨을 반영하며, 게임 씬은 이 값을 그대로 읽는다
- 소환/합성 후보는 `IsAvailable`로 해금된 헌터만 거른다. 로비를 거치지 않은 에디터 테스트에서는 제한이 없다

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
