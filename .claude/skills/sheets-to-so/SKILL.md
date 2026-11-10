---
name: sheets-to-so
description: SlimeTD-Data 저장소의 기획 시트(CSV)를 클라이언트 ScriptableObject(SO) 에셋에 반영한다. "시트 반영", "데이터 적용", "SO 만들어", "CSV를 SO로" 같은 요청이나, 데이터 저장소 시트를 고친 뒤 클라이언트에 적용할 때 사용.
---

# 시트 → SO 반영 (코드 없이)

기획 데이터 원본은 클라이언트 옆 저장소 `C:\Projects\SlimeTD-Data\Sheets`다. 클라이언트 프로젝트 안에는 CSV 복사본을 두지 않는다. 클라이언트 SO는 **서버 연결 전까지 쓰는 임시 데이터**이고, 서버가 생기면 서버에서 받는 데이터로 바뀐다.

**변환 코드를 만들지 않는다.** 에디터 메뉴, 임포터, 임시 C# 스크립트 모두 쓰지 않는다. CSV를 읽고 `.asset` 파일(YAML 텍스트)을 직접 고친다. 열 설명과 값 규칙은 데이터 저장소 `Sheets/README.md`가 기준이다.

## 실행 순서

1. **데이터 저장소를 최신으로 받는다**: `git -C C:\Projects\SlimeTD-Data fetch` 후 `pull --ff-only`. 서버 세션도 같은 저장소에 커밋한다. 시트를 고쳐야 하면 저장소 `CLAUDE.md`의 작업 순서(작업 전 pull, 커밋 전 시트 검사, 푸시 전 fetch)를 따른다
2. 반영할 시트를 읽고 아래 "매핑"대로 대상 `.asset`의 값을 고친다. 바뀐 값만 고치고 나머지 줄(프리팹 참조, 색, 디버그 값 등 시트에 없는 필드)은 건드리지 않는다
3. 시트와 에셋을 다시 비교해 빠진 행·필드가 없는지 확인한다 (행 수, ID 목록, 바뀐 값)
4. 에셋을 새로 만들었으면 `DataKeys`를 다시 만든다 (`Unity.exe -batchmode -quit -projectPath C:\Projects\SlimeTD -executeMethod ResourceKeysGenerator.CreateDataKeys`)
5. 화면에 보이는 한글(이름, 설명)이 바뀌었으면 TMP 폰트 글자 정리를 한다 (`-executeMethod TmpFontTrimmer.Trim`)
6. Unity를 배치 모드로 한 번 열어(`-batchmode -quit`, 4·5단계와 같이 해도 된다) 로그에 에셋 읽기 오류가 없는지 확인한다
7. 배치 모드 부작용을 되돌린다: `ProjectSettings/NotchSolutionSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/QualitySettings.asset`, `.vsconfig`
8. `git diff`로 바뀐 줄이 시트 변경과 일치하는지 보고한다. 커밋은 사용자가 요청할 때 한다

## YAML 작성 규칙

- 파일 앞 12줄(`%YAML`, `MonoBehaviour:`, `m_Script` 등)은 그대로 둔다. 필드는 2칸 들여쓰기 `_필드명: 값`
- **한글 문자열**은 큰따옴표 안에 `\uXXXX` 이스케이프로 쓴다 (예: 궁수 → `"\uAD81\uC218"`). 공백과 숫자, `%`는 그대로 쓴다. Unity가 긴 문자열을 줄바꿈해 저장한 경우가 있는데, 한 줄로 써도 된다
- 빈 문자열은 `_vfxKey: ` (콜론 뒤 공백, 값 없음)
- bool은 `1`/`0`, 이넘은 **정수 값**(`Assets/Scripts/Data/SheetEnums.cs`의 값). 시트에는 이름이 적혀 있다
- 실수는 시트 값을 그대로 쓴다 (`0.15`, `2.5`)
- **int 배열**(`_levelUpCost._cards`, `_gold`)은 hex 한 줄이다. 값마다 int32 리틀엔디언 8자리를 이어 붙인다 (5 → `05000000`, 300 → `2c010000`)
- 리스트는 `- ` 항목. 객체 리스트는 첫 필드 앞에 `- `, 나머지 필드는 두 칸 더 들여쓴다 (기존 파일 모양을 따른다)
- 에셋 참조는 `{fileID: 11400000, guid: <대상 .asset.meta의 guid>, type: 2}`
- CRLF/LF는 원래 파일을 따른다. Unity 에셋은 LF다

### 새 에셋 만들기

1. 같은 종류의 기존 에셋을 복사해 시작한다 (헤더와 필드 순서를 그대로 쓰기 위해)
2. `m_Name`을 파일 이름과 같게 바꾼다
3. `.meta` 파일을 새로 만든다. guid는 새로 만든 32자리 소문자 hex. 기존 `.asset.meta`의 형식(`fileFormatVersion: 2`, `guid:`, `NativeFormatImporter:` 블록)을 복사하고 guid만 바꾼다
4. `m_Script`의 guid는 클래스 스크립트 `.cs.meta`의 guid다

| 클래스 | 스크립트 guid |
|---|---|
| TowerData | `5a564b8603519415aa919cd3e7532039` |
| SlimeData | `7a1c4e9b2d3f4a5b8c6d7e8f9a0b1c2d` |
| BossSlimeData | `9c3e5a71b8d24f60a417e9c2d5b306f8` |
| StageData | `4cfaff4b83cf63746a1581dd24eed0dc` |

## 매핑

### Towers.csv + Grades.csv + LevelUpCosts.csv + TowerProjectiles.csv → `Assets/Datas/Characters/<codeName>/TowerData_<codeName>.asset`

| 필드 | 값 |
|---|---|
| `_id` | towerId |
| `_gradeId` | gradeId |
| `_enabled` | enabled (비우면 1) |
| `_codeName` | codeName |
| `_towerName` | name (한글 이스케이프) |
| `_attackType` | attackType → `TowerAttackType` 값 (Melee 0, Projectile 1, Beam 2) |
| `_attackRange`, `_attackCooldown`, `_critChance`, `_critBonus` | 같은 이름의 열 |
| `_baseAttack`, `_attackGrowthRate` | 그 등급의 Grades.csv `baseAttack`, `attackGrowthRate` |
| `_projectileSpeed`, `_projectileHitRadius`, `_projectileLifetime` | TowerProjectiles.csv의 `speed`, `hitRadius`, `lifetime`. 행이 없으면 0 |
| `_startUnlocked` | startUnlocked (비우면 0) |
| `_unlockGemPrice` | unlockGemPrice (비우면 0) |
| `_inGacha` | inGacha (비우면 1) |
| `_levelUpCost._cards`, `_gold` | 그 등급의 LevelUpCosts.csv를 level 1~14 순서로 14개 (hex 배열) |

- 타워 프리팹은 시트에 없다. `codeName`과 같은 이름의 프리팹을 `TowerPrefabProvider`가 로드하고, 투사체 모양은 그 프리팹의 `ProjectileLauncher._projectilePrefab`이 정한다
- 타워를 새로 추가하면 에셋을 만들고 **`TowerCatalog.asset`의 `_towers`에 towerId 순서로 참조를 추가**한다. 프리팹(`Assets/Prefabs/Game/Characters/<codeName>.prefab`)이 없으면 사용자에게 알린다

### Grades.csv `name` → `Assets/Datas/GradeStyle.asset`

`_colors[gradeId - 1]._name`. 색(`_fill`, `_border`)은 시트에 없으니 건드리지 않는다.

### Skills.csv → `Assets/Datas/SkillCatalog.asset` `_skills`

정렬: towerId → unlockLevel(비우면 1) → skillId. 행마다:

| 필드 | 값 |
|---|---|
| `_id` | skillId |
| `_displayName`, `_description` | name, description (한글 이스케이프) |
| `_towerId` | towerId |
| `_unlockLevel` | unlockLevel (비우면 1) |
| `_upgradeOfId` | upgradeOf (비우면 0) |
| `_trigger` | `SkillTrigger` 값 |
| `_nth`, `_chance`, `_cooldown` | 같은 이름의 열 (비우면 0) |
| `_effects` | 항목 1개: `_type` = `SkillEffectType` 값, `_value` = value의 숫자, `_valueType` = value에 `%`가 있으면 1(Percent) 없으면 0(Fixed), `_duration` = duration (비우면 0) |
| `_vfxKey` | vfxKey (비우면 빈 문자열). `PrefabKeys`에 없는 키면 보고한다 |
| `_vfxAnchor` | `VfxAnchor` 값 (비우면 0 = Root) |
| `_animTrigger` | animTrigger (비우면 빈 문자열) |

허용 단위(README "effectType과 수치 해석")를 어긴 행은 반영하지 말고 보고한다.

### HunterCardGachaPacks.csv + HunterCardGachaSettings.csv + Grades.csv `gachaWeight` → `Assets/Datas/HunterCardGachaConfig.asset`

- `_packs`: 행마다 `_id` = packId, `_cardCount`, `_price`, `_guaranteeGradeId`
- `_gradeWeights`: 등급마다 `_gradeId`, `_weight` = gachaWeight
- `_lockedCardChance`, `_pityCount`, `_pityGradeId`, `_maxLevelRefundGold`: Settings의 같은 key

### Slimes.csv → `Assets/Datas/Slimes/<codeName>.asset`

kind가 `Normal`이면 `SlimeData`, `Boss`면 `BossSlimeData`.

| 필드 | 값 |
|---|---|
| `_slimeId` | slimeId |
| `_baseHealth`, `_baseSpeed`, `_lifeCost`, `_jellyReward` | 같은 이름의 열 |
| `_renderGroup` | `SlimeRenderGroup` 값 (Normal 0, Boss 1) |
| `_instantGameOver` | 보스만. instantGameOver |

`_prefab`은 인스펙터에서 연결하는 값이라 건드리지 않는다. 새 슬라임은 프리팹이 비어 있다고 사용자에게 알린다.

### Stages.csv + Waves/<codeName>.csv → `Assets/Datas/Stages/Stage_<codeName>.asset`

| 필드 | 값 |
|---|---|
| `_stageId`, `_codeName` | stageId, codeName |
| `_mode`, `_difficulty` | `GameMode`, `Difficulty` 값 |
| `_displayName` | name (한글 이스케이프) |
| `_enabled` | enabled |
| `_startingLife`, `_startingJelly`, `_spawnInterval`, `_interWaveDelay`, `_normalHpGrowth` | 같은 이름의 열 |
| `_waves` | 웨이브 1부터 순서대로 항목 하나씩. 각 항목의 `_groups`는 order 순서로 `_slime` = 그 slimeId 슬라임 에셋 참조, `_count`, `_delay`(비우면 0) |

로비 웨이브 맵은 `LobbyScene`의 `LobbyLifetimeScope._stage`가 가리키는 스테이지를 읽는다 (지금 `Stage_SingleNormal`).

### GameConstants.csv → `Assets/Datas/GameConfig.asset`

`_summonBaseCost`, `_summonCostIncrease`, `_summonGradeId`, `_slowMinSpeedRatio`, `_bossSlowScale` = 같은 이름의 key.

### NewAccount.csv (+ Items.csv codeName) → `Assets/Datas/GameConfig.asset`

서버 전용 시트지만 서버 전까지 로컬 계정 생성에 쓴다. codeName이 `Gold`인 아이템의 amount → `_initialLobbyGold`, `Gem` → `_initialGem`. 다른 아이템은 클라이언트가 아직 지급하지 못하므로 보고만 한다.

### Enums.csv → `Assets/Scripts/Data/SheetEnums.cs`

이넘을 손으로 맞춘다 (서버 프로토콜 코드가 생기면 그쪽 이넘으로 교체 예정). 기존 이넘(`SkillTrigger`, `SkillEffectType`, `VfxAnchor`, `SlimeRenderGroup`, `TowerAttackType`)은 에셋에 숫자로 저장돼 있으므로 값이 바뀌면 해당 에셋도 다시 반영해야 한다.

### Items.csv

클라이언트가 아직 아이템을 쓰지 않아 반영할 SO가 없다.
