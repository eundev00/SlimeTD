using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 타워 프리팹을 정면에서 투명 배경으로 찍어 썸네일 PNG로 저장한다.
// 배치 실행: Unity.exe -batchmode -quit -executeMethod TowerThumbnailCapture.RunFromArgs -capturePrefab <경로> -captureOut <PNG 경로>
// 포즈는 기본이 Idle 상태의 첫 프레임이다. -capturePose <상태 이름> -captureTime <0~1 정규화 시간>으로 바꾼다.
public static class TowerThumbnailCapture
{
    private const int Size = 1024;
    private const float FieldOfView = 22f;
    private const float Margin = 1.12f;
    private const float FramePadding = 0.06f;
    private const float IdleSampleTime = 0f;
    private static readonly string[] HiddenNames = { "CircleSelect", "Quad" };

    [MenuItem("SlimeTD/Preview/Capture Selected Tower Thumbnail")]
    public static void CaptureSelected()
    {
        var selected = Selection.activeObject as GameObject;
        string path = selected == null ? null : AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("[TowerThumbnailCapture] 프로젝트 창에서 타워 프리팹을 선택하세요.");
            return;
        }

        string name = Path.GetFileNameWithoutExtension(path);
        Capture(path, $"Doc/Preview/Thumbnail/{name}_front.png");
    }

    // 캐릭터 폴더에서 BaseTower가 붙은 프리팹을 전부 찍는다. 무기/투사체 프리팹은 BaseTower가 없어 제외된다.
    [MenuItem("SlimeTD/Preview/Capture All Tower Thumbnails")]
    public static void CaptureAll()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Game/Characters" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<BaseTower>() != null)
            .ToList();

        foreach (string path in paths)
            Capture(path, $"Doc/Preview/Thumbnail/{Path.GetFileNameWithoutExtension(path)}_front.png");

        Debug.Log($"[TowerThumbnailCapture] 타워 {paths.Count}종 촬영 완료");
    }

    public static void RunFromArgs()
    {
        string prefabPath = GetArg("-capturePrefab");
        string outPath = GetArg("-captureOut");
        if (string.IsNullOrEmpty(prefabPath) || string.IsNullOrEmpty(outPath))
        {
            Debug.Log("[TowerThumbnailCapture] -capturePrefab, -captureOut 인자가 필요합니다.");
            return;
        }

        Capture(prefabPath, outPath);
    }

    public static void Capture(string prefabPath, string outputPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.Log($"[TowerThumbnailCapture] 프리팹을 찾지 못했습니다: {prefabPath}");
            return;
        }

        string fullPath = Path.IsPathRooted(outputPath)
            ? outputPath
            : Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

        var root = new GameObject("TowerThumbnailRoot");
        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (HiddenNames.Contains(t.name))
                    t.gameObject.SetActive(false);
            }

            SampleIdlePose(instance);
            RollWeapons(instance);
            var bounds = CalculateBounds(instance);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = FieldOfView;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.targetTexture = rt;

            // 모델 앞(+Z)에서 -Z 방향으로 바라본다. 거리는 바운즈가 화면에 꽉 차도록 계산한다.
            float radius = bounds.extents.magnitude;
            float distance = radius / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad) * Margin;
            camGo.transform.position = bounds.center + new Vector3(0f, 0f, distance);
            camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var texture = RenderToTexture(cam, rt);

            // 1차 결과에서 실제 그려진 영역을 재서 화면에 꽉 차도록 거리와 위치를 다시 잡는다.
            if (TryGetOpaqueBounds(texture, out var pixelBounds))
            {
                float worldPerPixel = 2f * distance * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) / Size;
                Vector2 offsetPixels = pixelBounds.center - new Vector2(Size * 0.5f, Size * 0.5f);
                camGo.transform.position += (camGo.transform.right * offsetPixels.x + camGo.transform.up * offsetPixels.y) * worldPerPixel;

                float fill = Mathf.Max(pixelBounds.width, pixelBounds.height) / (Size * (1f - FramePadding * 2f));
                camGo.transform.position += camGo.transform.forward * distance * (1f - fill);

                UnityEngine.Object.DestroyImmediate(texture);
                texture = RenderToTexture(cam, rt);
            }

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            Debug.Log($"[TowerThumbnailCapture] 저장 완료: {fullPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    private static Texture2D RenderToTexture(Camera cam, RenderTexture rt)
    {
        cam.Render();

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;
        return texture;
    }

    private static bool TryGetOpaqueBounds(Texture2D texture, out Rect bounds)
    {
        var pixels = texture.GetPixels32();
        int minX = Size, maxX = -1, minY = Size, maxY = -1;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (pixels[y * Size + x].a <= 10)
                    continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        bounds = maxX < 0 ? default : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return maxX >= 0;
    }

    // 에디터에서는 애니메이터가 돌지 않아 T 포즈로 남으므로 Idle 클립을 직접 샘플링한다.
    private static void SampleIdlePose(GameObject instance)
    {
        var animator = instance.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        string state = GetArg("-capturePose") ?? "Idle";
        var clip = FindStateClip(animator.runtimeAnimatorController, state);
        if (clip == null)
        {
            Debug.Log($"[TowerThumbnailCapture] {state} 클립을 찾지 못해 기본 포즈로 찍습니다.", instance);
            return;
        }

        float normalized = float.TryParse(GetArg("-captureTime"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : IdleSampleTime;
        clip.SampleAnimation(animator.gameObject, normalized * clip.length);
    }

    // 무기가 어색한 포즈를 찍을 때만 쓴다. 손 뼈대 위치를 축으로 화면 평면(카메라 방향 축)에서 무기를 돌린다. 프리팹은 건드리지 않는다.
    private static void RollWeapons(GameObject instance)
    {
        string names = GetArg("-captureWeapon");
        if (string.IsNullOrEmpty(names) || !float.TryParse(GetArg("-captureWeaponRoll"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var roll))
            return;

        var weapons = names.Split(',')
            .Select(name => instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name))
            .Where(t => t != null)
            .ToList();
        if (weapons.Count == 0)
        {
            Debug.Log($"[TowerThumbnailCapture] 무기 오브젝트를 찾지 못했습니다: {names}", instance);
            return;
        }

        Vector3 pivot = weapons[0].parent != null ? weapons[0].parent.position : weapons[0].position;
        foreach (var weapon in weapons)
        {
            // 다른 무기의 자식이면 부모가 돌 때 같이 돈다
            if (weapons.Any(other => other != weapon && weapon.IsChildOf(other)))
                continue;

            weapon.RotateAround(pivot, Vector3.forward, roll);
        }
    }

    // 클립 이름에 상태 이름이 없는 컨트롤러가 있어서 해당 상태에 연결된 클립을 먼저 찾는다.
    private static AnimationClip FindStateClip(RuntimeAnimatorController controller, string stateName)
    {
        var path = AssetDatabase.GetAssetPath(controller);
        var editorController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (editorController != null && editorController.layers.Length > 0)
        {
            foreach (var child in editorController.layers[0].stateMachine.states)
            {
                if (child.state.name == stateName && child.state.motion is AnimationClip stateClip)
                    return stateClip;
            }
        }

        return controller.animationClips
            .FirstOrDefault(c => c.name.IndexOf(stateName, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static Bounds CalculateBounds(GameObject instance)
    {
        Bounds bounds = default;
        bool first = true;

        foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (renderer is SkinnedMeshRenderer skinned)
                skinned.updateWhenOffscreen = true;

            if (first)
            {
                bounds = renderer.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private static bool HasArg(string name)
    {
        return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return null;
    }
}
