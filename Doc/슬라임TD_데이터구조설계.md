# 슬라임 TD 데이터 구조 설계

모든 밸런스 수치는 ScriptableObject로 분리한다. 아래는 `Assets/Scripts/Data/` 기준 실제 구조다.

## 게임 전역

```
GameConfig (SO, 1개)
├─ startingLife      : int = 20                 // 에셋 값 10
├─ startingJelly     : int = 0                  // 에셋 값 50 (인게임 재화)
├─ summonBaseCost    : int = 50                 // 에셋 값 10. 소환 비용 = 기본 + 증가 × 소환 횟수
├─ summonCostIncrease: int = 10                 // 에셋 값 2
├─ summonGradeId     : int = 1                  // 소환 등급 (일반)
├─ initialLobbyGold  : int = 10000               // 새 계정 시작 골드 (개발용 임시)
├─ initialGem        : int = 10000               // 새 계정 시작 보석 (개발용 임시)
└─ initialCards      : int = 10000               // 새 계정 시작 헌터 카드 (개발용 임시)
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
├─ gradeName         : string                   // 등급 시트의 이름 (임포터가 채움)
├─ enabled           : bool                     // false면 뽑기 후보에서 제외
├─ towerName         : string
├─ prefab            : GameObject
├─ attackRange       : float = 5                // 탐지 범위 (발사체 도달거리와 별개)
├─ attackType        : Melee / Projectile       // 근접 / 원거리(투사체)
├─ projectilePrefab  : GameObject               // Projectile일 때. 풀 크기는 코드 상수
├─ baseAttack        : float                    // 등급 시트의 기준공격력 (임포터가 채움)
├─ attackGrowthRate  : float                    // 레벨당 공격력 증가율 % (임포터가 채움)
├─ attackCooldown    : float                    // 시트
├─ critChance        : float = 10               // %. 시트
├─ critBonus         : float = 100              // %. 시트
├─ startUnlocked     : bool                     // 시작 해금 (등급 기본, 타워 시트로 예외 지정)
├─ unlockGemPrice    : int                      // 잠긴 헌터의 보석 구매 가격. 0이면 구매 불가
├─ levelUpCost       : LevelUpCostRule          // 등급 시트의 카드/골드 기본값과 레벨당 증가
└─ thumbnailSmall/Large : Sprite                // 카드(54px) / 상세 팝업(128px). 인스펙터에서 연결

스킬은 TowerData가 가지지 않는다. SkillCatalog가 towerId로 찾아 준다 (아래 "타워 스킬")

AttackBehaviourFactory.Create(towerData) -> MeleeAttack / ProjectileAttack
```

공격 애니메이션은 모든 타워가 Animator의 `Attack` 트리거 하나를 쓴다(`AttackBehaviourBase`의 상수). 타워 프리팹의 Animator Controller에 `Attack` Trigger 파라미터가 없으면 조용히 무시되고 애니메이션이 재생되지 않는다.

## 시트 → SO 임포트
 숫자의 원천은 CSV다. 1줄은 영문 키, 2줄은 한글 설명이며, 임포터는 1줄만 읽는다. 위치는 `Assets/Datas/Sheets/`다.

| 시트 | 키 | 열 |
|---|---|---|
| `Grades.csv` | `gradeId` | `name`, `baseAttack`, `attackGrowthRate`, `startUnlocked`, `cardBase`, `cardPerLevel`, `goldBase`, `goldPerLevel` |
| `Towers.csv` | `towerId` | `codeName`, `name`, `gradeId`, `enabled`, `attackRange`, `attackCooldown`, `critChance`, `critBonus`, `startUnlocked`(비우면 등급 기본), `unlockGemPrice` |
| `Skills.csv`, `SkillEffects.csv` | `skillId` | 아래 [타워 스킬](#타워-스킬) 참고 |

- 실행: 메뉴 `SlimeTD > Data > Import Tower Sheets`. `TowerData`를 ID로 찾아(없으면 `TowerData_<코드명>` 이름, 그래도 없으면 새로 생성) 숫자 필드를 덮어쓰고 `TowerCatalog`를 시트 기준으로 채운다
- 프리팹, 투사체 프리팹 같은 에셋 참조는 임포터가 건드리지 않으므로 인스펙터에서 한 번 연결한다
- 숫자는 시트에서만 고친다. 인스펙터에서 고쳐도 다음 임포트 때 덮어써진다

## 타워 스킬

스킬은 **타워 한 개에 귀속**되고, 시트(`Skills.csv`, `SkillEffects.csv`)가 원천이다. 임포트하면 스킬 에셋(`TowerSkillData`)과 `SkillCatalog`가 만들어진다. `TowerData`는 스킬을 모르고, 스킬이 자기 소속(`towerId`)을 가진다. 지금은 데이터 구조와 임포트까지 구현돼 있고, 전투에서 스킬을 실행하는 부분(발동 판정, 효과 적용)은 아직 없다. 스킬 내용은 미확정이라 시트의 `9xxx` 번호 스킬은 구조 검증용 테스트 스킬이다. 기존 [타워 스킬 설계](슬라임TD_타워스킬설계.md)의 스킬 목록은 참고용이다.

**정의(SO)와 런타임을 나눈다.** SO는 읽기 전용이고, 타워가 만들어질 때(`BaseTower.Initialize`) `TowerData`와 `SkillCatalog.GetSkills(towerId)`를 합쳐 타워별 런타임 객체가 쿨타임, N번째 공격 카운터, 수치 합산을 들고 관리한다 (런타임 객체는 아직 없다).

```
SkillCatalog (SO, 1개. Assets/Datas/SkillCatalog.asset)
└─ skills            : TowerSkillData[]           // 임포터가 채움
   런타임 인덱스: towerId -> 스킬 목록 (해금 레벨 순, 같으면 ID 순)
   GetSkills(towerId) — 없으면 빈 목록. 해금 여부와 상관없이 그 타워의 전체 스킬 (UI 슬롯용)
   GetActiveSkills(towerId, level, buffer) — 레벨에 해금됐고, 해금된 업그레이드 스킬에 대체되지 않은 스킬만
   GetSlots(towerId, level, buffer) — UI 슬롯용. 계열(upgradeOf로 이어진 스킬들) 하나가 슬롯 하나
     SkillSlot: Root(계열 대표), Current(적용 중인 단계, 잠겼으면 없음), Next(다음에 해금될 단계, 없으면 끝)
     IsLocked = Current 없음 → 자물쇠 / HasNextUpgrade = Current와 Next 둘 다 있음 → 화살표

TowerSkillData (SO, 스킬당 1개. Assets/Datas/Skills/<타워 코드명>/Skill_<코드명>.asset)
├─ id, codeName, displayName, description
├─ towerId           : int                        // 귀속된 타워
├─ unlockLevel       : int = 1                    // 타워 레벨이 이 값 이상이면 활성
├─ upgradeOfId       : int                        // 0이면 일반 스킬. 값이 있으면 그 스킬을 대체하는 업그레이드
├─ trigger           : Always / OnAttack / OnHit / OnKill / EveryNthAttack
├─ nth               : int                        // EveryNthAttack일 때
├─ chance            : float                      // %. 0이면 확률 판정 없음
├─ cooldown          : float                      // 초. 0이면 쿨타임 없음
└─ effects           : SkillEffectEntry[]         // type, value, valueType, duration
```

| 시트 | 키 | 열 |
|---|---|---|
| `Skills.csv` | `skillId` | `codeName`, `name`, `description`, `towerId`, `unlockLevel`(기본 1), `upgradeOf`(대체할 스킬 ID, 비우면 없음), `trigger`, `nth`, `chance`(%, 비우면 없음), `cooldown`(초, 비우면 없음) |
| `SkillEffects.csv` | `skillId` (스킬 하나에 효과 여러 줄) | `effectType`, `value`, `valueType`, `duration`(초, 비우면 상시) |

- 실행: 메뉴 `SlimeTD > Data > Import Skill Sheets`. 스킬을 ID로 찾아(없으면 새로 생성) 값을 덮어쓰고, 시트에 있는 스킬로 `SkillCatalog`를 채운다. 여러 번 실행해도 결과가 같다. `towerId`가 `Towers.csv`에 없는 행은 건너뛴다
- `effectType`: `CritChance` / `CritBonus` / `AttackSpeed` / `Attack` / `MultiShot` / `Pierce` / `Slow` / `JellyGain`
- `valueType`: `Fixed`(고정, 더하기) / `Percent`(퍼센트). 효과마다 쓸 수 있는 단위가 정해져 있다 (`SkillEffectRules`)

| 효과 | 고정 | 퍼센트 |
|---|---|---|
| `Attack` (공격력) | +N | 레벨이 반영된 기본 공격력의 N% |
| `AttackSpeed` | 없음 | +N% (예: +10% → 1.1배) |
| `CritChance`, `CritBonus` | +N%p | 없음 |
| `Slow` (이감) | 속도 −N | 슬라임 기본 속도의 N% |
| `MultiShot`, `Pierce`, `JellyGain` | +N (발 수, 관통 수, 젤리) | 없음 |

- 업그레이드: 같은 스킬을 강화하는 단계(예: 2연발 확률 10% → 20%)는 새 행을 만들고 `upgradeOf`에 이전 단계의 스킬 ID를 넣는다. 같은 계열인지는 이 연결로 안다 (대표 = `upgradeOf`가 없는 스킬, 따라가면 계열 전체). 업그레이드가 해금되면 이전 단계는 비활성이 되고 가장 높게 해금된 단계 하나만 활성이다. 대체 대상은 같은 타워의 더 낮은 해금 레벨 스킬이어야 하고, 아니면 임포트 때 경고한다
- 지원하지 않는 단위 조합이나 해석할 수 없는 값이 있는 행은 로그를 남기고 건너뛴다
- 공격력(레벨) = `baseAttack × (1 + attackGrowthRate / 100 × (레벨 - 1))`. 공격속도는 레벨로 오르지 않는다. 기본 100%이고 스킬이 가산한다
- 데미지는 발사 시점(근접은 타격 프레임)에 `DamageInfo`(데미지, 치명타 여부)로 확정되어 슬라임에게 전달된다. 효과 목록은 이후 단계에서 `DamageInfo`에 추가한다. 공격 시 확률로 젤리를 얻는 스킬(운빨존많겜 산적 방식)은 타워가 공격할 때 바로 지급한다
- **타워 레벨**은 로비에서 올리는 영구 성장이며 최대 15다. 타워 종류별로 저장하며 합성과 무관하다. 인게임에서는 오르지 않고, 게임 시작 시 정해진 값을 타워 생성 시점에 스냅샷으로 받는다 (`ITowerLevelService`, 전역 스코프)
- 같은 종류 효과의 중첩은 수치를 합산하고, 지속 효과는 같은 종류를 새로 걸면 타이머를 갱신한다. 상시 합과 지속 합(수치 + 공유 타이머)은 따로 보관하고 최종 값 = 상시 합 + 지속 합에 캡을 적용한다. 효과 종류마다 하드캡을 둘 수 있다 (상한 수치 미정)
- **타워 강화**(전투 중 성장)는 런타임 상태로만 존재하고 전투가 끝나면 초기화된다. 후순위

## 플레이어 데이터 (메타)

```
IPlayerDataStore                                // 저장 위치를 숨기는 경계
├─ LoadAsync() / SaveAsync(PlayerData)
└─ LocalPlayerDataStore (persistentDataPath/player.json) — 서버 전환 시 구현체만 교체

PlayerData (저장 대상)
├─ gold, gem, cards : int                      // 헌터 카드는 공용 재화 하나
├─ bestWave         : int                      // 도달한 최고 웨이브. 저장 파일에 없으면 0
└─ hunters          : HunterRecord[] { towerId, level, unlocked }

IPlayerDataService (Project 스코프)
├─ Gold, Gem, Cards, BestWave : ReactiveProperty<int>
├─ Hunters          : HunterState[] { Level, Unlocked : ReactiveProperty }
└─ AddGold / AddCards / UnlockHunter / TryLevelUp / IsUnlocked / IsAvailable / ReportWaveReached
```

- `ReportWaveReached(n)`은 기존 값보다 클 때만 갱신·저장한다. 초기화 전(게임 씬만 단독 실행)에는 저장하지 않는다. 호출 시점과 값 규칙은 [로비 UI](슬라임TD_로비UI.md#최고-웨이브-기록-규칙) 참고

- 레벨업 비용은 `TowerData.levelUpCost`(등급 시트 값을 임포터가 채움)에서 읽는다. `Grades.csv`의 `startUnlocked`, `cardBase`, `cardPerLevel`, `goldBase`, `goldPerLevel`이 원천이다
- 로드와 레벨업 때 `ITowerLevelService`에 레벨을 반영하며, 게임 씬은 이 값을 그대로 읽는다
- 해금: 시작 해금은 `Grades.csv`의 `startUnlocked`(등급 기본)와 `Towers.csv`의 `startUnlocked`(타워별 예외)로 정한다. 잠긴 헌터는 `unlockGemPrice`만큼 보석을 써서 `TryPurchaseHunter`로 해금한다
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
