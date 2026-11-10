---
name: sheets-to-so
description: SlimeTD-Data 저장소의 기획 시트(CSV)를 클라이언트 ScriptableObject(SO)에 반영한다. "시트 반영", "데이터 적용", "SO 만들어", "CSV를 SO로" 같은 요청이나, 데이터 저장소 시트를 고친 뒤 클라이언트에 적용할 때 사용.
---

# 시트 → SO 반영

기획 데이터 원본은 클라이언트 옆 저장소 `C:\Projects\SlimeTD-Data\Sheets`다. 클라이언트 프로젝트 안에는 CSV 복사본을 두지 않는다. 클라이언트 SO는 **서버 연결 전까지 쓰는 임시 데이터**이고, 서버가 생기면 서버에서 받는 데이터로 바뀐다. 그래서 변환 도구는 에디터 메뉴로 만들지 않고 이 스킬이 임시 스크립트로 실행한다.

## 실행 순서

1. 데이터 저장소를 최신으로 받는다 (`git -C C:\Projects\SlimeTD-Data fetch` 후 `pull --ff-only`). 서버 세션도 같은 저장소에 커밋한다. 시트를 고쳐야 하면 저장소의 `CLAUDE.md` 작업 순서(작업 전 pull, 커밋 전 시트 검사, 푸시 전 fetch)를 따른다
2. `TmpSheetsToSo.cs.txt`(이 폴더)를 `Assets/Scripts/Editor/TmpSheetsToSo.cs`로 복사한다
3. 배치 모드로 실행한다 (git이 PATH에 없으므로 GitHub Desktop의 git 경로를 PATH 앞에 붙인다)
   ```
   Unity.exe -batchmode -quit -projectPath C:\Projects\SlimeTD -executeMethod TmpSheetsToSo.Run -logFile <로그>
   ```
4. 로그에서 `[TmpSheetsToSo]`, `[TowerSheetImporter]`, `[SkillSheetImporter]`, `[HunterCardGachaSheetImporter]`, `error CS`, `Exception`을 확인한다. 마지막 줄 `완료, 오류 N개`가 0이 아니면 원인을 보고한다
5. 정리
   - `Assets/Scripts/Editor/TmpSheetsToSo.cs`와 `.meta`를 지운다
   - 배치 모드 부작용을 되돌린다: `ProjectSettings/NotchSolutionSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/QualitySettings.asset`, `.vsconfig`
6. `git diff --stat`으로 바뀐 에셋을 확인하고, 시트에 없는 변경이 섞이지 않았는지 본다. 커밋은 사용자가 요청할 때 한다

## 무엇이 어디로 가는가

| 시트 | SO | 비고 |
|---|---|---|
| Grades, Towers, LevelUpCosts, TowerProjectiles | `TowerData_<codeName>`(공격 방식, 투사체 속도·판정 반경·수명 포함), `TowerCatalog`, `GradeStyle` 이름 | 기존 `TowerSheetImporter.Import` 호출. 타워 프리팹은 `codeName`과 같은 이름의 프리팹을 로드하고, 투사체 모양은 그 프리팹의 `ProjectileLauncher._projectilePrefab`이 정한다 (시트에 넣지 않는다) |
| Skills | `SkillCatalog` | 기존 `SkillSheetImporter.Import` 호출 |
| HunterCardGachaPacks, HunterCardGachaSettings, Grades.gachaWeight | `HunterCardGachaConfig` | 기존 `HunterCardGachaSheetImporter.Import` 호출 |
| Slimes | `Assets/Datas/Slimes/<codeName>.asset` (`SlimeData` / `BossSlimeData`) | 없으면 만든다. 프리팹은 인스펙터에서 연결 |
| Stages + Waves/<codeName>.csv | `Assets/Datas/Stages/Stage_<codeName>.asset` (`StageData`) | 웨이브 무리는 슬라임 에셋을 직접 참조 |
| GameConstants | `GameConfig` 소환/디버프 값 | |
| NewAccount (+ Items의 codeName) | `GameConfig` 시작 골드/보석 | 서버 전용 시트지만 서버 전까지 로컬 계정 생성에 쓴다. Gold/Gem 외 아이템은 로그만 남긴다 |
| Enums | `Assets/Scripts/Data/SheetEnums.cs` | 자동 변환하지 않는다. 이넘이 바뀌면 이 파일을 손으로 맞춘다 (서버 프로토콜 코드가 생기면 그쪽 이넘으로 교체 예정) |
| Items | 없음 | 클라이언트가 아직 아이템을 쓰지 않는다 |

실행 끝에 `DataKeys`를 다시 만들고, 로비 씬 `LobbyLifetimeScope._stage`에 `Stage_SingleNormal`을 연결하고, TMP 폰트 글자 정리(`TmpFontTrimmer.Trim`)를 한다.

## 시트 구조가 바뀌었을 때

- 새 열·시트가 클라이언트에 필요하면 SO에 필드를 추가하고 `TmpSheetsToSo.cs.txt`에 매핑을 추가한다
- 이넘 값이 바뀌면 `SheetEnums.cs`를 먼저 맞춘다. 기존 이넘(`SkillTrigger`, `SkillEffectType`, `VfxAnchor`, `SlimeRenderGroup`, `TowerAttackType`)은 에셋에 숫자로 저장돼 있으므로 값을 바꾸면 에셋도 다시 반영해야 한다
- 열 설명은 데이터 저장소 `Sheets/README.md`에 있다
