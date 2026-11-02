using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 확인용 WebGL 빌드. 배치 실행:
// Unity.exe -batchmode -quit -projectPath <빌드 폴더> -executeMethod WebGLBuilder.Build -webglOut <출력 폴더> [-webglRelease] -logFile <로그>
// WebGL 전용 설정은 빌드 직전에 코드로 넣는다. 빌드 전용 클론에서만 실행하고, 그 폴더의 ProjectSettings 변경은 커밋하지 않는다 (Android/iOS 설정과 분리).
public static class WebGLBuilder
{
    public const string TemplateName = "PROJECT:SlimeTDPortrait";

    public static void Build()
    {
        string outDir = GetArg("-webglOut") ?? Path.Combine("Builds", "WebGL");
        bool development = !HasArg("-webglRelease");
        var started = DateTime.Now;

        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Fail("빌드 대상을 WebGL로 바꾸지 못했습니다.");
                return;
            }

            ApplyWebGLSettings(development);

            AddressableAssetSettings.BuildPlayerContent(out var addressablesResult);
            if (!string.IsNullOrEmpty(addressablesResult.Error))
            {
                Fail($"Addressables 빌드 실패: {addressablesResult.Error}");
                return;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Fail($"플레이어 빌드 실패: {report.summary.result}, 에러 {report.summary.totalErrors}개");
                return;
            }

            Debug.Log($"[WebGLBuilder] 빌드 완료: {outDir}, {report.summary.totalSize / (1024f * 1024f):F1}MB, {(DateTime.Now - started).TotalMinutes:F1}분, 개발 빌드 {development}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Fail(e.Message);
        }
    }

    private static void ApplyWebGLSettings(bool development)
    {
        var target = NamedBuildTarget.WebGL;

        // GitHub Pages는 Content-Encoding 헤더를 주지 않아 압축 파일을 JS로 풀어야 한다.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = TemplateName;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = development
            ? WebGLExceptionSupport.FullWithStacktrace
            : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        // VContainer는 리플렉션으로 생성자와 [Inject] 메서드를 찾으므로 스트리핑을 최소로 둔다.
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);

        SetWasmCodeOptimizationForBuildTime();
    }

    // UnityEditor.WebGL은 WebGL 모듈이 있어야 존재하므로 직접 참조하지 않는다 (모듈 없는 PC에서도 컴파일되게).
    private static void SetWasmCodeOptimizationForBuildTime()
    {
        var settingsType = Type.GetType("UnityEditor.WebGL.UserBuildSettings, UnityEditor.WebGL.Extensions");
        var property = settingsType?.GetProperty("codeOptimization");
        if (property == null)
        {
            Debug.Log("[WebGLBuilder] codeOptimization 설정을 찾지 못해 기본값으로 빌드합니다.");
            return;
        }

        try
        {
            property.SetValue(null, Enum.Parse(property.PropertyType, "BuildTimes"));
        }
        catch (Exception e)
        {
            Debug.Log($"[WebGLBuilder] codeOptimization 설정 실패, 기본값으로 빌드합니다: {e.Message}");
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError($"[WebGLBuilder] {message}");
        EditorApplication.Exit(1);
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static bool HasArg(string name)
    {
        return Environment.GetCommandLineArgs().Contains(name);
    }
}
