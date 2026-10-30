# 슬라임 렌더링

슬라임 몸통·얼굴·아웃라인의 셰이더 구성과, 슬라임끼리 겹칠 때 서로 파고들어 보이는 문제를 **Depth Bias**로 해결한 구조를 정리한다.

---

## 1. 해결한 문제

**증상**: 슬라임들이 경로상 가까워지면 실제 3D 공간에서 구체끼리 겹친다. Opaque + ZWrite On은 픽셀 단위 depth test로 그리므로 겹친 부분에서 두 메시의 교차선이 그대로 드러나 **서로 파고든 것처럼** 보인다.

Opaque로 그리는 한 구조적으로 발생한다. 이전에 Transparent + SortingGroup이었을 때는 마리 단위로 통째 정렬돼 이 현상이 감춰져 있었으나, ZWrite Off라 같은 메시 안의 삼각형 정렬이 깨지는 별개의 문제가 있었다.

**해결**: 머티리얼은 Opaque를 유지하고, 셰이더 vertex 단계에서 clip space z에 **스폰 순번 기반 순환 버킷 오프셋**을 더해 그리는 순서를 강제한다. 각 슬라임이 depth상 서로 다른 층에 놓이므로 공간적으로 겹쳐 있어도 교차선 없이 한 마리가 온전히 다른 마리 앞에 그려진다.

**Transform 실제 좌표는 건드리지 않는다.** 타워 타겟팅과 이동 로직에 영향이 없다.

### Depth Bias와 버킷이란

**Depth Bias**는 오브젝트를 실제로 움직이지 않고, 셰이더가 깊이 값만 살짝 더하거나 빼서 **그려지는 순서를 속이는** 기법이다. GPU는 각 픽셀의 깊이를 비교해 앞뒤를 정하는데, 이 값에 인위적인 편차(bias)를 주면 물리적 위치와 무관하게 무엇이 앞에 보일지 정할 수 있다.

이 프로젝트에서는 두 슬라임이 실제로 겹쳐 있어도, 한쪽의 깊이를 조금 뒤로 밀어 **교차선 없이 한 마리가 통째로 뒤에 있는 것처럼** 보이게 만든다. 좌표를 실제로 옮기면 이동 경로와 타워 타겟팅이 깨지지만, Depth Bias는 화면에 그릴 때만 적용되므로 게임 로직에는 아무 영향이 없다.

**버킷(bucket)**은 그 편차를 얼마나 줄지 정하는 **정수 번호**다. 연속적인 값 대신 0, 1, 2… 같은 칸을 미리 나눠두고 슬라임마다 하나씩 배정한다.

```
버킷 0 → 오프셋 0.0  (가장 앞)
버킷 1 → 오프셋 0.3
버킷 2 → 오프셋 0.6
버킷 3 → 오프셋 0.9  (더 뒤)
```

칸으로 나누는 이유는 **번호만 다르면 앞뒤가 확실히 갈리기** 때문이다. 칸 간격(`_bucketSpacing`)을 슬라임 지름보다 크게 잡으면 두 슬라임의 깊이가 절대 애매해지지 않는다. 슬라임은 스폰될 때 번호 하나를 받고, 그 값은 죽을 때까지 바뀌지 않는다.

칸 개수(`_bucketCount`)는 유한하므로 다 쓰면 처음으로 돌아가 **순환**한다. 같은 번호를 다시 받는 슬라임은 경로상 그 개수만큼 떨어져 있어 실질적으로 겹치지 않는다.

---

## 2. 셰이더 구성

슬라임은 툰 셰이딩을 쓰지 않는다. URP Unlit 수준의 경량 자체 셰이더 2개를 쓴다.

| 파일 | 용도 | Surface | ZWrite | Queue |
|---|---|---|---|---|
| `Assets/Art/Shaders/SlimeBody.shader` | 몸통 + 아웃라인 | Opaque | On | Geometry(2000) |
| `Assets/Art/Shaders/SlimeFace.shader` | 얼굴 | AlphaClip | On | AlphaTest(2450) |
| `Assets/Art/Shaders/SlimeDepthBucket.hlsl` | 오프셋 계산 (공용) | — | — | — |

**URP 내장 Unlit을 직접 쓰지 않는 이유**는 패키지 파일이라 `_SlimeDepthBucket` 프로퍼티를 추가할 수 없기 때문이다. 기능은 URP Unlit과 동등하다.

### SlimeBody.shader — 패스 순서가 중요하다

```
Pass 1  Outline   LightMode=SRPDefaultUnlit,  Cull Front, ZWrite On
Pass 2  Unlit     LightMode=UniversalForward, Cull [_Cull], ZWrite On
```

아웃라인을 **먼저** 그려야 본체가 안쪽을 덮어 테두리만 남는다. 순서를 바꾸면 아웃라인이 보이지 않는다.

### 얼굴은 Alpha Clipping이어야 한다

얼굴 텍스처(`T_Slime_Face_XX_Custorm.png`, 1024×1024 RGBA)의 알파 분포를 실측한 값이다.

| 텍스처 | 완전투명 | 불투명 | 반투명 |
|---|---|---|---|
| Face_01 | 75.6% | 5.1% | 19.3% |
| Face_07 | 78.3% | 8.9% | 12.8% |
| Face_17 | 78.2% | 9.9% | 11.9% |

- **순수 Opaque 불가**: 76~78%가 완전투명이라 1024×1024 사각형이 몸통을 덮는다
- **Transparent(ZWrite Off) 불가**: 얼굴이 depth를 안 쓰면 얼굴만 정렬에 의존해 같은 문제가 재현된다
- **Alpha Clipping 채택**: 투명 픽셀은 discard하면서 ZWrite On을 유지한다

반투명 12~19%는 눈·입의 안티에일리어싱 경계다. 클리핑은 임계값으로 잘라내 계단이 생기지만, `_AlphaToMask: 1`(alpha-to-coverage)이 Mobile 퀄리티의 MSAA 2×와 함께 동작해 경계를 완화한다. **MSAA를 끄면 얼굴 경계가 거칠어진다.**

---

## 3. Depth Bucket

### 오프셋 계산 (`SlimeDepthBucket.hlsl`)

```hlsl
float ndcPerUnit = 2.0 / (_ProjectionParams.z - _ProjectionParams.y);
return _SlimeDepthBucket * _SlimeBucketSpacing * ndcPerUnit * direction;
```

**직교 투영 전용 공식이다.** 게임 카메라가 orthographic이라, 원근용 공식(카메라 거리 `clipCameraPos.z`로 스케일)을 쓰면 clip z가 near~far 전체 범위에 선형 대응해 오프셋이 폭발한다. 실제로 이 실수로 뒷 웨이브 슬라임이 near plane을 뚫고 화면에서 사라졌다.

`ndcPerUnit`으로 환산하므로 **spacing 단위는 월드 유닛**이다. spacing 0.3이면 버킷 한 칸당 깊이 0.3유닛 차이이고, 카메라 거리와 무관하게 일정하다.

**오프셋 방향은 뒤로 민다.** 카메라 쪽으로 당기면 전경 오브젝트(지형, 타워)를 뚫고 나온다. 뒤로 밀면 그 문제가 구조적으로 생기지 않는다. 대신 **바닥보다 뒤로 가면 슬라임이 지형에 묻히므로** 그쪽이 상한이다.

### 버킷 배정 (`SpawnOrderCounter`)

오프셋이 뒤로 미는 방향이라 **작은 버킷일수록 앞에** 그려진다.

```
보스        → 버킷 0          (오프셋 없음, 항상 맨 앞)
일반 슬라임 → 버킷 1 ~ N      (스폰 순서대로, 소진 시 1로 순환)
```

- 먼저 스폰된 슬라임이 앞에 온다 (경로를 앞서가는 쪽이 위)
- 보스는 카운터를 쓰지 않고 0을 고정으로 받는다. 화면 동시 등장 보스 1마리 전제
- **죽은 슬라임의 버킷은 회수하지 않는다.** 스폰 시점에 한 번 정하고 끝이며, 풀에서 재사용될 때 새 값으로 덮어쓴다

버킷이 순환하므로 스폰 순번 k와 k+N은 같은 버킷이다. 경로상 N마리만큼 떨어져 있어 보통 겹치지 않지만, 속도 차로 따라잡으면 문제가 재현될 수 있다. 그때는 `_bucketCount`를 늘린다.

### 값 전달 경로

```
SpawnOrderCounter.NextBucket()        발급 (정수 하나)
  └ WaveSpawner.SpawnOne()            스폰 시 1회 호출
      └ BaseSlime.SetDepthBucket()    MaterialPropertyBlock 주입
          └ 셰이더 _SlimeDepthBucket
```

- **몸통·얼굴 두 렌더러에 같은 값을 주입한다.** 다른 값이 들어가면 얼굴이 몸통과 어긋난다. `BaseSlime._depthBucketRenderers` 배열에 프리팹에서 둘 다 할당해야 한다
- **스폰 이후 재계산하지 않는다.** 매 프레임 갱신하면 원래 문제가 그대로 재현된다
- 풀 반환 시 MPB를 정리하지 않는다. 다음 `Get`에서 반드시 덮어쓴다

`_SlimeBucketSpacing`은 `Shader.SetGlobalFloat`로 전파되는 **전역**이라, 값을 바꾸면 살아있는 슬라임 전체에 즉시 반영된다. `SlimeDepthBucketBinder`가 `GameInitiator.StartAsync`에서 한 번 바인딩한다.

---

## 4. SRP Batcher 제약

**`_SlimeDepthBucket`은 반드시 `CBUFFER_START(UnityPerMaterial)` 안에 선언한다.**

SRP Batcher가 켜져 있으면(`m_UseSRPBatcher: 1`) 머티리얼 상수는 전부 이 CBUFFER 안에 있어야 한다. 밖에 선언하면 정의되지 않은 레지스터를 읽어 **첫 프레임에 쓰레기 값이 들어간다** — 실제로 슬라임이 파랗게/검게 번쩍이는 증상이 나왔다.

반면 `_SlimeBucketSpacing`은 `SetGlobalFloat`로 설정하는 전역이므로 **CBUFFER 밖**에 있어야 한다. 둘의 위치가 다르다.

**`[PerRendererData]` 속성을 붙이면 안 된다.** 이 속성은 값이 머티리얼에 저장되지 않게 하는데, SRP Batcher는 머티리얼 데이터로 CBUFFER를 채우므로 MPB가 쓰이기 전까지 빈 값을 읽는다. 머티리얼에 `_SlimeDepthBucket: 0`이 직렬화돼 있어야 한다.

선언 순서도 지켜야 한다. CBUFFER 선언이 먼저, `SlimeDepthBucket.hlsl` include가 나중이다.

---

## 5. 아웃라인

슬라임과 타워 모두 툰 셰이더 `SlimeTD/SlimeFlatKit`의 외곽선을 쓴다. 예전에 두 번째 머티리얼로 외곽선을 덧그리던 `Custom_Outline_Opaque`와 그 셰이더(`OutlineOpaque.shader`, `Outline.shader`)는 삭제했다.

- 외곽선은 셰이더의 `Outline` 패스(`LightMode=Outline`)로 그린다. 이 패스는 URP 렌더러의 `Flat Kit Per Object Outline` 기능(Render Objects)이 있어야 실행되므로 `Mobile_Renderer`와 `PC_Renderer` 둘 다에 넣어 두었다. 렌더러를 새로 만들면 이 기능을 빠뜨리지 않는다
- 머티리얼에서 `Enable Outline`을 켜고 두께·색을 정한다. 머티리얼(서브메시)마다 따로 그려지므로 메시를 한 조각으로 합칠 필요가 없다
- 타워 머티리얼은 `Smooth Normals`를 켠다. 하드 엣지가 많은 캐릭터 메시에서 외곽선이 갈라지지 않게 하는 옵션이고, UV 채널 2(uv3)의 노멀을 읽는다. FBX 메시에는 이 채널이 없어 `OutlineSmoothNormals` 컴포넌트가 `Awake`에서 구워 넣는다. 이 옵션을 켠 렌더러에는 이 컴포넌트가 꼭 있어야 하며, 없으면 외곽선이 보이지 않는다
- 그래서 에디터에서 플레이하지 않은 상태(프리팹 편집 화면 등)에서는 타워 외곽선이 보이지 않는다
- 슬라임은 둥근 메시라 `Smooth Normals`를 끄고 메시 노멀을 그대로 쓴다
- 여러 조각으로 된 캐릭터(KayKit 등)는 조각마다 외곽선이 생겨 몸 안쪽에 검은 선이 비친다. 머티리얼 옵션 `Silhouette Write` + `Silhouette Only`를 켜면 몸체가 스텐실 비트 0을 쓰고 외곽선은 그 위에 그리지 않아 바깥 테두리만 남는다 (대신 활이 몸 앞을 지나는 식의 안쪽 경계선도 사라진다)
- 스킨드 메시는 uv3에 구운 노멀이 뼈를 따라 돌지 않는다. `Tangent Space Normals (Skinned)`를 켜면 `OutlineSmoothNormals`가 탄젠트 공간으로 굽고 셰이더가 스키닝된 노멀/탄젠트로 복원한다
- 현재 아처(`RogueGray`)만 두께 3 + 위 세 옵션을 쓴다. 나머지 타워는 확인 후 같은 설정으로 옮긴다

| 셰이더 | 용도 | 노멀 소스 |
|---|---|---|
| `SlimeFlatKit.shader` (`Outline` 패스) | 슬라임, 타워, 무기, 투사체 | 슬라임은 메시 노멀, 타워는 uv3 스무스 노멀 |

---

## 6. 현재 설정값

`Assets/Datas/SlimeDepthBucketSettings.asset` (Addressables, `DataKeys.SlimeDepthBucketSettings`)

| 항목 | 값 | 의미 |
|---|---|---|
| `_enabled` | 1 | 끄면 오프셋 0. on/off 비교용 |
| `_bucketSpacing` | 0.3 | 버킷 한 칸당 깊이 차이 (월드 유닛) |
| `_bucketCount` | 16 | 서로 다른 순서를 보장하는 마릿수 |

머티리얼 (몸통 33개 / 얼굴 8개, 파일명과 GUID는 `_Custom` 유지)

| 항목 | 몸통 | 얼굴 |
|---|---|---|
| 셰이더 | `SlimeTD/SlimeBody` | `SlimeTD/SlimeFace` |
| Queue | 셰이더 상속(Geometry) | 2450 |
| ZWrite | 1 | 1 |
| `_OutlineWidth` | 4 | — |
| `_Cutoff` | — | 0.5 |

### 튜닝 지침

- **파고드는 게 남으면** spacing을 올린다. 슬라임 지름이 약 1유닛이라 그 근처면 확실히 갈린다
- **슬라임이 바닥에 묻히면** spacing을 내린다. 최대 `spacing × bucketCount`만큼 뒤로 간다
- `_enabled` 토글로 즉시 on/off 비교가 가능하다

---

## 7. 검증 항목

- 겹친 슬라임이 교차선 없이 마리 단위로 앞뒤가 갈리는지
- 뿔을 포함한 정면 메시가 정상 노출되는지
- 지형·장애물에 정상적으로 가려지는지
- 몸통과 얼굴이 어긋나지 않는지 (두 렌더러에 같은 값이 들어갔는지)
- 얼굴 눈·입 경계가 거칠지 않은지 (MSAA + AlphaToMask)
- 보스가 항상 일반 슬라임보다 앞에 그려지는지
- 스폰 첫 프레임에 색이 번쩍이지 않는지 (SRP Batcher CBUFFER 확인)
- 마릿수를 늘렸을 때 드로우콜 (Frame Debugger)

**검증은 Mobile 퀄리티 기준.** 실제 기기와 같은 포워드 렌더러다. PC 퀄리티(`PC_Renderer`, 디퍼드)에도 외곽선 기능은 있지만 화면이 조금 다르게 나올 수 있다.

---

## 8. 알려진 제약 (이번 범위 아님)

- **오프셋은 슬라임 간에만 옳고 슬라임 vs 월드에는 틀리다.** 버킷이 큰 슬라임은 지형에 대해서도 뒤로 밀려 있다. spacing을 과하게 키우면 바닥에 묻힌다
- **버킷 재사용 잔여 문제.** 스폰 순번 k와 k+N은 같은 버킷이다
- **SkinnedMeshRenderer라 GPU Instancing 불가.** MaterialPropertyBlock 사용으로 SRP Batcher 배칭에서도 이탈한다
- **몸통·얼굴이 별도 머티리얼**이라 마리당 드로우콜이 최대 3개(몸통 컬러·아웃라인 + 얼굴)다. 텍스처 아틀라싱으로 통합하면 줄일 수 있으나 UV와 표정 교체 로직을 건드려야 한다

### 남아있는 UTS3 파일

툰 셰이딩으로 되돌릴 경우를 대비해 `SlimeToon.shader`, `SlimeToonBody/Input/Outline.hlsl`을 남겨두었다. 현재 어떤 머티리얼도 참조하지 않는다. 되돌리려면 머티리얼의 셰이더를 `SlimeTD/SlimeToon`으로 바꾸면 된다.

UTS3 원본(`Library/PackageCache/com.unity.toonshader`)은 패키지라 수정할 수 없어, URP SubShader만 발췌해 복사한 것이다. 발췌 과정에서 UTS3의 `CustomEditor`가 빠져 렌더 큐를 자동 설정해주지 않으므로, 되돌릴 때 큐와 `Blend [_SrcBlend] [_DstBlend]` 설정을 직접 확인해야 한다.
