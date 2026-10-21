using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 웨이브 맵 컨셉 하나를 프리팹으로 만든다. 기준 수치는 WaveForest.prefab이며, 실행할 때마다 소품 배치를 새로 뽑아 기존 프리팹을 덮어쓴다.
public static class WaveMapBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Lobby";
    private const string FontPath = "Assets/Fonts/Pretendard/Pretendard-SemiBold SDF.asset";
    private const string ThemeFolder = "Assets/ThirdParty/Layer Lab/GUI Pro-MinimalGame";

    private const string Cream = "#FBF3E4";
    private const string CreamBorder = "#C9B48C";
    private const string BrownDark = "#5A3D2B";
    private const string NumberHex = "#7A5A35";

    private const float BaseOffset = 330f;
    private const float PixelsPerWave = 112f;
    private const int MaxWave = 50;
    private const float HalfScreenWidth = 540f;
    private const float PathVisibleWidth = 460f;
    private const float TileOverlap = 2f;
    private const float TopMargin = 760f;
    private const float HeavyPropSpacing = 450f;
    private const float PropSkipChance = 0.4f;
    private const float WaveLineClearance = 90f;
    private const int PropIndexRockSmall = 2;
    private const int PropIndexGrass = 3;
    private const int PropIndexFlower = 4;
    private const int PropIndexGrassAlt = 5;

    private static TMP_FontAsset _font;

    [MenuItem("SlimeTD/Wave/Build Wave Map Forest")]
    public static void BuildForest()
    {
        Build("Forest", "Assets/Art/UI/Lobby/WaveMap");
    }

    public static void Build(string concept, string artFolder)
    {
        string prefabPath = $"{PrefabFolder}/Wave{concept}.prefab";
        var ground = LoadSprites(artFolder, "Path_Dirt", "Bg_Grass_Left", "Bg_Grass_Right");
        var props = LoadSprites(artFolder, "Prop_Tree", "Prop_Rock", "Prop_RockSmall", "Prop_Grass1", "Prop_Flower", "Prop_Grass2");
        if (ground == null || props == null)
            return;

        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
            Debug.Log($"[WaveMapBuilder] 폰트를 찾지 못했습니다: {FontPath}");

        var root = BuildRoot($"Wave{concept}", ground, props);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[WaveMapBuilder] 웨이브 맵 프리팹 생성 완료: {prefabPath}");
    }

    private static GameObject BuildRoot(string name, Sprite[] ground, Sprite[] props)
    {
        float waveTopY = BaseOffset + PixelsPerWave * MaxWave;
        float contentHeight = waveTopY + TopMargin;

        var root = NewRect(name, null);
        root.sizeDelta = new Vector2(1080f, 1920f);
        var rootImage = root.gameObject.AddComponent<Image>();
        rootImage.color = new Color(1f, 1f, 1f, 0f);

        var viewport = NewRect("Viewport", root);
        Stretch(viewport);

        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 0f);
        content.pivot = new Vector2(0.5f, 0f);
        content.sizeDelta = new Vector2(0f, contentHeight);

        var grassLayer = NewRect("Layer_Grass", content);
        Stretch(grassLayer);
        var pathLayer = NewRect("Layer_Path", content);
        Stretch(pathLayer);
        var propLayer = NewRect("Layer_Props", content);
        Stretch(propLayer);

        // 풀밭은 중앙에서 화면 가장자리까지 채우고, 흙길을 그 위에 얹는다. 레이어마다 이미지 높이가 달라 각자 자기 높이로 쌓는다.
        float grassScale = HalfScreenWidth / ground[1].rect.width;
        TileVertically(grassLayer, "IMG_GrassLeft_", ground[1], 0f, 1f, grassScale, contentHeight);
        TileVertically(grassLayer, "IMG_GrassRight_", ground[2], 0f, 0f, grassScale, contentHeight);

        // 흙길 이미지는 좌우에 투명 여백이 있어, 실제 흙 폭이 PathVisibleWidth가 되도록 배율을 잡고 흙 중심을 화면 중앙에 맞춘다.
        PathSpan(ground[0], out float pathLeft, out float pathRight);
        float pathScale = PathVisibleWidth / (pathRight - pathLeft);
        float pathCenterX = ((pathLeft + pathRight) * 0.5f - ground[0].rect.width * 0.5f) * pathScale;
        TileVertically(pathLayer, "IMG_Path_", ground[0], -pathCenterX, 0.5f, pathScale, contentHeight);
        BuildProps(propLayer, props, contentHeight);

        for (int i = 1; i <= MaxWave / 10; i++)
        {
            var number = Label("TXT_Wave_" + i * 10, content, (i * 10).ToString(), 170, NumberHex, TextAlignmentOptions.Center, false,
                new Vector2(0.5f, 0f), new Vector2(0f, WaveY(i * 10)), new Vector2(300f, 200f), new Vector2(0.5f, 0.5f));
            number.alpha = 0.4f;
        }

        var marker = NewRect("PlayerMarker", content);
        Place(marker, new Vector2(0.5f, 0f), new Vector2(0f, BaseOffset), new Vector2(360f, 330f), new Vector2(0.5f, 0f));
        var character = Block("IMG_Player", marker, "#FFFFFF", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(150f, 200f), new Vector2(0.5f, 0f));
        SetThemeSprite(character, "Sample_Cha02_s", "Demo_Character");
        var tag = FrameBlock("IMG_BestTag", marker, new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(340f, 70f), new Vector2(0.5f, 0f));
        var bestText = Label("TXT_BestWave", tag, "도전 시작!", 34, BrownDark, TextAlignmentOptions.Center, true);

        var complete = NewRect("WaveComplete", content);
        Place(complete, new Vector2(0.5f, 0f), new Vector2(0f, waveTopY + 330f), new Vector2(620f, 420f), new Vector2(0.5f, 0.5f));

        var flag = Block("ICON_WaveCompleteFlag", complete, "#FFFFFF", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(240f, 240f), new Vector2(0.5f, 1f));
        SetThemeSprite(flag, "Item_Flag_01_Finish", "UniqueIcon/256");
        var banner = FrameBlock("FRAME_WaveComplete", complete, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(560f, 120f), new Vector2(0.5f, 0f));
        Label("TXT_WaveComplete", banner, "웨이브 완료", 56, BrownDark, TextAlignmentOptions.Center, true);

        var scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        var map = root.gameObject.AddComponent<WaveScrollView>();
        var so = new SerializedObject(map);
        so.FindProperty("_scroll").objectReferenceValue = scroll;
        so.FindProperty("_content").objectReferenceValue = content;
        so.FindProperty("_playerMarker").objectReferenceValue = marker;
        so.FindProperty("_bestWaveText").objectReferenceValue = bestText;
        so.FindProperty("_completeMark").objectReferenceValue = complete.gameObject;
        so.FindProperty("_baseOffset").floatValue = BaseOffset;
        so.FindProperty("_pixelsPerWave").floatValue = PixelsPerWave;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root.gameObject;
    }

    private static float WaveY(int wave)
    {
        return BaseOffset + PixelsPerWave * wave;
    }

    private static void TileVertically(RectTransform layer, string namePrefix, Sprite sprite, float x, float pivotX, float scale, float contentHeight)
    {
        float tileHeight = sprite.rect.height * scale - TileOverlap;
        int tileCount = Mathf.CeilToInt(contentHeight / tileHeight);

        for (int i = 0; i < tileCount; i++)
        {
            SpriteImage(namePrefix + i, layer, sprite, new Vector2(x, i * tileHeight), new Vector2(pivotX, 0f), scale, false, false);
        }
    }

    // 줄마다 다른 가장자리 위치는 평균을 쓴다.
    private static void PathSpan(Sprite sprite, out float left, out float right)
    {
        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));

        var pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        Object.DestroyImmediate(texture);

        float leftTotal = 0f;
        float rightTotal = 0f;
        int rows = 0;
        for (int y = 0; y < height; y++)
        {
            int first = -1;
            int last = -1;
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 128)
                    continue;

                if (first < 0)
                    first = x;

                last = x;
            }

            if (first < 0)
                continue;

            leftTotal += first;
            rightTotal += last + 1;
            rows++;
        }

        left = rows == 0 ? 0f : leftTotal / rows;
        right = rows == 0 ? width : rightTotal / rows;
    }

    // 웨이브 숫자 근처는 비운다.
    private static void BuildProps(RectTransform layer, Sprite[] sprites, float contentHeight)
    {
        float[] weights = { 0.1f, 0.15f, 0.15f, 0.4f, 0.2f };
        int seed = System.Environment.TickCount;
        var rng = new System.Random(seed);
        Debug.Log($"[WaveMapBuilder] 소품 시드: {seed}");
        var heavyYs = new List<float>();
        int count = 0;

        foreach (float side in new[] { -1f, 1f })
        {
            float y = 60f;
            int last = -1;
            while (y < contentHeight - 80f)
            {
                if (!NearWaveNumber(y) && rng.NextDouble() >= PropSkipChance)
                {
                    float roll = (float)rng.NextDouble();
                    int index = 0;
                    for (; index < weights.Length - 1; index++)
                    {
                        roll -= weights[index];
                        if (roll <= 0f)
                            break;
                    }

                    // 나무/바위는 좌우 구분 없이 일정 간격 이상 띄우고, 가까우면 풀/꽃으로 바꾼다
                    if (index <= PropIndexRockSmall && IsNearHeavy(heavyYs, y))
                        index = rng.Next(2) == 0 ? PropIndexGrass : PropIndexFlower;

                    if (index == last && index != PropIndexGrass)
                        index = PropIndexGrass;

                    last = index;
                    if (index <= PropIndexRockSmall)
                        heavyYs.Add(y);

                    float x = side * Mathf.Lerp(310f, 440f, (float)rng.NextDouble());
                    // 풀은 풀잎 2종 중 하나를 고르고, 크기는 원본의 50~100%를 10% 단위로 뽑는다
                    float scale = Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble()) * 1.2f;
                    var sprite = sprites[index];
                    if (index == PropIndexGrass)
                    {
                        sprite = sprites[rng.Next(2) == 0 ? PropIndexGrass : PropIndexGrassAlt];
                        scale = (5 + rng.Next(6)) * 0.1f;
                    }

                    bool flipX = rng.Next(2) == 0;
                    SpriteImage($"IMG_{sprite.name}_{count++}", layer, sprite, new Vector2(x, y), new Vector2(0.5f, 0f), scale, false, flipX);
                }

                y += Mathf.Lerp(300f, 650f, (float)rng.NextDouble());
            }
        }
    }

    private static bool IsNearHeavy(List<float> heavyYs, float y)
    {
        foreach (float heavyY in heavyYs)
        {
            if (Mathf.Abs(y - heavyY) < HeavyPropSpacing)
                return true;
        }

        return false;
    }

    private static bool NearWaveNumber(float y)
    {
        for (int wave = 10; wave <= MaxWave; wave += 10)
        {
            if (Mathf.Abs(y - WaveY(wave)) < WaveLineClearance)
                return true;
        }

        return false;
    }

    private static Sprite[] LoadSprites(string folder, params string[] names)
    {
        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            sprites[i] = LoadSprite($"{folder}/{names[i]}.png");
            if (sprites[i] == null)
                return null;
        }

        return sprites;
    }

    private static Sprite LoadSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.Log($"[WaveMapBuilder] 스프라이트를 찾지 못했습니다: {path}");
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency || importer.mipmapEnabled || importer.maxTextureSize < 4096)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SetThemeSprite(RectTransform rt, string spriteName, string pathHint, Color? tint = null)
    {
        Sprite sprite = null;
        foreach (var guid in AssetDatabase.FindAssets(spriteName + " t:Sprite", new[] { ThemeFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == spriteName && path.Contains(pathHint))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                break;
            }
        }

        if (sprite == null)
        {
            Debug.Log($"[WaveMapBuilder] 에셋을 찾지 못했습니다: {spriteName}");
            return;
        }

        var image = rt.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = image.type == Image.Type.Simple;
        image.color = tint ?? Color.white;
    }

    private static RectTransform FrameBlock(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        var rt = Block(name, parent, Cream, anchor, position, size, pivot);
        SetThemeSprite(rt, "BasicFrame_Rectangle_01~04_White_Bg", "Sprite_Common", ParseColor(Cream));

        var border = NewRect("IMG_Border", rt);
        Stretch(border);
        var image = border.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        SetThemeSprite(border, "BasicFrame_Rectangle_01~04_White_Border1", "Sprite_Common", ParseColor(CreamBorder));
        return rt;
    }

    private static RectTransform SpriteImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 pivot, float scale, bool flipY, bool flipX)
    {
        var rt = NewRect(name, parent);
        Place(rt, new Vector2(0.5f, 0f), position, sprite.rect.size * scale, pivot);
        rt.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return rt;
    }

    private static RectTransform Block(string name, Transform parent, string hex, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        var rt = NewRect(name, parent);
        Place(rt, anchor, position, size, pivot);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = ParseColor(hex);
        image.raycastTarget = false;
        return rt;
    }

    private static TMP_Text Label(string name, Transform parent, string text, float size, string hex, TextAlignmentOptions align, bool stretch,
        Vector2 anchor = default, Vector2 position = default, Vector2 sizeDelta = default, Vector2 pivot = default)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = ParseColor(hex);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (_font != null)
            tmp.font = _font;

        if (stretch)
            Stretch(rt);
        else
            Place(rt, anchor, position, sizeDelta, pivot);

        return tmp;
    }

    private static TMP_Text Label(string name, Transform parent, string text, float size, string hex, TextAlignmentOptions align, bool stretch)
    {
        return Label(name, parent, text, size, hex, align, stretch, default, default, default, default);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent, false);

        return (RectTransform)go.transform;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Color ParseColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }
}
