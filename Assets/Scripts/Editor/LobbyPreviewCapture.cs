using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LobbyPreviewCapture
{
    public static void Run()
    {
        var outDir = System.Environment.GetEnvironmentVariable("LOBBY_PREVIEW_DIR");
        LobbyUiBuilder.Build();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity", OpenSceneMode.Single);

        var main = FindIn(scene, "LobbyMain");
        var canvas = main.GetComponentInParent<Canvas>().rootCanvas;

        var camGo = new GameObject("PreviewCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.93f, 0.95f, 0.93f);
        cam.orthographic = true;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;

        var battle = main.Find("Content/BattleTab").gameObject;
        var hunter = main.Find("Content/HunterTab").gameObject;
        battle.SetActive(true);
        hunter.SetActive(false);

        var mapRoot = battle.transform.Find("MapScroll");
        var map = mapRoot.GetComponent<BattleMap>();
        var scroll = mapRoot.GetComponent<ScrollRect>();

        cam.targetTexture = new RenderTexture(1080, 1920, 24);
        Canvas.ForceUpdateCanvases();
        map.SetBestWave(23);
        Capture(cam, canvas, battle, hunter, Path.Combine(outDir, "battle_map_best.png"));

        var content = (RectTransform)scroll.content;
        var viewportHeight = ((RectTransform)scroll.viewport).rect.height;

        content.anchoredPosition = new Vector2(0f, -(1920f - viewportHeight * 0.5f));
        Capture(cam, canvas, battle, hunter, Path.Combine(outDir, "battle_map_seam1.png"));

        content.anchoredPosition = new Vector2(0f, -(content.rect.height - viewportHeight));
        Capture(cam, canvas, battle, hunter, Path.Combine(outDir, "battle_map_top.png"));

        content.anchoredPosition = Vector2.zero;
        Capture(cam, canvas, battle, hunter, Path.Combine(outDir, "battle_map_bottom.png"));
    }

    private static void Capture(Camera cam, Canvas canvas, GameObject show, GameObject hide, string path)
    {
        show.SetActive(true);
        hide.SetActive(false);

        var rt = new RenderTexture(1080, 1920, 24);
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.orthographicSize = 960f * canvas.transform.localScale.y;
        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;
    }

    private static Transform FindIn(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
        }

        return null;
    }
}
