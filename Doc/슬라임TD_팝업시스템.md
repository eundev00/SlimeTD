# 슬라임TD — 팝업 시스템

인게임 팝업을 씬마다 따로 두지 않고, 프리팹으로 분리해 `PopupService`로 열고 닫는다.

## 구조

```
GameScene / GameScene2 / GameSceneTest
 └─ PopupRoot (프리팹, 씬마다 1개)        팝업이 붙는 Canvas + 딤
GameLifetimeScope
 ├─ PopupService (EntryPoint, 구현 인터페이스 자동 등록)   열기/닫기/스택/딤/백키
 └─ GameResultPresenter (EntryPoint)           결과 이벤트 → 결과 팝업 열기
Addressables
 └─ GameResultPopup.prefab
```

| 클래스 | 위치 | 역할 |
|---|---|---|
| `PopupBase` | `Scripts/Services/PopupService` | 모든 팝업의 부모. Open/Close 애니메이터 연출, 닫기 요청, 백키 처리 |
| `PopupRoot` | 〃 | 팝업 부모 `_container`와 딤 `_dim`. 씬 LifetimeScope에 `RegisterComponentInHierarchy`로 등록 |
| `IPopupService` / `PopupService` | 〃 | 프리팹 키로 팝업 생성(`IObjectResolver.Instantiate`로 주입 포함), 재사용, 스택, 딤 위치, 백키 |
| `GameResultPopup` | `Scripts/Game/Hud/Result` | 결과 팝업. 백키 = 로비 버튼 |

## 규칙

- 팝업은 `IObjectResolver.Instantiate`로 생성되므로 `[Inject] public void Construct(...)`로 씬·프로젝트 서비스를 주입받을 수 있다. 주입은 `Awake`보다 먼저 끝난다
- 팝업은 닫히면 비활성화되고 다음에 재사용된다. 주입은 처음 생성할 때 한 번뿐이므로, 구독은 `OnOpen`에서 걸고 `OnClose`에서 해제한다
- `OpenAsync`는 열림 연출을 기다리지 않고 바로 인스턴스를 돌려준다. 받은 즉시 내용을 채운다
- 팝업이 열려도 게임은 멈추지 않는다. 딤의 레이캐스트가 팝업 아래 터치(타워 소환/선택)를 막는다
- 백키(Escape / 안드로이드 Back): 맨 위 팝업의 `OnBackPressed` 호출. 기본은 닫기, 연출 중에는 무시

## 애니메이터

- 공용 컨트롤러 하나에 상태 **`Open`**, **`Close`** 두 개만 둔다. 이름이 정확해야 한다. 트랜지션·파라미터는 필요 없다 (`Animator.Play`로 직접 재생)
- 두 클립 모두 **Loop Time 끄기**. 끝 프레임이 유지돼야 한다
- Open 클립은 끝 값이 alpha 1, scale 1이어야 한다 (연출이 끝나면 Animator를 꺼서 마지막 값이 그대로 남는다)
- Update Mode는 기본값(Normal). 일시정지 팝업이 생기면 Unscaled Time으로 바꾼다
- Animator가 없거나 상태가 없으면 연출 없이 바로 열고 닫힌다
- 팝업마다 연출을 바꾸려면 `AnimatorOverrideController`로 클립만 교체한다

## 생성된 에셋

| 에셋 | 내용 |
|---|---|
| `Assets/Animations/Popup/PopupOpen.anim` | alpha 0→1 (0.15초), scale 0.8→1.05→1 (0.25초) |
| `Assets/Animations/Popup/PopupClose.anim` | alpha 1→0, scale 1→0.9 (0.15초) |
| `Assets/Animations/Popup/PopupAnimator.controller` | 상태 `Open`(기본), `Close` |
| `Assets/Prefabs/Game/UI/Popup/PopupRoot.prefab` | Canvas(Sort Order 10, 1080×1920, Match 0) + Container + Dim(검정 60%) |
| `Assets/Prefabs/Game/UI/Popup/GameResultPopup.prefab` | 기존 씬 결과 패널을 `Panel` 자식으로 옮기고 루트에 Canvas/GraphicRaycaster/CanvasGroup/Animator/GameResultPopup 구성 |

- `Assets/Prefabs` 폴더 전체가 Addressables에 등록돼 있어, 그 아래 프리팹은 에셋 경로가 곧 주소다. 별도 등록이 필요 없다
- `GameScene`, `GameScene2`, `GameSceneTest`에서 기존 결과 패널을 삭제하고 `PopupRoot` 프리팹을 배치했다

## 남은 에디터 작업

- [ ] `GameResultPopup`의 `_titleText`, `_waveText`, `_rewardRoot` 연결 (기존 씬에서도 비어 있었다. `Panel` 아래 `Text (TMP)` 두 개가 후보)
- [ ] `Panel` 아래에 기존 패널 자체 `Dim`이 있다. `PopupRoot` 딤과 겹쳐 더 어두워지므로 하나를 정리한다 (`Panel/Dim` 삭제 또는 `_useDim` 끄기)
- [ ] 연출 수치·딤 색은 취향대로 조정

## 확인

- [ ] 게임오버 / 클리어 시 결과 팝업이 연출과 함께 열린다
- [ ] 팝업 뒤 딤이 깔리고, 딤 위를 눌러도 타워가 소환·선택되지 않는다
- [ ] 다시하기 / 로비 버튼 동작
- [ ] Escape 키(에디터) / Back 키(기기)로 로비 이동
