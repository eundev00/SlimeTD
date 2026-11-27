using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// 실제 게임 화면을 찍는 공용 도구. 배치 모드에서는 Game 뷰가 없어 UI가 틀어지므로 에디터 창을 띄워 실행한다.
// Unity.exe -projectPath <프로젝트> -executeMethod ScreenshotCapture.Run -shotSteps "wait:3;click:TabHunter;wait:1;shot:hunter"
//   [-shotOut <폴더>] [-shotScene <씬 경로>] [-shotSize 1080x1920] [-shotTimeout 120]
// 단계: wait:<초>, click:<오브젝트 이름>, shot:<파일 이름>. 끝나면 에디터를 닫는다.
[InitializeOnLoad]
public static class ScreenshotCapture
{
    private const string StateKey = "ScreenshotCapture.State";

    [Serializable]
    private class State
    {
        public string[] Steps;
        public string OutDir;
        public int Index;
        public double StepStart = -1;
        public double Deadline;
        public string PendingFile;
    }

    private static State _state;

    static ScreenshotCapture()
    {
        var json = SessionState.GetString(StateKey, "");
        if (string.IsNullOrEmpty(json))
            return;

        _state = JsonUtility.FromJson<State>(json);
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        string steps = Arg("-shotSteps", "wait:3;shot:screen");
        string scene = Arg("-shotScene", EditorBuildSettings.scenes.Length > 0 ? EditorBuildSettings.scenes[0].path : "");
        string outDir = Path.GetFullPath(Arg("-shotOut", "Temp/Screenshots"));
        var size = Arg("-shotSize", "1080x1920").Split('x').Select(int.Parse).ToArray();
        double timeout = double.Parse(Arg("-shotTimeout", "120"));

        Directory.CreateDirectory(outDir);
        _state = new State
        {
            Steps = steps.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray(),
            OutDir = outDir,
            Deadline = EditorApplication.timeSinceStartup + timeout,
        };
        Save();

        SetGameViewSize(size[0], size[1]);
        if (!string.IsNullOrEmpty(scene))
            EditorSceneManager.OpenScene(scene);

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (_state == null)
            return;

        if (EditorApplication.timeSinceStartup > _state.Deadline)
        {
            Debug.LogError($"[ScreenshotCapture] 시간 초과: {_state.Index}번 단계 {_state.Steps[Mathf.Min(_state.Index, _state.Steps.Length - 1)]}");
            Finish(1);
            return;
        }

        if (!EditorApplication.isPlaying)
            return;

        if (!string.IsNullOrEmpty(_state.PendingFile))
        {
            if (!File.Exists(_state.PendingFile))
                return;

            Debug.Log($"[ScreenshotCapture] 저장: {_state.PendingFile}");
            _state.PendingFile = null;
            Next();
            return;
        }

        if (_state.Index >= _state.Steps.Length)
        {
            Finish(0);
            return;
        }

        string step = _state.Steps[_state.Index];
        int colon = step.IndexOf(':');
        string kind = colon < 0 ? step : step.Substring(0, colon);
        string value = colon < 0 ? "" : step.Substring(colon + 1);

        switch (kind)
        {
            case "wait":
                if (_state.StepStart < 0)
                {
                    _state.StepStart = EditorApplication.timeSinceStartup;
                    Save();
                }
                if (EditorApplication.timeSinceStartup - _state.StepStart >= double.Parse(value))
                    Next();
                break;

            case "click":
                // 씬 로딩이 끝나기 전일 수 있어 나타날 때까지 기다린다 (시간 초과로 끝난다)
                var target = FindActive(value);
                if (target == null)
                    return;
                ExecuteEvents.ExecuteHierarchy(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                Debug.Log($"[ScreenshotCapture] 클릭: {value}");
                Next();
                break;

            case "shot":
                string file = Path.Combine(_state.OutDir, (string.IsNullOrEmpty(value) ? "screen" : value) + ".png");
                if (File.Exists(file))
                    File.Delete(file);
                ScreenCapture.CaptureScreenshot(file);
                _state.PendingFile = file;
                Save();
                break;

            default:
                Debug.LogError($"[ScreenshotCapture] 알 수 없는 단계: {step}");
                Finish(1);
                break;
        }
    }

    private static void Next()
    {
        _state.Index++;
        _state.StepStart = -1;
        Save();
    }

    private static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.EraseString(StateKey);
        _state = null;
        EditorApplication.Exit(code);
    }

    private static void Save()
    {
        SessionState.SetString(StateKey, JsonUtility.ToJson(_state));
    }

    private static GameObject FindActive(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == name);
                if (found != null)
                    return found.gameObject;
            }
        }
        return null;
    }

    private static string Arg(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
    }

    // Game 뷰 해상도는 공개 API가 없어 내부 타입을 리플렉션으로 쓴다
    private static void SetGameViewSize(int width, int height)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var groupType = sizesType.GetProperty("currentGroupType").GetValue(instance);
        var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
        var groupT = group.GetType();

        int total = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null);
        int index = -1;
        for (int i = 0; i < total; i++)
        {
            var size = groupT.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            var sizeT = size.GetType();
            if ((int)sizeT.GetProperty("width").GetValue(size) == width
                && (int)sizeT.GetProperty("height").GetValue(size) == height
                && (int)sizeT.GetProperty("sizeType").GetValue(size) == 1)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            var sizeType = asm.GetType("UnityEditor.GameViewSizeType");
            var ctor = asm.GetType("UnityEditor.GameViewSize").GetConstructor(new[] { sizeType, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new[] { Enum.ToObject(sizeType, 1), width, height, $"Shot {width}x{height}" });
            groupT.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
            index = total;
        }

        var gameViewType = asm.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        gameViewType.GetMethod("SizeSelectionCallback", all).Invoke(gameView, new object[] { index, null });
    }
}
