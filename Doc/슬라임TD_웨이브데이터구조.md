# 슬라임TD 웨이브 데이터 구조

웨이브 데이터의 타입·필드·계산식을 정리한 문서다. 구간별 수치, 전체 웨이브 표, 젤리, 밸런싱 근거는 [웨이브 기획](슬라임TD_웨이브기획.md)을 참고한다.

수치의 원본은 데이터 저장소(`C:\Projects\SlimeTD-Data`)의 `Sheets/Slimes.csv`, `Sheets/Stages.csv`, `Sheets/Waves/<codeName>.csv`다. 아래 SO는 서버 연결 전까지 쓰는 임시 데이터이고, `sheets-to-so` 스킬로 시트를 반영한다. 열 설명은 데이터 저장소 `Sheets/README.md`에 있다.

---

## 1. 타입 정의

```
SlimeDataBase (abstract SO, CreateAssetMenu 없음)
 ├─ _slimeId          (Slimes.slimeId)
 ├─ _prefab           (인스펙터에서 연결)
 ├─ _baseHealth
 ├─ _baseSpeed
 ├─ _lifeCost
 ├─ _renderGroup
 └─ _jellyReward

SlimeData : SlimeDataBase        (kind = Normal)

BossSlimeData : SlimeDataBase    (kind = Boss)
 └─ _instantGameOver

StageData (SO, 스테이지마다 하나: Assets/Datas/Stages/Stage_<codeName>.asset)
 ├─ _stageId, _codeName, _mode, _difficulty, _displayName, _enabled
 ├─ _startingLife, _startingJelly
 ├─ _spawnInterval, _interWaveDelay, _normalHpGrowth
 └─ _waves : List<StageWave>          (1웨이브부터 순서대로)

StageWave ([Serializable])
 └─ _groups : List<StageWaveGroup>    (웨이브 안에서 나오는 순서대로)

StageWaveGroup ([Serializable])
 ├─ _slime : SlimeDataBase
 ├─ _count
 └─ _delay                            (이 무리가 나오기 전 추가 대기)
```

`SlimeData`는 concrete를 유지한다. abstract로 바꾸면 기존 일반 슬라임 에셋이 전부 깨진다.

---

## 2. 웨이브 진행

- 웨이브 시작 전 `_interWaveDelay`만큼 기다린다
- 무리를 순서대로 내보낸다. 무리 앞에 `_delay`, 무리 안 슬라임은 `_spawnInterval` 간격
- 스폰이 끝나면 즉시 다음 웨이브로 넘어간다. 이전 웨이브 슬라임이 남아 있어도 무관하다. 최종 웨이브만 예외로, 슬라임이 전부 처리된 후 스테이지 클리어를 판정한다
- 보스 웨이브는 따로 표시하지 않는다. 보스가 든 무리가 있는 웨이브가 보스 웨이브다. 지금 `SingleNormal`은 10의 배수 웨이브에 일반 슬라임이 먼저 나오고, 보스 무리의 `_delay`(0.5~1초) 뒤에 보스가 나온다

최종 웨이브 = `_waves` 개수 (`FinalWave`). 로비 웨이브 진행도 맵의 최대 웨이브도 이 값이다 ([로비 UI](슬라임TD_로비UI.md) 참고). 게임 씬은 지금 `Stage_SingleNormal`을 읽는다 (스테이지 선택은 아직 없다).

---

## 3. 계산식

```
일반 HP    = slime._baseHealth × (1 + _normalHpGrowth)^(N - 1)
보스 HP    = boss._baseHealth
처치 젤리   = _jellyReward
```

`(N - 1)`을 지수로 쓰므로 1웨이브에서 배율이 1이 되어 `_baseHealth` 그대로 나온다. 구간이 바뀌면 슬라임의 `_baseHealth`가 바뀌면서 HP가 한 번 더 뛴다.

웨이브 클리어 젤리는 없다. 젤리는 처치 보상만 지급한다.

---

## 4. 패배 조건

- 경로 끝 도달 시 `_lifeCost`만큼 라이프 차감. 라이프 0이면 게임오버
- `_instantGameOver`가 켜진 보스는 도달 즉시 게임오버. 지금은 모든 보스가 꺼져 있고 `_lifeCost` 10을 차감한다
- 시작 라이프와 시작 젤리는 스테이지(`_startingLife`, `_startingJelly`) 값이다

---

## 5. 수치

초기 수치, HP 표, 보스 HP, 젤리, 밸런싱 노브와 근거는 [웨이브 기획](슬라임TD_웨이브기획.md) 참고.
