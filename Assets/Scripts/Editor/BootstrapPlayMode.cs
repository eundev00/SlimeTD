using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;

[InitializeOnLoad]
public class BootstrapPlayMode
{
    private const string PreviousSceneKey = "BootstrapPlayMode_PreviousScene";
    private const string ButtonPressedKey = "BootstrapPlayMode_ButtonPressed";
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

    private static bool IsButtonPressed
    {
        get => EditorPrefs.GetBool(ButtonPressedKey, false);
        set => EditorPrefs.SetBool(ButtonPressedKey, value);
    }

    private static string PreviousScene
    {
        get => EditorPrefs.GetString(PreviousSceneKey);
        set => EditorPrefs.SetString(PreviousSceneKey, value);
    }

    static BootstrapPlayMode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += RegisterToolbar;
    }

    private static void RegisterToolbar()
    {
        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null)
            return;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0)
            return;

        var visualTree = toolbars[0].GetType().GetProperty(
            "visualTree",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        )?.GetValue(toolbars[0]) as VisualElement;

        if (visualTree == null)
            return;

        var playModeButtons = visualTree.Q("PlayMode");
        if (playModeButtons == null)
            return;

        var btn = new Button();
        btn.text = "Bootstrap";
        btn.AddToClassList("unity-editor-toolbar-element");
        btn.style.alignSelf = Align.Center;
        btn.clicked += () =>
        {
            IsButtonPressed = true;
            EditorApplication.isPlaying = true;
        };

        var parent = playModeButtons.parent;
        int index = parent.IndexOf(playModeButtons);
        parent.Insert(index, btn);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (!IsButtonPressed)
                return;

            PreviousScene = EditorSceneManager.GetActiveScene().path;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(BootstrapScenePath);
            }
            else
            {
                EditorApplication.isPlaying = false;
                IsButtonPressed = false;
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (!IsButtonPressed || string.IsNullOrEmpty(PreviousScene))
                return;

            EditorSceneManager.OpenScene(PreviousScene);
            IsButtonPressed = false;
        }
    }
}
