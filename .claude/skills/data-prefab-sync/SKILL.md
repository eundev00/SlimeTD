---
name: data-prefab-sync
description: 기획 데이터(SlimeTD-Data 시트)가 이름으로 참조하는 프리팹을 추가, 이름 변경, 삭제, 수정할 때 사용. 스킬 연출(vfxKey), 타워·슬라임 프리팹(codeName) 등. 프리팹 수정 → 데이터 저장소 수정 → 클라 데이터 반영을 한 흐름으로 맞춘다. 단, 진행 전에 사용자에게 먼저 묻는다.
---

# 데이터가 참조하는 프리팹 추가/수정

시트는 프리팹을 **이름(키)**으로 참조한다. 프리팹 이름이 바뀌거나 새로 생기면 시트와 클라 데이터도 같이 맞춰야 하지만, 이름이 그대로인 사소한 수정까지 전체 흐름을 돌 필요는 없다.

## 0. 먼저 묻는다

바로 진행하지 않는다. 바뀌는 내용을 정리해서 사용자에게 고르게 한다.

| 선택 | 해당하는 경우 | 하는 일 |
|---|---|---|
| **프리팹만** | 이름은 그대로, 모양/파티클/위치/수치만 수정 | 프리팹만 고친다. 데이터는 건드리지 않는다 |
| **전체 흐름** | 이름 변경, 새 프리팹을 데이터에 연결, 삭제 | 아래 1~5단계 |

물을 때 함께 알린다: 바뀌는 키, 참조하는 시트 행(예: Skills.csv 20301~20303), 영향을 받는 클라 에셋.

## 시트가 프리팹을 참조하는 곳

| 시트.열 | 프리팹 위치 | 클라 반영 대상 |
|---|---|---|
| Skills.vfxKey | `Assets/Prefabs/Game/Vfx/<키>.prefab` | `SkillCatalog.asset`의 `_vfxKey` |
| Towers.codeName | `Assets/Prefabs/Game/Characters/<codeName>.prefab` (`TowerPrefabProvider`가 이름으로 로드) | `TowerData_<codeName>.asset`의 `_codeName`, 에셋 파일·폴더 이름 |
| Slimes.codeName | `Assets/Prefabs/Game/Slime/<codeName>.prefab` (`SlimePrefabProvider`가 이름으로 로드) | `SlimeData_<codeName>.asset`의 `_codeName`, 에셋 파일 이름 |
| Stages.mapKey | `Assets/Prefabs/Game/Map/<mapKey>.prefab` (`GameInitiator`가 이름으로 로드) | `Stage_<codeName>.asset`의 `_mapKey` |

새로 생기는 참조 열이 있으면 이 표에 추가한다. 투사체 프리팹(`ProjectileLauncher._projectilePrefab`)처럼 인스펙터로 연결하는 것은 시트가 참조하지 않으므로 이 스킬 대상이 아니다.

## 전체 흐름

### 1. 프리팹

- **이름 변경**: `.prefab`과 `.prefab.meta`를 **같이** 옮긴다(파일 이름 변경). meta의 guid가 유지돼 프리팹을 guid로 참조하는 곳(씬, 다른 프리팹, SO)은 깨지지 않는다. 프리팹 루트의 `m_Name`도 새 이름으로 바꾼다
- **새로 만들기**: 기존 프리팹을 복사할 때는 Unity에서 `AssetDatabase.CopyAsset`처럼 새 guid가 생기게 한다 (meta를 그대로 복사하면 guid가 겹친다)
- **삭제**: 시트에서 참조를 먼저 지운 뒤 삭제한다
- 이름 규칙: 영문, 기존 이름 패턴을 따른다 (스킬 연출은 `Vfx<대상><모양>`, 예: `VfxKnightSlash`)
- 써드파티 에셋을 쓸 때는 프로젝트로 복사한 사본을 쓴다

### 2. 프리팹 키

`Assets/Scripts/Services/ResourceService/PrefabKeys.cs`를 맞춘다. 상수와 경로 두 줄을 알파벳 순서 위치에 고친다. 마지막에 Unity를 열 때 `-executeMethod ResourceKeysGenerator.CreatePrefabKeys`로 다시 만들어 손으로 고친 내용과 같은지 확인한다.

### 3. 데이터 저장소

`C:\Projects\SlimeTD-Data`의 `CLAUDE.md` 작업 순서를 따른다 (서버 세션도 같이 커밋한다).

1. `git fetch` 후 원격에만 있는 커밋과 로컬 미커밋 변경 확인, `git pull --ff-only`
2. 시트의 키를 고친다. CSV는 UTF-8 BOM, CRLF를 유지한다 (PowerShell `UTF8Encoding($true)`로 쓴다). 문자열 치환을 쓸 때는 다른 행이 같이 바뀌지 않게 열 경계(`,키,`)나 행 ID로 범위를 좁히고, `git diff`로 바뀐 행이 의도한 행뿐인지 확인한다
3. 열 설명이나 예시에 옛 이름이 있으면 `Sheets/README.md`도 고친다
4. `check-sheets` 스킬(`.claude/skills/check-sheets/SKILL.md`) 검사를 하고 오류가 있으면 커밋하지 않고 보고한다
5. 푸시 전에 다시 `git fetch`로 원격 새 커밋이 없는지 확인하고 커밋, 푸시한다

### 4. 클라 데이터

`sheets-to-so` 스킬로 바뀐 시트를 반영한다. 키 이름만 바뀐 경우 해당 에셋의 값만 고치면 된다 (예: `SkillCatalog.asset`의 `_vfxKey`).

### 5. 확인

- 프로젝트 안에서 옛 이름 검색 (`.cs`, `.asset`, `.prefab`, `.unity`, `.md`, 데이터 저장소 시트) → 남은 곳이 없어야 한다
- Unity 배치 모드로 열어 `PrefabKeys` 재생성, 로그에 import/컴파일 오류가 없는지 확인. 배치 모드 부작용(`ProjectSettings/NotchSolutionSettings.asset`, `ProjectSettings.asset`, `QualitySettings.asset`, `.vsconfig`)은 되돌린다
- 시트와 클라 에셋 값이 맞는지 비교한다
- 결과를 보고한다. 클라 커밋은 사용자가 요청할 때 한다 (데이터 저장소는 3단계에서 커밋)
