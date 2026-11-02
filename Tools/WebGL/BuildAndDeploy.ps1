# SlimeTD 확인용 WebGL 빌드 + GitHub Pages 배포
# 사용: powershell -ExecutionPolicy Bypass -File Tools\WebGL\BuildAndDeploy.ps1 [-SkipDeploy] [-Dev]
# 작업 폴더에 커밋된 main만 빌드한다 (빌드 폴더는 작업 폴더를 origin으로 둔 별도 클론).
param(
    [string]$WorkRepo = "C:\Projects\SlimeTD",
    [string]$BuildRepo = "C:\Projects\SlimeTD_Build",
    [string]$DeployRepo = "C:\Projects\SlimeTD_Web",
    [string]$DeployRemote = "https://github.com/eundev00/SlimeTD-Web.git",
    [string]$PagesUrl = "https://eundev00.github.io/SlimeTD-Web/",
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe",
    [string]$Git = "C:\Users\eunv0\AppData\Local\GitHubDesktop\app-3.6.6\resources\app\git\cmd\git.exe",
    [string]$CredentialTarget = "GitHub - https://api.github.com/eundev00",
    [switch]$SkipDeploy,
    [switch]$Dev
)

# 네이티브 명령의 stderr를 에러로 취급하지 않도록 Continue로 두고 종료 코드로 판정한다.
$ErrorActionPreference = "Continue"

function Step($message) { Write-Host "[WebGL] $message" }

function Get-GitHubToken {
    Add-Type -TypeDefinition @'
using System; using System.Runtime.InteropServices; using System.Text;
public static class SlimeTDCred {
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  struct CREDENTIAL { public uint Flags; public uint Type; public string TargetName; public string Comment; public long LastWritten; public uint CredentialBlobSize; public IntPtr CredentialBlob; public uint Persist; public uint AttributeCount; public IntPtr Attributes; public string TargetAlias; public string UserName; }
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool CredRead(string t, uint type, uint flags, out IntPtr p);
  [DllImport("advapi32.dll")] static extern void CredFree(IntPtr p);
  public static string Read(string target) {
    IntPtr p; if (!CredRead(target, 1, 0, out p)) return null;
    var c = (CREDENTIAL)Marshal.PtrToStructure(p, typeof(CREDENTIAL));
    var b = new byte[c.CredentialBlobSize]; Marshal.Copy(c.CredentialBlob, b, 0, b.Length); CredFree(p);
    return Encoding.UTF8.GetString(b);
  }
}
'@ -ErrorAction SilentlyContinue
    return [SlimeTDCred]::Read($CredentialTarget)
}

function Invoke-GitWithToken([string[]]$GitArgs) {
    $token = Get-GitHubToken
    if (-not $token) { throw "GitHub 토큰을 찾지 못했습니다 ($CredentialTarget)." }
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("x-access-token:$token"))
    $output = & $Git -c "http.https://github.com/.extraheader=AUTHORIZATION: basic $b64" @GitArgs 2>&1
    $code = $LASTEXITCODE
    $output | ForEach-Object { "$_" -replace [regex]::Escape($b64), '***' } | Write-Host
    if ($code -ne 0) { throw "git $($GitArgs -join ' ') 실패 (exit $code)" }
}

# 1. 빌드 폴더를 작업 폴더의 main과 맞춘다
if (-not (Test-Path $BuildRepo)) {
    Step "빌드 폴더 클론: $BuildRepo"
    & $Git clone $WorkRepo $BuildRepo
    if ($LASTEXITCODE -ne 0) { throw "클론 실패" }
}
Push-Location $BuildRepo
try {
    & $Git fetch origin main
    if ($LASTEXITCODE -ne 0) { throw "fetch 실패" }
    # 이전 빌드가 바꾼 ProjectSettings 등은 버린다. Library/Logs/Builds는 무시 대상이라 유지된다.
    & $Git reset --hard origin/main | Out-Null
    & $Git clean -fd -q
    $sha = (& $Git rev-parse --short HEAD).Trim()
    Step "빌드 대상 커밋: $sha"
} finally { Pop-Location }

# 2. Unity 배치 빌드
$outDir = Join-Path $BuildRepo "Builds\WebGL"
$log = Join-Path $BuildRepo "Logs\webgl_build.log"
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
$unityArgs = @("-batchmode", "-quit", "-projectPath", $BuildRepo, "-executeMethod", "WebGLBuilder.Build", "-webglOut", $outDir, "-logFile", $log)
if ($Dev) { $unityArgs += "-webglDev" }

# git URL 패키지(UniTask, UniRx, VContainer)를 받으려면 Unity가 PATH에서 git을 찾아야 한다. 이 PC의 git은 GitHub Desktop 내장이라 PATH에 없다.
$env:PATH = "$(Split-Path $Git);$env:PATH"

Step "Unity 빌드 시작 (로그: $log)"
$started = Get-Date
$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru
$proc.WaitForExit()
$minutes = [math]::Round(((Get-Date) - $started).TotalMinutes, 1)
if ($proc.ExitCode -ne 0) {
    Select-String -Path $log -Pattern "error CS|\[WebGLBuilder\]|Exception|Error building" | Select-Object -Last 20 | ForEach-Object { $_.Line } | Write-Host
    throw "Unity 빌드 실패 (exit $($proc.ExitCode), $minutes 분). 로그: $log"
}
Step "빌드 완료 ($minutes 분)"

# GitHub는 100MB가 넘는 파일을 받지 않는다
$big = Get-ChildItem $outDir -Recurse -File | Where-Object { $_.Length -gt 95MB }
if ($big) { throw "95MB가 넘는 파일이 있어 배포할 수 없습니다: $($big.Name -join ', ')" }

if ($SkipDeploy) { Step "배포 생략. 결과: $outDir"; return }

# 3. 배포 레포에 복사 후 푸시
if (-not (Test-Path (Join-Path $DeployRepo ".git"))) {
    Step "배포 레포 준비: $DeployRepo"
    New-Item -ItemType Directory -Force $DeployRepo | Out-Null
    Push-Location $DeployRepo
    try {
        & $Git init -q -b main
        & $Git remote add origin $DeployRemote
    } finally { Pop-Location }
}

Push-Location $DeployRepo
try {
    & $Git ls-remote --exit-code origin main 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Invoke-GitWithToken @("fetch", "-q", "origin", "main")
        & $Git reset -q --hard origin/main
    }
} finally { Pop-Location }

robocopy $outDir $DeployRepo /MIR /XD .git /XF .nojekyll /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "결과물 복사 실패 (robocopy $LASTEXITCODE)" }
New-Item -ItemType File -Force (Join-Path $DeployRepo ".nojekyll") | Out-Null

Push-Location $DeployRepo
try {
    & $Git add -A
    & $Git commit -q -m "WebGL build $sha ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))"
    if ($LASTEXITCODE -ne 0) { Step "바뀐 파일이 없어 커밋을 건너뜁니다." }
    Invoke-GitWithToken @("push", "-u", "origin", "main")
} finally { Pop-Location }

Step "배포 완료: $($PagesUrl)?v=$sha"
Step "Pages 반영까지 1~2분 걸릴 수 있습니다."
