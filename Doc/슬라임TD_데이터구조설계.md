# 슬라임 TD 데이터 구조 설계

모든 밸런스 수치는 ScriptableObject로 분리한다. 아래는 `Assets/Scripts/Data/` 기준 실제 구조다.

## 게임 전역

```
GameConfig (SO, 1개)
├─ startingLife      : int = 20                 // 에셋 값 10
├─ startingGold      : int = 0                  // 에셋 값 50 (인게임 골드)
├─ summonBaseCost    : int = 50                 // 에셋 값 10. 소환 비용 = 기본 + 증가 × 소환 횟수
├─ summonCostIncrease: int = 10                 // 에셋 값 2
├─ summonGradeId     : int = 1                  // 소환 등급 (일반)
├─ slowMinSpeedRatio : float = 0.25             // 감속 하한. 기본 속도의 25% 밑으로 내려가지 않는다
└─ bossSlowScale     : float = 0.5              // 보스에게 걸리는 감속 비율 (절반)
```

새 계정 시작 재화는 서버 `NewAccount.csv`가 정한다 (클라이언트에 두지 않는다).

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
└─ towers            : TowerData[]              // 전체 타워 목록. 시트 기준으로 채움 (sheets-to-so 스킬)
   런타임 인덱스: ID -> TowerData, 등급 -> 뽑기 후보(활성 + 프리팹 있음)
   TryDraw(등급, 필터, 난수), TryDrawNextGrade(소스, 필터, 난수) — 소환과 합성 결과에 사용
   HasNextGrade(소스, 필터) — 합성 가능 여부만 본다 (뽑지 않음)

TowerData (SO, 종류당 1개)
├─ id                : int                      // 시트 ID. 등급 대역을 가짐
├─ gradeId           : int                      // 시트 등급. 등급의 유일한 원천
├─ gradeName         : string                   // 등급 시트의 이름
├─ enabled           : bool                     // false면 뽑기 후보에서 제외
├─ towerName         : string
├─ prefab            : GameObject
├─ attackRange       : float = 5                // 탐지 범위 (발사체 도달거리와 별개)
├─ attackType        : Melee / Projectile / Beam  // 근접 / 원거리(투사체) / 즉시 타격 빔. 인스펙터에서 지정
├─ projectilePrefab  : GameObject               // Projectile일 때. 풀 크기는 코드 상수
├─ baseAttack        : float                    // 등급 시트의 기준공격력
├─ attackGrowthRate  : float                    // 레벨당 공격력 증가율 %
├─ attackCooldown    : float                    // 시트
├─ critChance        : float = 10               // %. 시트
├─ critBonus         : float = 100              // %. 시트
├─ unlockDiamondPrice: int                      // 잠긴 헌터의 다이아 구매 가격 (표시용, 판정은 서버). 0이면 구매 불가
├─ levelUpCost       : LevelUpCostRule          // LevelUpCosts 시트의 레벨별 카드/코인 (표시용, 판정은 서버)
└─ thumbnailSmall/Large : Sprite                // 카드(54px) / 상세 팝업(128px). 인스펙터에서 연결

스킬은 TowerData가 가지지 않는다. SkillCatalog가 towerId로 찾아 준다 (아래 "타워 스킬")

AttackBehaviourFactory.Create(towerData) -> MeleeAttack / ProjectileAttack / BeamAttack
  BeamAttack: 타격 프레임에 대상에게 바로 데미지를 주고, ProjectileLauncher.FirePoint에서 대상까지 빔을 그린다 (빔 연출은 임시 LineRenderer)
```

공격 애니메이션은 모든 타워가 Animator의 `Attack` 트리거 하나를 쓴다(`AttackBehaviourBase`의 상수). 타워 프리팹의 Animator Controller에 `Attack` Trigger 파라미터가 없으면 조용히 무시되고 애니메이션이 재생되지 않는다.

## 시트 → SO 임포트
 숫자의 원천은 CSV다. 1줄은 영문 키이고 열 설명은 데이터 저장소 `Sheets/README.md`에 있다. 위치는 데이터 저장소 `C:\Projects\SlimeTD-Data\Sheets\`다 (클라이언트 프로젝트 안에 복사본을 두지 않는다).

| 시트 | 키 | 열 |
|---|---|---|
| `Grades.csv` | `gradeId` | `name`, `baseAttack`, `attackGrowthRate`, `gachaWeight` (레벨별 비용은 `LevelUpCosts.csv`의 `cards`, `coin`) |
| `Towers.csv` | `towerId` | `codeName`, `name`, `gradeId`, `enabled`, `attackRange`, `attackCooldown`, `critChance`, `critBonus`, `startUnlocked`(1/0, 비우면 잠금), `unlockDiamondPrice` |
| `Skills.csv` | `skillId` | 아래 [타워 스킬](#타워-스킬) 참고 |

- 실행: `sheets-to-so` 스킬로 반영한다. 변환 코드나 메뉴 없이 시트 값을 `TowerData_<코드명>` 에셋에 직접 쓰고 `TowerCatalog`를 시트 기준으로 맞춘다
- 타워 프리팹은 `codeName`으로 로드하고, 투사체 모양은 타워 프리팹의 `ProjectileLauncher`가 가진다. 시트에는 넣지 않는다
- 숫자는 시트에서만 고친다. 인스펙터에서 고쳐도 다음 임포트 때 덮어써진다

## 타워 스킬

스킬은 **타워 한 개에 귀속**되고, 시트(`Skills.csv`) 한 파일이 원천이다. 임포트하면 `SkillCatalog` 하나가 스킬 데이터를 직접 가진다 (스킬별 에셋 파일은 없다). `TowerData`는 스킬을 모르고, 스킬이 자기 소속(`towerId`)을 가진다. 지금은 궁수, 일병, 나무꾼, 착한 슬라임, 석궁병, 레인저, 기사 스킬이 들어 있고, 전투에서 실행되는 것은 상시 스탯 효과(`Attack`, `AttackSpeed`, `CritChance`, `CritBonus`), 상시 관통(`Pierce`. 화살 한 발의 판정을 맞은 모든 대상이 공유한다), 상시 감속(`Slow`. 맞은 적에게 건다)과 공격 시 발동 효과(`MultiShot`, `DamageMultiplier`, `Splash`, `GoldGain`)다.

공격 1회마다 `OnAttack` 스킬의 확률과 쿨타임을 한 번만 판정해(`TowerSkillController.RollAttack`) 결과(`AttackSkillRoll`)를 공격 방식에 넘긴다. **데미지 스킬(`MultiShot`, `DamageMultiplier`, `Splash`)이 발동한 공격은 치명타 판정을 하지 않고**, 그 공격의 모든 화살이 같은 데미지를 가진다. 광역(`Splash`)은 맞은 대상 중심 반경 안의 다른 슬라임에게 같은 `DamageInfo`를 준다 (`SplashDamage`). 골드 획득(`GoldGain`)은 데미지와 무관해 치명타 규칙에 영향이 없으며, 공격을 시작할 때 발동이 정해지면 타격 결과와 상관없이 바로 지급한다 (`IGameplayService.AddSkillGold`). 쿨타임은 발동했을 때부터 `Time.time` 기준으로 흐르고 공격속도와 무관하다. 그 밖의 트리거와 효과는 아직 없다. 기존 [타워 스킬 설계](슬라임TD_타워스킬설계.md)의 스킬 목록은 참고용이다.

**정의와 런타임을 나눈다.** `SkillCatalog`의 데이터는 읽기 전용이고, 타워가 만들어질 때(`BaseTower.Initialize`) 레벨에 맞는 활성 스킬을 구해 `TowerStats`(상시 스탯 합산)와 `TowerSkillController`(공격 시 확률 판정)에 넘긴다.

```
SkillCatalog (SO, 1개. Assets/Datas/SkillCatalog.asset)
└─ skills            : TowerSkillData[]           // 시트 기준으로 채움. 직렬화 클래스(SO 아님)
   런타임 인덱스: towerId -> 스킬 목록 (해금 레벨 순, 같으면 ID 순)
   GetSkills(towerId) — 없으면 빈 목록. 해금 여부와 상관없이 그 타워의 전체 스킬 (UI 슬롯용)
   GetActiveSkills(towerId, level, buffer) — 레벨에 해금됐고, 해금된 업그레이드 스킬에 대체되지 않은 스킬만
   GetSlots(towerId, level, buffer) — UI 슬롯용. 계열(upgradeOf로 이어진 스킬들) 하나가 슬롯 하나
     SkillSlot: Root(계열 대표), Current(적용 중인 단계, 잠겼으면 없음), Next(다음에 해금될 단계, 없으면 끝)
     IsLocked = Current 없음 → 자물쇠 / HasNextUpgrade = Current와 Next 둘 다 있음 → 화살표

TowerSkillData ([Serializable] 클래스. SkillCatalog 안에 들어 있다)
├─ id, displayName, description
├─ IconName          : string (속성)               // SkillIcon_<ID>. 스킬 아이콘 아틀라스의 스프라이트 이름. 파일은 Assets/Art/UI/SkillIcon/SkillIcon_<ID>.png(128x128)
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
- 실행: `sheets-to-so` 스킬로 반영한다. 시트 전체로 `SkillCatalog`의 스킬 목록을 새로 채운다. `towerId`가 `Towers.csv`에 없는 행은 반영하지 않고 보고한다
- 스킬 ID는 `타워ID×100+순번`이다 (궁수 10101~, 일병 10301~). 한 번 정한 ID는 바꾸지 않고, 추가는 그 타워의 다음 빈 번호를 쓴다
- `effectType`: `CritChance` / `CritBonus` / `AttackSpeed` / `Attack` / `MultiShot` / `Pierce` / `Slow` / `GoldGain` / `DamageMultiplier` / `Splash`
- `value`: `%`가 붙으면 `Percent`, 없으면 `Fixed`로 읽는다. 효과마다 쓸 수 있는 단위가 정해져 있다 (`SkillEffectRules`). `MultiShot`의 값은 추가로 쏘는 발수(2연발이면 1)이고, `DamageMultiplier`는 공격력의 총 배율(300%는 3배, 치명타 보너스와 달리 "더하기"가 아니다)이다

| 효과 | 고정 | 퍼센트 |
|---|---|---|
| `Attack` (공격력) | +N | 레벨이 반영된 기본 공격력의 N% |
| `AttackSpeed` | 없음 | +N% (예: +10% → 1.1배) |
| `CritChance`, `CritBonus` | +N%p | 없음 |
| `Slow` (이감) | 없음 | 슬라임 기본 속도의 N% 감소. `duration`(초)이 필요하다 |
| `MultiShot`, `Pierce`, `GoldGain` | +N (발 수, 관통 수, 인게임 골드) | 없음 |
| `DamageMultiplier` | 없음 | 공격력의 N% (스킬 발동 시 치명타 판정 없이 이 배율로만 계산) |
| `Splash` (광역) | 반경 N (같은 데미지) | 없음 |

- 업그레이드: 같은 스킬을 강화하는 단계(예: 2연발 확률 10% → 20%)는 새 행을 만들고 `upgradeOf`에 이전 단계의 스킬 ID를 넣는다. 같은 계열인지는 이 연결로 안다 (대표 = `upgradeOf`가 없는 스킬, 따라가면 계열 전체). 업그레이드가 해금되면 이전 단계는 비활성이 되고 가장 높게 해금된 단계 하나만 활성이다. 대체 대상은 같은 타워의 더 낮은 해금 레벨 스킬이어야 하고, 아니면 임포트 때 경고한다
- 지원하지 않는 단위 조합이나 해석할 수 없는 값이 있는 행은 로그를 남기고 건너뛴다
- 공격력(레벨) = `baseAttack × (1 + attackGrowthRate / 100 × (레벨 - 1))`. 공격속도는 레벨로 오르지 않는다. 기본 100%이고 스킬이 가산한다
- 데미지는 발사 시점(근접은 타격 프레임)에 `DamageInfo`(데미지, 치명타 여부)로 확정되어 슬라임에게 전달된다. 효과 목록은 이후 단계에서 `DamageInfo`에 추가한다. 공격 시 확률로 골드를 얻는 스킬(운빨존많겜 산적 방식)은 타워가 공격을 시작할 때 바로 지급한다 (타격 때 대상이 죽어 있어도 받는다)
- **타워 레벨**은 로비에서 올리는 영구 성장이며 최대 15다. 타워 종류별로 저장하며 합성과 무관하다. 인게임에서는 오르지 않고, 게임 시작 시 정해진 값을 타워 생성 시점에 스냅샷으로 받는다 (`ITowerLevelService`, 전역 스코프)
- 버프/디버프 중첩은 [타워 스킬 설계 5장](슬라임TD_타워스킬설계.md#5-버프--디버프)을 따른다. 감속은 타워 단위(`SpawnId`)로 구분해 다른 타워끼리 곱연산으로 쌓고, 같은 타워는 지속 시간만 갱신한다. 감속 정보는 타격 정보(`DamageInfo.SourceSpawnId`, `SlowPercent`, `SlowDuration`)에 실려 슬라임의 `SlimeSlow`가 관리한다. 광역과 관통으로 맞은 대상에도 같이 걸린다
- **타워 강화**(전투 중 성장)는 런타임 상태로만 존재하고 전투가 끝나면 초기화된다. 후순위

## 계정 (메타)

계정은 서버가 들고 확정한다. 클라이언트에 로컬 저장은 없다. Bootstrap에서 서버에 접속·로그인해 계정을 받고, 실패하면 접속 실패 화면을 띄운다.

```
IAccountService (Project 스코프)
├─ Coin, Diamond, BestWave : ReactiveProperty<int>
├─ Hunters          : HunterState[] { Level, Unlocked, Cards : ReactiveProperty }
├─ InitializeAsync()                            // 접속 + 로그인 + 계정 받기
├─ LevelUpAsync / PurchaseHunterAsync / DrawHunterCardGachaAsync   // 서버 요청. 결과는 응답의 아이템·헌터로 갱신
└─ SetBestWave(n)                               // 싱글 전투 종료 응답의 best_wave를 받는다
```

- 레벨업은 그 헌터의 `Cards`와 코인을, 구매는 다이아를 쓴다. 차감과 확정은 서버가 하고, 클라이언트의 `CanLevelUp`은 버튼 표시용이다
- 레벨업 비용은 `TowerData.levelUpCost`(LevelUpCosts 시트 값)에서 읽는다. `LevelUpCosts.csv`의 `cards`, `coin`이 원천이다
- 로그인과 레벨업 때 `ITowerLevelService`에 레벨을 반영하며, 게임 씬은 이 값을 그대로 읽는다
- 해금: 시작 해금은 `Towers.csv`의 `startUnlocked`로 타워마다 정한다 (등급 기본값은 없다). 잠긴 헌터는 `unlockDiamondPrice`만큼 다이아를 써서 구매한다. 확정 기획은 일반·희귀·영웅은 시작 해금, 전설만 다이아 구매·이벤트다
- 헌터 카드 뽑기는 서버가 굴린다 (팩 티켓으로 사고 보유 헌터만, 전설·천장·잠긴 카드 없음). 클라이언트에는 굴림 코드가 없고 `HunterCardGachaConfig`는 팩 표시용(카드 수, 티켓 가격)만 갖는다
- 클라이언트 SO의 메타 값(`TowerData`의 레벨업 비용·다이아 가격, 팩)은 서버가 판정하는 값의 표시용 사본이다. 해금, 뽑기 확률, 환급, 새 계정, 보상, 에너지 값은 클라이언트에 두지 않는다
- 팩 티켓(1000006)과 에너지(1000007)는 서버 계정 아이템이다. 에너지 최대치와 다음 회복 시각은 계정 정보(`energy_max`, `energy_recover_at_ms`)로 온다 ([메타 재화 밸런스](슬라임TD_메타재화밸런스.md))
- 소환/합성 후보는 `IsAvailable`로 해금된 헌터만 거른다. 로비를 거치지 않은 에디터 테스트에서는 제한이 없다
- 재화 지급(`AddCoin` 등)은 아직 서버 기능이 없어 화면에만 반영되고 "서버 미반영" 경고를 남긴다

## 싱글 전투 서버 검증

싱글은 클라이언트가 혼자 플레이하고, 서버는 시작과 끝만 받는다. 서버는 받은 기록으로 같은 전투를 자기 전투 코어(`SlimeTD.Battle`)로 다시 계산해 결과가 같은지 확인하고, 같으면 최고 웨이브를 기록한다. 서버 형식은 서버 저장소 `Protocol/single_battle.proto`.

```
ISingleBattleService (Project 스코프)
├─ StartAsync(stageId)        // 로비 싱글 버튼. 응답의 battle_id, seed, 스테이지(code_name, map_key 등)를 들고 게임 씬을 연다
├─ MarkStarted / AdvanceTick  // WaveSpawner가 부른다. 틱 = 고정 업데이트 1번
├─ RecordArrival / RecordKill / RecordSummon / RecordMerge / RecordSkillGold
├─ SummonRandom, MergeRandom  // seed로 만든 BattleRandom (서버 파일 그대로 복사)
└─ FinishAsync(승패, 도달 웨이브, 라이프, 골드)   // 불일치면 서버가 준 차이 목록을 로그로 남긴다
```

- **틱**: 1틱 = 1/30초 = 고정 업데이트 1번 (`TimeManager` Fixed Timestep 1/30). 웨이브 일정(웨이브 시작, 소환, 스폰 완료)은 시작할 때 틱으로 미리 정하고(초 × 30을 반올림), 슬라임은 틱마다 `속도 / 30`만큼 경로를 간다. 화면은 틱 사이를 보간한다. 프레임 시간으로 움직이면 서버 계산과 몇 틱씩 어긋난다
- **슬라임 번호**: 전투 안에서 소환 순서대로 1, 2, 3...
- **좌표**: 내 구역(slot 0) 그리드 좌표 (x 0부터 오른쪽, y 0부터 위쪽)
- **소환·합성 결과**: 서버가 준 seed로 만든 난수에서 뽑는다 (용도별 목록: 소환용, 합성용). 후보는 해금된 헌터를 towerId 오름차순으로 놓는다. 실제로 성공할 때만 뽑고, 뽑은 뒤 실패하면 뽑기 전 위치로 되돌린다. 치명타와 스킬 확률은 서버가 계산하지 않아 `UnityEngine.Random`을 쓴다
- **골드 스킬**(벌목 수익 등): 발동하면 바로 지급한다 (`IGameplayService.AddSkillGold(양, 칸)`)
- 진행 중인 싱글 전투가 없으면(멀티, 게임 씬 단독 실행) 기록하지 않고 소환·합성은 `UnityEngine.Random`으로 뽑는다
- 끝나기 전에 게임 씬을 나가면 전투를 버린다. 서버 쪽 전투는 다음 시작 때 무효가 된다

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
