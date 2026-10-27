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
| `Skills.csv` | `skillId` | 아래 [타워 스킬](#타워-스킬) 참고 |

- 실행: 메뉴 `SlimeTD > Data > Import Tower Sheets`. `TowerData`를 ID로 찾아(없으면 `TowerData_<코드명>` 이름, 그래도 없으면 새로 생성) 숫자 필드를 덮어쓰고 `TowerCatalog`를 시트 기준으로 채운다
- 프리팹, 투사체 프리팹 같은 에셋 참조는 임포터가 건드리지 않으므로 인스펙터에서 한 번 연결한다
- 숫자는 시트에서만 고친다. 인스펙터에서 고쳐도 다음 임포트 때 덮어써진다

## 타워 스킬

스킬은 **타워 한 개에 귀속**되고, 시트(`Skills.csv`) 한 파일이 원천이다. 임포트하면 `SkillCatalog` 하나가 스킬 데이터를 직접 가진다 (스킬별 에셋 파일은 없다). `TowerData`는 스킬을 모르고, 스킬이 자기 소속(`towerId`)을 가진다. 지금은 궁수, 일병, 나무꾼, 석궁병, 레인저, 기사 스킬이 들어 있고, 전투에서 실행되는 것은 상시 스탯 효과(`Attack`, `AttackSpeed`, `CritChance`, `CritBonus`), 상시 관통(`Pierce`. 화살 한 발의 판정을 맞은 모든 대상이 공유한다)과 공격 시 발동 효과(`MultiShot`, `DamageMultiplier`, `Splash`, `JellyGain`)다.

공격 1회마다 `OnAttack` 스킬의 확률과 쿨타임을 한 번만 판정해(`TowerSkillController.RollAttack`) 결과(`AttackSkillRoll`)를 공격 방식에 넘긴다. **데미지 스킬(`MultiShot`, `DamageMultiplier`, `Splash`)이 발동한 공격은 치명타 판정을 하지 않고**, 그 공격의 모든 화살이 같은 데미지를 가진다. 광역(`Splash`)은 맞은 대상 중심 반경 안의 다른 슬라임에게 같은 `DamageInfo`를 준다 (`SplashDamage`). 젤리는 데미지와 무관해 치명타 규칙에 영향이 없으며 `IGameplayService.AddJelly`로 바로 지급한다. 쿨타임은 발동했을 때부터 `Time.time` 기준으로 흐르고 공격속도와 무관하다. 그 밖의 트리거와 효과는 아직 없다. 기존 [타워 스킬 설계](슬라임TD_타워스킬설계.md)의 스킬 목록은 참고용이다.

**정의와 런타임을 나눈다.** `SkillCatalog`의 데이터는 읽기 전용이고, 타워가 만들어질 때(`BaseTower.Initialize`) 레벨에 맞는 활성 스킬을 구해 `TowerStats`(상시 스탯 합산)와 `TowerSkillController`(공격 시 확률 판정)에 넘긴다.

```
SkillCatalog (SO, 1개. Assets/Datas/SkillCatalog.asset)
└─ skills            : TowerSkillData[]           // 임포터가 채움. 직렬화 클래스(SO 아님)
   런타임 인덱스: towerId -> 스킬 목록 (해금 레벨 순, 같으면 ID 순)
   GetSkills(towerId) — 없으면 빈 목록. 해금 여부와 상관없이 그 타워의 전체 스킬 (UI 슬롯용)
   GetActiveSkills(towerId, level, buffer) — 레벨에 해금됐고, 해금된 업그레이드 스킬에 대체되지 않은 스킬만
   GetSlots(towerId, level, buffer) — UI 슬롯용. 계열(upgradeOf로 이어진 스킬들) 하나가 슬롯 하나
     SkillSlot: Root(계열 대표), Current(적용 중인 단계, 잠겼으면 없음), Next(다음에 해금될 단계, 없으면 끝)
     IsLocked = Current 없음 → 자물쇠 / HasNextUpgrade = Current와 Next 둘 다 있음 → 화살표

TowerSkillData ([Serializable] 클래스. SkillCatalog 안에 들어 있다)
├─ id, displayName, description
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
| `Skills.csv` | `skillId` | 식별 `name`, `description` / 대상 `towerId`, `unlockLevel`(기본 1), `upgradeOf`(대체할 스킬 ID, 비우면 없음) / 발동 `trigger`, `nth`, `chance`(%, 비우면 없음), `cooldown`(초, 비우면 없음) / 효과 `effectType`, `value`(`%`를 붙이면 퍼센트), `duration`(초, 비우면 상시) |

- 스킬 하나에 효과는 한 개다. 효과가 둘인 스킬이 필요해지면 효과 열을 한 세트 더 추가한다
- 실행: 메뉴 `SlimeTD > Data > Import Skill Sheets`. 시트 전체로 `SkillCatalog`의 스킬 목록을 새로 채운다. 여러 번 실행해도 결과가 같다. `towerId`가 `Towers.csv`에 없는 행은 건너뛴다
- 스킬 ID는 `타워ID×100+순번`이다 (궁수 10101~, 일병 10301~). 한 번 정한 ID는 바꾸지 않고, 추가는 그 타워의 다음 빈 번호를 쓴다
- `effectType`: `CritChance` / `CritBonus` / `AttackSpeed` / `Attack` / `MultiShot` / `Pierce` / `Slow` / `JellyGain` / `DamageMultiplier` / `Splash`
- `value`: `%`가 붙으면 `Percent`, 없으면 `Fixed`로 읽는다. 효과마다 쓸 수 있는 단위가 정해져 있다 (`SkillEffectRules`). `MultiShot`의 값은 추가로 쏘는 발수(2연발이면 1)이고, `DamageMultiplier`는 공격력의 총 배율(300%는 3배, 치명타 보너스와 달리 "더하기"가 아니다)이다

| 효과 | 고정 | 퍼센트 |
|---|---|---|
| `Attack` (공격력) | +N | 레벨이 반영된 기본 공격력의 N% |
| `AttackSpeed` | 없음 | +N% (예: +10% → 1.1배) |
| `CritChance`, `CritBonus` | +N%p | 없음 |
| `Slow` (이감) | 속도 −N | 슬라임 기본 속도의 N% |
| `MultiShot`, `Pierce`, `JellyGain` | +N (발 수, 관통 수, 젤리) | 없음 |
| `DamageMultiplier` | 없음 | 공격력의 N% (스킬 발동 시 치명타 판정 없이 이 배율로만 계산) |
| `Splash` (광역) | 반경 N (같은 데미지) | 없음 |

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
