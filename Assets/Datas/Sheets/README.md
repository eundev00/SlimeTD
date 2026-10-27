# 시트 편집 가이드

시트는 UTF-8 CSV이고 **1줄이 영문 열 이름**이다. 숫자는 시트에서만 고치며, 고친 뒤 메뉴로 임포트한다. 이 폴더의 숫자를 인스펙터에서 직접 고치면 다음 임포트 때 덮어써진다.

- 타워/등급: `SlimeTD > Data > Import Tower Sheets`
- 스킬: `SlimeTD > Data > Import Skill Sheets`

## ID 규칙

모든 데이터의 ID는 종류와 상관없이 겹치면 안 된다. 한 번 정한 ID는 바꾸지 않는다.

| 종류 | 규칙 |
|---|---|
| 등급 | 1~4 |
| 타워 | 101~ (3자리) |
| 스킬 | 타워ID×100+순번 (궁수 10101~, 일병 10301~). 추가는 그 타워의 다음 빈 번호 |

## Grades.csv

| 열 | 설명 |
|---|---|
| gradeId | 등급 ID |
| name | 이름 |
| baseAttack | 기준 공격력 |
| attackGrowthRate | 레벨당 공격력 증가율(%) |
| startUnlocked | 시작 해금 (1/0) |
| cardBase, cardPerLevel | 레벨업 카드 기본값, 레벨당 증가 |
| goldBase, goldPerLevel | 레벨업 골드 기본값, 레벨당 증가 |

## Towers.csv

| 열 | 설명 |
|---|---|
| towerId | 타워 ID (101~) |
| codeName | 영문 코드명. 에셋 이름 `TowerData_<코드명>`에 쓰인다 |
| name | 화면에 보이는 이름 |
| gradeId | 등급 ID |
| enabled | 활성 (1/0) |
| attackRange | 사거리 |
| attackCooldown | 공격 쿨다운(초) |
| critChance | 치명타 확률(%) |
| critBonus | 치명타 보너스(%) |
| startUnlocked | 시작 해금. 비우면 등급 기본값 |
| unlockGemPrice | 해금 보석 가격 (0 = 구매 불가) |

## Skills.csv

스킬 하나에 효과는 한 개다.

| 열 | 설명 |
|---|---|
| skillId | 스킬 ID |
| name, description | 화면에 보이는 이름과 설명 |
| towerId | 귀속된 타워 |
| unlockLevel | 해금 레벨 (비우면 1) |
| upgradeOf | 대체하는 스킬 ID. 업그레이드일 때만. 같은 타워의 더 낮은 해금 레벨 스킬이어야 한다 |
| trigger | 발동 시점: `Always` / `OnAttack` / `OnHit` / `OnKill` / `EveryNthAttack` |
| nth | `EveryNthAttack`일 때 N |
| chance | 발동 확률(%). 비우면 확률 판정 없음 |
| cooldown | 쿨타임(초). 비우면 없음 |
| effectType | 효과 종류 (아래) |
| value | 수치. `%`를 붙이면 퍼센트, 없으면 고정값 |
| duration | 지속 시간(초). 비우면 상시 |

스킬 아이콘은 시트에 적지 않는다. `Assets/Art/UI/SkillIcon/SkillIcon_{스킬ID}.png`(128x128)를 두고 스킬 아이콘 아틀라스로 묶어 로드한다. 스프라이트 이름은 파일 이름이고 코드에서는 `TowerSkillData.IconName`으로 얻는다.

### effectType과 수치 해석

| effectType | 허용 단위 | 의미 |
|---|---|---|
| Attack | 고정 / 퍼센트 | 공격력 +N, 또는 레벨 반영 기본 공격력의 +N% |
| AttackSpeed | 퍼센트만 | 공격속도 +N% |
| CritChance | 고정만 | 치명타 확률 +N%p |
| CritBonus | 고정만 | 치명타 보너스 +N%p |
| MultiShot | 고정만 | **추가로** 쏘는 발수 (2연발이면 1) |
| DamageMultiplier | 퍼센트만 | 공격력의 **총** 배율 (300%는 3배). 발동 시 치명타 판정을 하지 않는다 |
| Pierce | 고정만 | **추가로** 관통하는 대상 수 (1이면 총 2마리). 맞은 모든 대상에게 같은 데미지 |
| Slow | 고정 / 퍼센트 | 이동속도 감소 (미구현) |
| Splash | 고정만 | 맞은 대상 중심 반경 N 안의 다른 슬라임에게 **같은 데미지**를 준다 (중심 대상 제외, 화살은 첫 타격 때 한 번만) |
| JellyGain | 고정만 | 젤리 +N. 공격 1회마다 확률 판정 후 바로 얻는다 (OnAttack) |

**스킬이 발동한 공격은 치명타 판정을 하지 않는다.** 데미지에 영향을 주는 OnAttack 스킬(MultiShot, DamageMultiplier, Splash)이 터지면 그 공격의 화살 전부가 치명타 없이 같은 데미지가 된다. Always 패시브와 JellyGain은 해당하지 않는다. chance와 cooldown은 함께 쓸 수 있고, 쿨타임은 공격속도와 무관하게 발동했을 때부터 흐른다.

허용되지 않는 단위를 쓰거나 해석할 수 없는 값이 있는 행은 임포트 로그에 이유를 남기고 건너뛴다.
