using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;

[InitializeOnLoad]
public class SceneBootstrapper
{
    const string PreviousSceneKey = "PreviousScene";
    const string BootstrapButtonPressedKey = "BootstrapButtonPressed";
    const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

    static bool IsBootstrapButtonPressed
    {
        get => EditorPrefs.GetBool(BootstrapButtonPressedKey, false);
        set => EditorPrefs.SetBool(BootstrapButtonPressedKey, value);
    }

    static string BootstrapScene =>
        EditorBuildSettings.scenes.Length > 0
            ? EditorBuildSettings.scenes[0].path
            : string.Empty;

    static string BootstrapSceneName =>
        string.IsNullOrEmpty(BootstrapScene)
            ? "No Scene"
            : System.IO.Path.GetFileNameWithoutExtension(BootstrapScene);

    static string PreviousScene
    {
        get => EditorPrefs.GetString(PreviousSceneKey);
        set => EditorPrefs.SetString(PreviousSceneKey, value);
    }

    static SceneBootstrapper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += RegisterToolbar;
    }

    static void RegisterToolbar()
    {
        Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null) return;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0) return;

        var visualTree = toolbars[0].GetType().GetProperty(
            "visualTree",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        )?.GetValue(toolbars[0]) as VisualElement;

        if (visualTree == null) return;

        var playModeButtons = visualTree.Q("PlayMode");
        if (playModeButtons == null) return;

        var btn = new Button();
        btn.text = $"Bootstrap";
        btn.AddToClassList("unity-editor-toolbar-element");
        btn.style.alignSelf = Align.Center;
        btn.clicked += () =>
        {
            IsBootstrapButtonPressed = true;
            EditorApplication.isPlaying = true;
        };

        var parent = playModeButtons.parent;
        int index = parent.IndexOf(playModeButtons);
        parent.Insert(index, btn);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (IsBootstrapButtonPressed)
            {
                PreviousScene = EditorSceneManager.GetActiveScene().path;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(BootstrapScenePath);
                }
                else
                {
                    EditorApplication.isPlaying = false;
                    IsBootstrapButtonPressed = false;
                }
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (IsBootstrapButtonPressed && !string.IsNullOrEmpty(PreviousScene))
            {
                EditorSceneManager.OpenScene(PreviousScene);
                IsBootstrapButtonPressed = false;
            }
        }
    }
}
