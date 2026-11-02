# 슬라임TD — WebGL 확인용 빌드

원격으로 작업할 때 폰 브라우저에서 바로 플레이해 보기 위한 빌드다. 출시용이 아니라 **자주 바뀌는 코드를 빠르게 확인**하는 용도라, 빌드 시간을 줄이는 쪽으로 설정한다.

- 결과 URL: https://eundev00.github.io/SlimeTD-Web/ (뒤에 `?v=커밋해시`를 붙여 캐시를 피한다)
- 배포 레포: `eundev00/SlimeTD-Web` (공개, GitHub Pages `main` 루트)

## 원칙: Android/iOS에 영향을 주지 않는다

| 항목 | 분리 방법 |
|---|---|
| 빌드 대상 전환 | 빌드 전용 클론(`C:\Projects\SlimeTD_Build`)에서만 WebGL로 바꾼다. 작업 폴더는 건드리지 않는다 |
| WebGL Player 설정 | 커밋하지 않는다. `WebGLBuilder`가 빌드 직전에 코드로 넣고, 빌드 폴더는 매번 `reset --hard`로 되돌린다 |
| 스트리핑, IL2CPP 코드 생성 | `NamedBuildTarget.WebGL` 값만 바꾼다 |
| 저장 코드 | `#if UNITY_WEBGL && !UNITY_EDITOR` 안에서만 동기 파일 IO + IndexedDB 동기화 |
| 로그 창 | `WebGLLogOverlay`는 `UNITY_WEBGL && DEVELOPMENT_BUILD`에서만 컴파일된다 |
| jslib | `Assets/Plugins/WebGL/`에 두어 WebGL 빌드에만 포함된다 |
| Addressables | 빌드 결과가 `[BuildTarget]` 폴더별로 따로 생긴다 |

## 구성

| 파일 | 역할 |
|---|---|
| `Tools/WebGL/BuildAndDeploy.ps1` | 빌드 폴더 동기화 → Unity 배치 빌드 → 배포 레포 복사·푸시 → URL 출력 |
| `Assets/Scripts/Editor/WebGLBuilder.cs` | 배치 빌드 진입점 (`-executeMethod WebGLBuilder.Build`). WebGL 설정 적용, Addressables 빌드, 플레이어 빌드, 실패 시 exit 1 |
| `Assets/WebGLTemplates/SlimeTDPortrait/` | 세로 9:16 템플릿. 데스크톱은 가운데 세로 화면, 휴대폰 세로는 꽉 채움, 가로로 돌리면 안내 문구. DPR은 2로 제한 |
| `Assets/Scripts/Common/WebGLLogOverlay.cs` | 개발 빌드의 화면 로그 창 (오른쪽 위 `LOG` 버튼, 에러 수 표시) |
| `Assets/Plugins/WebGL/SlimeTDPersistentData.jslib` | 저장 후 `FS.syncfs`로 IndexedDB에 기록 |

## 빌드 시 적용하는 WebGL 설정 (`WebGLBuilder.ApplyWebGLSettings`)

- 압축 Gzip + Decompression Fallback. GitHub Pages가 `Content-Encoding` 헤더를 주지 않기 때문이다
- 템플릿 `PROJECT:SlimeTDPortrait`, 데이터 캐싱 켬
- 예외: 개발 빌드는 스택트레이스 포함, `-webglRelease`면 명시적 예외만
- Managed Stripping Minimal. VContainer가 리플렉션으로 생성자와 `[Inject]`를 찾기 때문이다
- IL2CPP 코드 생성 OptimizeSize, Wasm 코드 최적화 BuildTimes (빌드 시간 단축). 후자는 WebGL 모듈 API라 리플렉션으로 설정한다 (모듈 없는 PC에서도 컴파일되게)
- 퀄리티는 프로젝트 설정 그대로 WebGL 기본값 Mobile

## 사용 흐름

```
"빌드해줘"
→ 작업 폴더 커밋 (빌드 폴더는 작업 폴더의 main을 가져오므로 커밋만 하면 된다)
→ powershell -ExecutionPolicy Bypass -File Tools\WebGL\BuildAndDeploy.ps1
→ 실패하면 C:\Projects\SlimeTD_Build\Logs\webgl_build.log를 보고 수정 후 재시도
→ 출력된 URL(?v=해시)을 폰에서 연다
```

- 첫 빌드는 Library를 WebGL용으로 새로 임포트해서 오래 걸린다. 이후에는 바뀐 부분만 다시 처리한다
- `-SkipDeploy`: 빌드만, `-Release`: 개발 빌드 끄기 (성능 확인용)
- 배포 레포는 `C:\Projects\SlimeTD_Web` 로컬 클론을 쓴다. 푸시는 PC에 저장된 GitHub 토큰으로 한다

## 주의

- **커밋한 내용만 빌드된다.** 작업 폴더의 커밋 안 된 변경은 들어가지 않는다
- GitHub는 100MB 넘는 파일을 받지 않는다. 스크립트가 95MB 넘는 결과물을 발견하면 배포를 멈춘다
- iOS Safari는 PC 없이 콘솔을 볼 수 없다. 개발 빌드의 `LOG` 버튼으로 확인한다
- 새 빌드가 안 보이면 URL의 `?v=` 값을 바꾸거나 새로고침한다. 홈 화면에 추가하면 주소창 없이 볼 수 있다
- 브라우저에서는 화면 회전을 막을 수 없어서, 가로로 돌리면 안내 문구만 띄운다
- 웹의 저장 데이터는 브라우저(IndexedDB)에 있다. 사이트 데이터를 지우면 새 계정으로 시작한다
