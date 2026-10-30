# 슬라임TD — 로비 UI

로비는 `LobbyScene`(Bootstrap 위에 Additive)에 있고 `LobbyLifetimeScope`가 씬 단위 서비스를 등록한다. 화면 규칙(1080×1920, 세로 고정, CanvasScaler Match 0)은 [CLAUDE.md](../CLAUDE.md)를 따른다.

## 구조

```
LobbyScene
 └─ LobbyMain 프리팹 ── TabBar(LobbyTabs) + Header(LobbyHeader)
     ├─ BattleTab (BattleView)   WaveScroll 자리에 WaveForest 프리팹이 들어간다
     └─ 헌터 탭 (HunterCardView)
LobbyLifetimeScope  (_popupRoot, _waveTable을 인스펙터에서 받는다)
 ├─ LobbyHeader                 재화 칩 초기화
 ├─ BattleView + BattlePresenter (EntryPoint)            전투 시작 버튼 → 게임 씬 전환
 ├─ WaveScrollView + WaveScrollPresenter (EntryPoint)    최고 웨이브 → 웨이브 맵
 ├─ PopupService (EntryPoint)   로비 팝업
 └─ HunterCardView + HunterCardPresenter (EntryPoint)    헌터 카드 목록, 정렬, 상세 팝업 열기
```

| 클래스 | 위치 | 역할 |
|---|---|---|
| `LobbyTabs` | `Scripts/Lobby` | 탭 버튼 ↔ 패널 전환. 잠긴 탭은 선택 불가 |
| `LobbyHeader` | 〃 | 골드/보석 `CurrencyView` 초기화 (메서드 주입) |
| `BattleView` / `BattlePresenter` | `Scripts/Lobby/BattleView` | 전투 탭. `WaveScrollSlot`과 시작 버튼(`StartClicked`, `SetStartInteractable`)을 노출하고, Presenter가 `TransitionAsync(Lobby, Game)`으로 게임 씬에 들어간다 |
| `WaveScrollView` / `WaveScrollPresenter` | `Scripts/Lobby/WaveScroll` | 웨이브 진행도 맵 (아래) |
| `HunterCardView` / `HunterCardPresenter` / `HunterCard` / `SortButton` | `Scripts/Lobby/HunterView` | 헌터 탭. 카드는 레벨순/등급순 4단계 정렬 |
| `HunterDetailPopup` / `HunterDetailPresenter` | `Scripts/Lobby/HunterDetailPopup` | 헌터 상세 팝업. 레벨업/해금/MAX 버튼, 스킬 슬롯 |
| `CurrencyView` / `CurrencyViewPresenter` | `Scripts/Common` | 재화 칩(골드/보석/카드). 값 구독과 클릭 이벤트 발행 |
| `SkillSlotGroup` / `SkillSlotItem` | `Scripts/Common` | 스킬 슬롯 공통 UI. 헌터 상세 팝업과 인게임 타워 정보가 같이 쓴다 (아래) |

- `CurrencyView`은 클릭하면 `CurrencyViewClickedEvent(CurrencyType)`를 MessagePipe로 발행한다. 상점이 생기면 이 이벤트를 받아 연다 (`// TODO` 상태)
- 헌터 카드는 `hunter.Data.Enabled`인 헌터만 만든다. 레벨/해금/카드/골드가 바뀌면 카드를 갱신한다
- 전투 시작: `BattleView`의 시작 버튼 → `BattlePresenter`가 버튼을 잠그고 `SceneNames.Game`으로 전환한다. 게임 → 로비 복귀는 `TopHudPresenter`와 `GameResultPresenter`가 한다. 게임1/테스트 씬으로 가는 DEV 버튼은 지금 연결되어 있지 않다
- 헌터 카드와 상세 팝업은 `ThumbnailAtlas`의 `Thumb_{codeName}` 스프라이트를 쓴다 (`TowerData.ThumbnailName`). 카드와 팝업이 같은 스프라이트를 쓴다. 잠긴 헌터는 어두운 실루엣으로 칠한다 (`_lockedThumbnailColor`)
- 썸네일은 `Assets/Art/UI/Thumbnail/Thumb_{코드명}.png` 한 장(128x128, 투명 PNG)이다. 캐릭터는 92%로 줄여 가운데 두고 진갈색(`#2A1D17`) 3px 아웃라인을 두른다. `ThumbnailAtlas`가 폴더 전체를 묶으므로 이름 규칙만 맞추면 따로 연결할 것이 없다
- `LobbyProfile`, `LobbyTab`, `Currency*`, `GradeBadge` 프리팹은 `PrefabKeys`에 등록돼 있다. `LobbyProfile`의 구성과 쓰임은 이 문서에 반영하지 못했다. 프리팹 경로를 옮기면 메뉴 `Tools > SlimeTD > Resource Keys > Create Prefab Keys`로 키를 다시 만든다

## 스킬 슬롯 (공통)

스킬 계열(업그레이드로 이어진 스킬들) 하나가 슬롯 하나다. 헌터 상세 팝업의 `SkillGroup`에 `SkillSlotGroup`이 붙어 있고, 공통 프리팹 `Assets/Prefabs/Common/SkillSlotItem.prefab` 3개를 담고 있다. 인게임 타워 정보 창(미구현)도 같은 두 부품을 붙여 쓴다.

- Presenter는 `SkillSlotGroup.Show(catalog, atlas, towerId, level)`만 부른다. 계열 목록은 `SkillCatalog.GetSlots`가 만든다
- 계열 수만큼 슬롯을 켜고 나머지는 끈다. 계열이 슬롯보다 많으면 로그를 남긴다. 슬롯이 더 필요하면 `SkillSlotItem` 프리팹을 하나 더 넣고 `SkillSlotGroup._slots` 배열에 연결한다 (가로 폭: 슬롯 160, 간격 30, 팝업 내용 900이라 5개는 넘친다)
- 표시 규칙

| 상태 | 아이콘 | 표시 |
|---|---|---|
| 잠김 | 계열 첫 스킬 | 반투명 검은 딤(`Dim`, 알파 0.45) + 자물쇠 |
| 해금, 다음 단계 있음 | 현재 단계 | 화살표 |
| 해금, 마지막 단계 | 현재 단계 | 없음 |

- 슬롯에는 레벨 글자를 넣지 않는다. 해금 레벨은 툴팁에서 보여 준다

- 보유하지 않은 헌터는 레벨 0으로 넘겨 모든 슬롯을 잠김으로 보여 준다
- 아이콘은 `SkillIconAtlas`에서 `TowerSkillData.IconName`으로 꺼낸다. `GetSprite`는 부를 때마다 복제본을 만들어서 `SkillSlotGroup`이 이름별로 한 번만 받아 두고 파괴될 때 정리한다
- `SkillCatalog`과 아틀라스는 Presenter가 `IResourceLoadService`로 읽는다. 전역 서비스가 캐시하므로 다시 열어도 다시 읽지 않는다
- 툴팁(슬롯 클릭)은 아직 없다

## 웨이브 진행도 맵 (전투 탭)

운빨 TD처럼 세로로 스크롤되는 진행도 맵이다. 좌우 버튼으로 맵을 고르지 않는다.

- 10단위(10/20/30/40/50)로 숫자를 표시한다. 시작점(0)은 표시하지 않는다
- 최고 웨이브 위치에 플레이어 마커와 태그를 놓는다. 기록이 0이면 "도전 시작!", 아니면 "최고 웨이브 N"
- 맨 위에 깃발과 "웨이브 완료" 표시가 있고 스크롤은 거기서 끝난다. 최고 웨이브가 최대 웨이브(`WaveTableData.MaxWave`)에 닿으면 켜진다
- 난이도 시스템이 미정이라 "다음 난이도" 버튼은 없다
- 로드 시 최고 웨이브가 화면 아래 40% 지점에 오도록 스크롤한다
- 최대 웨이브는 `LobbyLifetimeScope._waveTable`(`WaveTableData`)에서 읽는다. 뷰는 값을 넘겨받기만 한다

### 최고 웨이브 기록 규칙

`GameplayService`가 `IPlayerDataService.ReportWaveReached(n)`을 호출한다. 기존 기록보다 클 때만 갱신·저장하고, 로비를 거치지 않아 초기화되지 않았으면 무시한다(빈 헌터 목록으로 저장이 덮이는 것을 막는다).

| 시점 | 기록 값 |
|---|---|
| `WaveStarted` (일반 웨이브) | 그 웨이브 번호 |
| `WaveStarted` (마지막 웨이브) | 번호 − 1 |
| `StageCleared` | 최대 웨이브 |

웨이브가 겹쳐 웨이브별 클리어 이벤트가 없어서 "시작"을 도달로 본다. 마지막 웨이브는 클리어해야 최대치가 찍혀 완료 표시가 켜진다.

## 웨이브 맵 프리팹 제작

메뉴 `SlimeTD > Wave > Build Wave Map Forest`가 `WaveMapBuilder`로 `Assets/Prefabs/Lobby/LobbyMain/WaveForest.prefab`을 만든다. 실행할 때마다 소품 배치를 새로 뽑아 기존 프리팹을 덮어쓴다 (시드는 로그에 남는다).

- 레이어 순서: `LayerGrass`(풀밭) → `LayerPath`(흙길) → `LayerProps`(소품). 풀밭은 중앙에서 화면 가장자리까지 채우고 흙길을 위에 얹는다
- 흙길 이미지의 투명 여백을 계산해 실제 흙 폭이 `PathVisibleWidth`(460)가 되도록 배율을 잡는다. 세로 타일은 `TileOverlap`만큼 겹쳐 이음새를 숨긴다
- 소품 배치: 나무/바위/작은바위는 좌우 구분 없이 Y 간격 450px 이상 띄운다. 가까우면 풀/꽃으로 바꾸고, 같은 소품 연속은 피한다. 웨이브 숫자 ±90px 안은 비우고, 일부는 확률적으로 건너뛴다
- 콘텐츠 높이는 최대 웨이브 위치 + `TopMargin`(760). 상단 HUD에 완료 표시가 가리지 않게 하는 여백이다
- 스프라이트 임포트 설정(Sprite, 알파 투명, 밉맵 끔, 최대 4096)을 빌더가 자동으로 맞춘다

아트는 `Assets/Art/UI/Lobby/WaveMap/`에 있다 (`Path_Dirt`, `Bg_Grass_Left/Right`, `Prop_*`). 풀밭 이미지의 안쪽 가장자리에 진한 초록 외곽선이 있어 흙길과의 경계선이 된다. 깃발 아이콘은 Layer Lab 에셋(`Item_Flag_01_Finish`)이다.

## 미구현 / 후속

- 상점 (`CurrencyViewClickedEvent` 수신)
- 스킬 슬롯 툴팁 (이름, 설명, 해금 레벨)
- 난이도 시스템 (웨이브 맵 위쪽 확장 지점)
