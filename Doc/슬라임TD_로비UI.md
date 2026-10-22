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
 ├─ WaveScrollView + WaveScrollPresenter (EntryPoint)    최고 웨이브 → 웨이브 맵
 ├─ PopupService (EntryPoint)   로비 팝업
 └─ HunterCardView + HunterCardPresenter (EntryPoint)    헌터 카드 목록, 정렬, 상세 팝업 열기
```

| 클래스 | 위치 | 역할 |
|---|---|---|
| `LobbyTabs` | `Scripts/Lobby` | 탭 버튼 ↔ 패널 전환. 잠긴 탭은 선택 불가 |
| `LobbyHeader` | 〃 | 골드/보석 `CurrencyView` 초기화 (메서드 주입) |
| `BattleView` | `Scripts/Lobby/BattleView` | 전투 탭. `WaveScrollSlot`만 노출한다 |
| `WaveScrollView` / `WaveScrollPresenter` | `Scripts/Lobby/WaveScroll` | 웨이브 진행도 맵 (아래) |
| `HunterCardView` / `HunterCardPresenter` / `HunterCard` / `SortButton` | `Scripts/Lobby/HunterView` | 헌터 탭. 카드는 레벨순/등급순 4단계 정렬 |
| `HunterDetailPopup` / `HunterDetailPresenter` | `Scripts/Lobby/HunterDetailPopup` | 헌터 상세 팝업. 레벨업/해금/MAX 버튼 |
| `CurrencyView` / `CurrencyViewPresenter` | `Scripts/Common` | 재화 칩(골드/보석/카드). 값 구독과 클릭 이벤트 발행 |

- `CurrencyView`은 클릭하면 `CurrencyViewClickedEvent(CurrencyType)`를 MessagePipe로 발행한다. 상점이 생기면 이 이벤트를 받아 연다 (`// TODO` 상태)
- 헌터 카드는 `hunter.Data.Enabled`인 헌터만 만든다. 레벨/해금/카드/골드가 바뀌면 카드를 갱신한다
- 로비에서 게임 씬으로 넘어가는 코드는 현재 없다 (`BtnStart`, `BtnDevGame1`, `BtnDevTest` 오브젝트는 남아 있지만 연결된 스크립트가 없다). 게임 → 로비 복귀만 `TransitionAsync`로 구현돼 있다
- `LobbyProfile`, `LobbyTab`, `Currency*` 프리팹은 `PrefabKeys`에 등록돼 있다. `LobbyProfile`의 구성과 쓰임은 이 문서에 반영하지 못했다

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

메뉴 `SlimeTD > Wave > Build Wave Map Forest`가 `WaveMapBuilder`로 `Assets/Prefabs/Lobby/WaveForest.prefab`을 만든다. 실행할 때마다 소품 배치를 새로 뽑아 기존 프리팹을 덮어쓴다 (시드는 로그에 남는다).

- 레이어 순서: `LayerGrass`(풀밭) → `LayerPath`(흙길) → `LayerProps`(소품). 풀밭은 중앙에서 화면 가장자리까지 채우고 흙길을 위에 얹는다
- 흙길 이미지의 투명 여백을 계산해 실제 흙 폭이 `PathVisibleWidth`(460)가 되도록 배율을 잡는다. 세로 타일은 `TileOverlap`만큼 겹쳐 이음새를 숨긴다
- 소품 배치: 나무/바위/작은바위는 좌우 구분 없이 Y 간격 450px 이상 띄운다. 가까우면 풀/꽃으로 바꾸고, 같은 소품 연속은 피한다. 웨이브 숫자 ±90px 안은 비우고, 일부는 확률적으로 건너뛴다
- 콘텐츠 높이는 최대 웨이브 위치 + `TopMargin`(760). 상단 HUD에 완료 표시가 가리지 않게 하는 여백이다
- 스프라이트 임포트 설정(Sprite, 알파 투명, 밉맵 끔, 최대 4096)을 빌더가 자동으로 맞춘다

아트는 `Assets/Art/UI/Lobby/WaveMap/`에 있다 (`Path_Dirt`, `Bg_Grass_Left/Right`, `Prop_*`). 풀밭 이미지의 안쪽 가장자리에 진한 초록 외곽선이 있어 흙길과의 경계선이 된다. 깃발 아이콘은 Layer Lab 에셋(`Item_Flag_01_Finish`)이다.

## 미구현 / 후속

- 로비에서 게임 씬을 시작하는 버튼과 진입 흐름
- 상점 (`CurrencyViewClickedEvent` 수신)
- 난이도 시스템 (웨이브 맵 위쪽 확장 지점)
