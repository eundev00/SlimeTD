# 슬라임TD 웨이브 데이터 구조

---

## 1. 타입 정의

```
SlimeDataBase (abstract SO, CreateAssetMenu 없음)
 ├─ _prefab
 ├─ _baseHealth
 ├─ _baseSpeed
 ├─ _lifeCost
 └─ _renderGroup

SlimeData : SlimeDataBase        (concrete 유지)
 ├─ _appearFromWave
 └─ _spawnWeight

BossSlimeData : SlimeDataBase
 └─ _goldReward

WaveTableData (SO, 스테이지마다 하나)
 ├─ _spawnInterval
 ├─ _interWaveDelay
 ├─ _finalWave
 ├─ _hpGrowth
 ├─ _goldRatio
 ├─ _waveClearGoldBase
 ├─ _waveClearGoldStep
 ├─ _fixedWaves  : List<FixedWave>
 ├─ _autoWave    : AutoWaveSetting
 └─ _bossEntries : List<BossSpawnEntry>

FixedWave ([Serializable])
 ├─ _waveNumber
 └─ _slimes : SlimeData[]

AutoWaveSetting ([Serializable])
 ├─ _baseCount
 ├─ _countGrowth
 ├─ _maxCount
 └─ _slimes : SlimeData[]

BossSpawnEntry ([Serializable])
 ├─ _waveNumber
 ├─ _bossData : BossSlimeData
 └─ _extraStartDelay
```

`FixedWave` / `AutoWaveSetting` / `BossSpawnEntry`는 별도 SO가 아니라 인라인 `[Serializable]`이다. 테이블 안에서만 쓰이고 외부에서 GUID로 참조되지 않는다.

`SlimeData`는 concrete를 유지한다. abstract로 바꾸면 기존 일반 슬라임 에셋이 전부 깨진다.

---

## 2. 필드 설명

### SlimeDataBase

| 필드 | 설명 |
|---|---|
| _prefab | 슬라임 프리팹 |
| _baseHealth | 1웨이브 기준 체력 |
| _baseSpeed | 이동 속도 |
| _lifeCost | 탈출 시 차감 라이프. 웨이브 스케일링 없음 |
| _renderGroup | 렌더링 버킷 |

### SlimeData

| 필드 | 설명 |
|---|---|
| _appearFromWave | 이 웨이브부터 자동 생성 구간에 등장 |
| _spawnWeight | 자동 생성 구간 배분 가중치 |

### BossSlimeData

| 필드 | 설명 |
|---|---|
| _goldReward | 처치 골드. 직접 지정 |

보스 HP는 `_baseHealth` 고정값이며 `_hpGrowth`를 적용하지 않는다.

### WaveTableData

| 필드 | 설명 |
|---|---|
| _spawnInterval | 슬라임 간 스폰 간격. 전 웨이브 공통 |
| _interWaveDelay | 웨이브 간 공통 대기 |
| _finalWave | 스테이지 최종 웨이브 |
| _hpGrowth | 웨이브당 HP 증가율(복리) |
| _goldRatio | 처치 골드 비율 |
| _waveClearGoldBase | 클리어 골드 기본값 |
| _waveClearGoldStep | 클리어 골드 웨이브당 증가분 |

### FixedWave

| 필드 | 설명 |
|---|---|
| _waveNumber | 웨이브 번호 |
| _slimes | 스폰할 슬라임 배열 |

배열 순서가 곧 스폰 순서이고, 같은 `SlimeData`의 반복이 곧 마릿수다. 셔플하지 않는다.

```
_waveNumber: 4
_slimes: [A, A, B, A, A, B, A, A]
```

### AutoWaveSetting

| 필드 | 설명 |
|---|---|
| _baseCount | 11웨이브 기준 마릿수 |
| _countGrowth | 웨이브당 마릿수 증가율 |
| _maxCount | 마릿수 상한 |
| _slimes | 자동 생성 구간에 등장할 슬라임 목록 |

`_maxCount`는 성능(SkinnedMeshRenderer + MPB라 GPU 인스턴싱 불가)과 머지 여유 확보(그리드 칸을 배치에 다 쓰면 머지 불가) 두 목적을 함께 가진다.

### BossSpawnEntry

| 필드 | 설명 |
|---|---|
| _waveNumber | 보스가 등장할 웨이브 |
| _bossData | 사용할 보스 |
| _extraStartDelay | 이 웨이브 시작 전 추가 대기 |

보스는 웨이브당 1마리, 웨이브 맨 앞에 등장한다. 고정 구간(1~10)의 보스도 `_bossEntries`로 일원화한다.

---

## 3. 계산식

```
HP         = _baseHealth × (1 + _hpGrowth)^(N - 1)                          전 구간 적용
처치 골드   = round(스케일된 HP × _goldRatio)
클리어 골드 = _waveClearGoldBase + _waveClearGoldStep × N
마릿수      = clamp(floor(_baseCount × (1 + _countGrowth)^max(0, N - 11)), 1, _maxCount)
시작 대기   = _interWaveDelay + (보스 엔트리의 _extraStartDelay, 없으면 0)
```

`(N - 1)`을 지수로 쓰므로 1웨이브에서 배율이 1이 되어 `_baseHealth` 그대로 나온다. 고정 구간에도 적용해 경계에 계단이 생기지 않는다.

마릿수 지수를 `max(0, N - 11)`로 클램프하는 이유는 `_fixedWaves`에 웨이브 번호가 빠졌을 때 음수 지수가 나오는 것을 막기 위해서다.

---

## 4. 배분 규칙

1. 후보 선정 — `_appearFromWave <= N && _spawnWeight > 0`
2. 가중치 비율로 마릿수를 정수 배분(최대잉여법)
3. Fisher-Yates 셔플
4. 보스를 맨 앞에 삽입

가중치는 상대값이다. 합계를 100으로 맞출 필요가 없다. 제외된 슬라임은 합계에서 빠지므로 가중치가 자동으로 재정규화된다.

확률 추첨이 아니라 정수 배분이므로 마릿수 편차가 없다.

---

## 5. 웨이브 구조

| 구간 | 방식 |
|---|---|
| 1~10 | `_fixedWaves`에서 배열 조회, 순서 그대로 |
| 11~50 | 마릿수 공식 + 가중치 배분 + 셔플 |
| 보스 | `_bossEntries`에서 지정 웨이브에 오버레이 |

스폰이 끝나면 즉시 다음 웨이브로 넘어간다. 이전 웨이브 슬라임이 남아 있어도 무관하다. 최종 웨이브만 예외로, 슬라임이 전부 처리된 후 스테이지 클리어를 판정한다.

클리어 골드는 스폰 완료 시점에 지급한다.

---

## 6. 초기 수치

| 항목 | 값 | 비고 |
|---|---|---|
| _finalWave | 50 | |
| _hpGrowth | 0.06 | 7장에서 역산 (균형 9.4%의 64%) |
| _goldRatio | 0.2 | 임시(baseHealth 20 기준). baseHealth 100 전환 시 0.04 |
| _waveClearGoldBase | 20 | |
| _waveClearGoldStep | 2 | |
| _spawnWeight | 7 : 2 : 1 | |
| 소환 비용 | baseCost 20 + costStep 3 × 필드 유닛 수 | |
| 판매 환급률 | 50% | |

### 목표 수치 (밸런스 작업 시 전환)

| 항목 | 값 |
|---|---|
| _baseHealth (첫 슬라임) | 100 |
| Tier 1 타워 데미지 | 25 |
| _goldRatio | 0.04 |

### 보스 HP 기준

같은 웨이브 일반 슬라임 HP의 배수로 잡되 후반으로 갈수록 배수를 올린다.

| 보스 웨이브 | 배수 |
|---|---|
| 10 | 30 |
| 20 | 40 |
| 30 | 50 |
| 40 | 60 |
| 50 | 70 |

---

## 7. _hpGrowth 역산

타워 등급 배율이 **3.2배 / 4등급**으로 확정됐다([타워 로스터](슬라임TD_타워로스터.md) 참고). 50웨이브에 4등급이면 티어업 1회당 약 13웨이브다.

```
균형 hpGrowth = 3.2^(1/13) - 1 = 9.4%
실제 사용값   = 균형 × 60~70% = 5.6 ~ 6.6%
```

균형점을 그대로 쓰면 화력과 HP가 같은 속도로 자라 머지 보상감이 사라진다. 남는 여유는 `_countGrowth`가 흡수한다.

### 처리 여유 시뮬레이션

타워 8개 유지, 13웨이브마다 1등급씩 상승, 웨이브 길이 = `마릿수 × 0.8 + 3초` 가정.

| hpGrowth | W13 (1등급 말) | W26 (2등급 말) | W39 (3등급 말) | W50 (4등급) |
|---|---|---|---|---|
| 5% | 1.15 | 1.71 | 2.80 | 5.26 |
| **6%** | **1.03** | **1.35** | **1.96** | **3.30** |
| 7% | 0.92 | 1.07 | 1.37 | 2.08 |

목표는 티어 직전 1.1~1.2, 티어 직후 3~4의 톱니다.

- **5%**는 후반 여유가 5.26까지 벌어져 헐거워진다
- **7%**는 W13에서 0.92로 떨어져 1등급 구간에서 막힌다
- **6%**가 전 구간 1.03~3.30으로 톱니가 가장 일정하다

### 권장값

```
_hpGrowth   = 0.06
_countGrowth = 0.06  (유지)
_maxCount    = 40    (재검토 필요, 아래 참고)
```

`_maxCount: 40`이 W32에서 걸려 **후반 19웨이브의 물량이 평평해진다.** 난이도가 HP 단독으로 결정되므로, 55~60으로 올리거나 후반 난이도를 보스에 더 의존시키는 판단이 필요하다.

---

## 8. 미결 항목

- **머지 구조 정리** — 현재 머지가 데미지 배수가 아니라 다음 티어 랜덤 교체이고 티어가 2개(1·2등급)뿐이다. 3·4등급 로스터와 합성 규칙이 있어야 위 역산이 실제로 성립한다
- **타워 공격력 에셋 반영** — 현재 `Attack_*_Basic.asset`은 10~20. 확정된 1등급 25로 교체 필요
- **_maxCount 재검토** — 40이면 W32부터 물량 고정
- **보스 골드** — 현재 전부 100 고정. W50 기준 잡몹 1마리(137골드)보다 적다
- **분열 메커닉** — 이번 설계에서 제외. 추가 시 `SlimeDataBase`에 `_splitTarget` / `_splitCount` 필드 추가
