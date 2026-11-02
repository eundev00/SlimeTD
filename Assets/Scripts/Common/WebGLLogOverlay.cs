#if UNITY_WEBGL && (DEVELOPMENT_BUILD || SLIMETD_WEB_DEBUG)
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// iOS Safari는 PC 없이 콘솔을 볼 수 없어서, WebGL 확인용 빌드에서만 화면에 로그 창을 띄운다 (SLIMETD_WEB_DEBUG는 WebGLBuilder가 빌드 때만 넣는다).
public class WebGLLogOverlay : MonoBehaviour
{
    private const int MaxLines = 200;

    private readonly Queue<string> _lines = new Queue<string>();
    private readonly StringBuilder _builder = new StringBuilder();
    private string _text = string.Empty;
    private int _errorCount;
    private bool _open;
    private Vector2 _scroll;
    private GUIStyle _textStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var go = new GameObject(nameof(WebGLLogOverlay));
        DontDestroyOnLoad(go);
        go.AddComponent<WebGLLogOverlay>();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        bool isError = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
        if (isError)
            _errorCount++;

        string line = isError ? $"<color=#ff6b6b>[{type}] {message}\n{stackTrace}</color>" : message;
        _lines.Enqueue(line);
        while (_lines.Count > MaxLines)
            _lines.Dequeue();

        _builder.Clear();
        foreach (var l in _lines)
            _builder.AppendLine(l);
        _text = _builder.ToString();
    }

    private void OnGUI()
    {
        float scale = Screen.width / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = 540f;
        float height = Screen.height / scale;

        string label = _errorCount > 0 ? $"LOG ({_errorCount})" : "LOG";
        if (GUI.Button(new Rect(width - 110f, 8f, 100f, 36f), label))
            _open = !_open;

        if (!_open)
            return;

        if (_textStyle == null)
            _textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 };

        var area = new Rect(8f, 52f, width - 16f, height * 0.55f);
        GUI.Box(area, GUIContent.none);
        GUILayout.BeginArea(area);
        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.Label(_text, _textStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (GUI.Button(new Rect(8f, area.yMax + 6f, 100f, 32f), "Clear"))
        {
            _lines.Clear();
            _text = string.Empty;
            _errorCount = 0;
        }
    }
}
#endif
