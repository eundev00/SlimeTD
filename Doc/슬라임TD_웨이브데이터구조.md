# 슬라임TD 웨이브 데이터 구조

웨이브 데이터의 타입·필드·계산식을 정리한 문서다. 구간별 수치, 전체 웨이브 표, 골드, 밸런싱 근거는 [웨이브 기획](슬라임TD_웨이브기획.md)을 참고한다.

---

## 1. 타입 정의

```
SlimeDataBase (abstract SO, CreateAssetMenu 없음)
 ├─ _prefab
 ├─ _baseHealth
 ├─ _baseSpeed
 ├─ _lifeCost
 ├─ _renderGroup
 └─ _goldReward

SlimeData : SlimeDataBase        (추가 필드 없음, 구간 슬롯 타입 구분용)

BossSlimeData : SlimeDataBase
 └─ _instantGameOver

WaveTableData (SO, 스테이지마다 하나)
 ├─ _spawnInterval
 ├─ _interWaveDelay
 ├─ _normalHpGrowth
 └─ _zones : List<WaveZone>

WaveZone ([Serializable])
 ├─ _slime : SlimeData
 ├─ _count
 ├─ _boss  : BossSlimeData
 └─ _bossExtraStartDelay
```

`WaveZone`은 별도 SO가 아니라 인라인 `[Serializable]`이다.

`SlimeData`는 concrete를 유지한다. abstract로 바꾸면 기존 일반 슬라임 에셋이 전부 깨진다.

---

## 2. 구간 구조

10웨이브가 한 구간이다. 구간마다 슬라임 종류가 바뀌고, 10의 배수 웨이브는 보스 단독이다. 한 웨이브에는 한 종류만 등장한다.

| 구간 | 웨이브 | 슬라임 | 보스 |
|---|---|---|---|
| 1 | 1~9 | Slime_01 | 10 → Boss_01 |
| 2 | 11~19 | Slime_02 | 20 → Boss_02 |
| 3 | 21~29 | Slime_03 | 30 → Boss_03 |
| 4 | 31~39 | Slime_04 | 40 → Boss_04 |
| 5 | 41~49 | Slime_05 (임시, 01 프리팹 복사본) | 50 → Boss_05 |

최종 웨이브 = `_zones` 개수 × 10.

스폰이 끝나면 즉시 다음 웨이브로 넘어간다. 이전 웨이브 슬라임이 남아 있어도 무관하다. 최종 웨이브만 예외로, 슬라임이 전부 처리된 후 스테이지 클리어를 판정한다.

---

## 3. 필드 설명

### SlimeDataBase

| 필드 | 설명 |
|---|---|
| _prefab | 슬라임 프리팹 |
| _baseHealth | 일반: 웨이브 성장률 적용 전 기본값 / 보스: 이 값 그대로 사용 |
| _baseSpeed | 이동 속도 |
| _lifeCost | 탈출 시 차감 하트 |
| _renderGroup | 렌더링 버킷 |
| _goldReward | 처치 골드 |

### BossSlimeData

| 필드 | 설명 |
|---|---|
| _instantGameOver | 켜면 경로 끝 도달 시 즉시 게임오버, 끄면 `_lifeCost`만큼 차감 |

### WaveTableData

| 필드 | 설명 |
|---|---|
| _spawnInterval | 슬라임 간 스폰 간격. 전 웨이브 공통 |
| _interWaveDelay | 웨이브 간 공통 대기 |
| _normalHpGrowth | 웨이브당 HP 증가율(복리). 일반 슬라임에만 적용 |
| _zones | 구간 목록 |

### WaveZone

| 필드 | 설명 |
|---|---|
| _slime | 구간 1~9번째 웨이브 슬라임 |
| _count | 일반 웨이브 마릿수 |
| _boss | 구간 10번째 웨이브 보스 |
| _bossExtraStartDelay | 보스 웨이브 시작 전 추가 대기 |

---

## 4. 계산식

```
구간       = _zones[(N - 1) / 10]
보스 웨이브 = N % 10 == 0

일반 HP    = slime._baseHealth × (1 + _normalHpGrowth)^(N - 1)
보스 HP    = boss._baseHealth
마릿수      = zone._count (보스 웨이브는 1)
처치 골드   = _goldReward
시작 대기   = _interWaveDelay + (보스 웨이브면 zone._bossExtraStartDelay)
```

`(N - 1)`을 지수로 쓰므로 1웨이브에서 배율이 1이 되어 `_baseHealth` 그대로 나온다. 구간이 바뀌면 슬라임의 `_baseHealth`가 바뀌면서 HP가 한 번 더 뛴다.

웨이브 클리어 골드는 없다. 골드는 처치 보상만 지급한다.

---

## 5. 패배 조건

- **일반 슬라임**: 경로 끝 도달 시 `_lifeCost`만큼 하트 차감. 하트 0이면 게임오버
- **보스**: 경로 끝 도달 시 즉시 게임오버. 난이도가 과하면 `_instantGameOver`를 끄고 `_lifeCost`를 크게 잡아 하트 대량 차감으로 전환

---

## 6. 수치

초기 수치, HP 표, 보스 HP, 골드, 밸런싱 노브와 근거는 [웨이브 기획](슬라임TD_웨이브기획.md) 참고.

---

## 7. 미결 항목

- **_lifeCost 존치 여부** — 일반 슬라임 전부 1. 삭제 시 차감량을 GameConfig로 이동
