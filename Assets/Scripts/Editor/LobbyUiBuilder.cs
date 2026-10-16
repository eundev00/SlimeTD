using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 에셋 없이 단색 블록과 라벨로 로비 레이아웃을 만든다. 에셋을 입힐 자리는 IMG_/ICON_/FRAME_ 접두와 [대괄호] 라벨로 표시한다.
public static class LobbyUiBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Lobby";
    private const string PrefabPath = PrefabFolder + "/LobbyMain.prefab";
    private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
    private const string FontPath = "Assets/Fonts/Pretendard/Pretendard-SemiBold SDF.asset";

    private const string Background = "#EEF1EC";
    private const string Bar = "#DDE3DC";
    private const string Panel = "#E3E8E2";
    private const string White = "#FFFFFF";
    private const string Accent = "#2F7D6E";
    private const string TextDark = "#2A2E2B";
    private const string TextMuted = "#5E665F";
    private const string Placeholder = "#B9C3B8";

    private static TMP_FontAsset _font;

    [MenuItem("SlimeTD/Lobby/Build Lobby UI")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
            Debug.Log($"[LobbyUiBuilder] 폰트를 찾지 못했습니다: {FontPath}");

        var prefab = BuildPrefab();
        PlaceInScene(prefab);
        Debug.Log("[LobbyUiBuilder] 로비 UI 생성 완료");
    }

    private static GameObject BuildPrefab()
    {
        var root = NewRect("LobbyMain", null);
        Stretch(root);

        var header = BuildHeader(root);
        var content = NewRect("Content", root);
        Stretch(content);
        content.offsetMin = new Vector2(0f, 200f);
        content.offsetMax = new Vector2(0f, -150f);

        var cardPrefab = BuildHunterCardPrefab();
        var battle = BuildBattleTab(content);
        var hunter = BuildHunterTab(content, cardPrefab);
        BuildTabBar(root, battle.gameObject, hunter.gameObject);
        hunter.gameObject.SetActive(false);

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);

        header = null;
        return saved;
    }

    private static RectTransform BuildHeader(RectTransform parent)
    {
        var header = NewRect("Header", parent);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 150f);
        header.anchoredPosition = Vector2.zero;
        AddImage(header, Bar);

        var profile = Block("FRAME_Profile", header, Placeholder, TopLeft(20f, -20f, 110f, 110f));
        Label("TXT_Level", profile, "LV 1", 28, "#4A524B", true, TextAlignmentOptions.Center, true);

        Label("TXT_Nickname", header, "[닉네임]", 30, TextDark, true, TextAlignmentOptions.Left, false, TopLeft(145f, -26f, 220f, 44f));
        var expBg = Block("IMG_ExpBar", header, "#C3CBC2", TopLeft(145f, -84f, 200f, 14f));
        var expFill = Block("IMG_ExpFill", expBg, Accent, null);
        expFill.anchorMin = Vector2.zero;
        expFill.anchorMax = new Vector2(0.4f, 1f);
        expFill.offsetMin = Vector2.zero;
        expFill.offsetMax = Vector2.zero;

        var stamina = Block("Chip_Stamina", header, "#E9ECE8", TopLeft(380f, -40f, 200f, 70f));
        Label("TXT_Stamina", stamina, "[스태미나 자리]", 22, "#8E978F", false, TextAlignmentOptions.Center, true);

        var gold = Block("Chip_Gold", header, White, TopLeft(595f, -40f, 210f, 70f));
        Block("ICON_Gold", gold, "#E0B54A", Anchored(new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f)));
        var goldText = Label("TXT_Gold", gold, "0", 30, TextDark, true, TextAlignmentOptions.Right, false);
        SetOffsets(goldText.rectTransform, new Vector2(60f, 0f), new Vector2(-14f, 0f));

        var gem = Block("Chip_Gem", header, White, TopLeft(820f, -40f, 210f, 70f));
        Block("ICON_Gem", gem, "#6E8FD6", Anchored(new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f)));
        var gemText = Label("TXT_Gem", gem, "0", 30, TextDark, true, TextAlignmentOptions.Right, false);
        SetOffsets(gemText.rectTransform, new Vector2(60f, 0f), new Vector2(-14f, 0f));

        var view = header.gameObject.AddComponent<LobbyHeader>();
        var so = new SerializedObject(view);
        so.FindProperty("_goldText").objectReferenceValue = goldText;
        so.FindProperty("_gemText").objectReferenceValue = gemText;
        so.ApplyModifiedPropertiesWithoutUndo();

        return header;
    }

    private static RectTransform BuildBattleTab(RectTransform parent)
    {
        var tab = NewRect("BattleTab", parent);
        Stretch(tab);

        var mail = ButtonBlock("BTN_Mail", tab, White, "우편", 26, TextDark, false, TopRight(-340f, -20f, 150f, 80f));
        var quest = ButtonBlock("BTN_Quest", tab, White, "퀘스트", 26, TextDark, false, TopRight(-180f, -20f, 150f, 80f));
        var setting = ButtonBlock("BTN_Setting", tab, White, "설정", 26, TextDark, false, TopRight(-20f, -20f, 150f, 80f));
        mail = null; quest = null; setting = null;

        Label("TXT_StageLabel", tab, "STAGE 1", 30, TextMuted, false, TextAlignmentOptions.Center, false, TopCenter(0f, -130f, 600f, 44f));
        Label("TXT_StageName", tab, "말랑 숲", 72, TextDark, true, TextAlignmentOptions.Center, false, TopCenter(0f, -180f, 800f, 100f));

        var boss = Block("IMG_BossShowcase", tab, Panel, TopCenter(0f, -310f, 700f, 520f));
        Label("TXT_BossHint", boss, "[스테이지 대표 보스 슬라임 3D]", 28, TextMuted, false, TextAlignmentOptions.Center, true);
        ButtonBlock("BTN_PrevStage", tab, White, "<", 40, TextDark, true, Anchored(new Vector2(0f, 1f), new Vector2(60f, -520f), new Vector2(90f, 90f), new Vector2(0f, 1f)));
        ButtonBlock("BTN_NextStage", tab, White, ">", 40, TextDark, true, Anchored(new Vector2(1f, 1f), new Vector2(-60f, -520f), new Vector2(90f, 90f), new Vector2(1f, 1f)));

        var record = Block("IMG_BestRecord", tab, White, TopCenter(0f, -860f, 960f, 90f));
        Label("TXT_RecordLabel", record, "최고 기록", 28, TextMuted, false, TextAlignmentOptions.Left, false);
        var recordText = Label("TXT_RecordValue", record, "WAVE 0 / 50", 32, TextDark, true, TextAlignmentOptions.Right, false);
        SetOffsets(((TMP_Text)recordText).rectTransform, new Vector2(30f, 0f), new Vector2(-30f, 0f));
        var recordLabel = record.Find("TXT_RecordLabel");
        SetOffsets((RectTransform)recordLabel, new Vector2(30f, 0f), new Vector2(-30f, 0f));

        var devGame = ButtonBlock("BTN_DevGame", tab, "#D9DED8", "DEV 게임", 24, TextMuted, false, BottomLeft(30f, 190f, 300f, 70f));
        var devTest = ButtonBlock("BTN_DevTest", tab, "#D9DED8", "DEV 테스트", 24, TextMuted, false, BottomLeft(350f, 190f, 300f, 70f));
        var devGame2 = ButtonBlock("BTN_DevGame2", tab, "#D9DED8", "DEV 게임2", 24, TextMuted, false, BottomLeft(670f, 190f, 300f, 70f));
        devGame = null; devTest = null; devGame2 = null;

        var start = ButtonBlock("BTN_Start", tab, Accent, "전투 시작", 60, White, true, null);
        start.anchorMin = new Vector2(0f, 0f);
        start.anchorMax = new Vector2(1f, 0f);
        start.pivot = new Vector2(0.5f, 0f);
        start.offsetMin = new Vector2(30f, 30f);
        start.offsetMax = new Vector2(-30f, 160f);

        return tab;
    }

    private static RectTransform BuildHunterTab(RectTransform parent, HunterCard cardPrefab)
    {
        var tab = NewRect("HunterTab", parent);
        Stretch(tab);

        var showcase = Block("IMG_HunterShowcase", tab, Panel, null);
        showcase.anchorMin = new Vector2(0f, 1f);
        showcase.anchorMax = new Vector2(1f, 1f);
        showcase.pivot = new Vector2(0.5f, 1f);
        showcase.sizeDelta = new Vector2(0f, 380f);
        showcase.anchoredPosition = Vector2.zero;
        Label("TXT_ShowcaseHint", showcase, "[대표 헌터 쇼케이스]", 30, TextMuted, false, TextAlignmentOptions.Center, true);
        var pet = Block("IMG_PetSlot", showcase, "#D5DBD4", Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(520f, 80f), new Vector2(0.5f, 0f)));
        Label("TXT_PetHint", pet, "[펫 영역 (예정)]", 24, "#8E978F", false, TextAlignmentOptions.Center, true);

        var info = NewRect("InfoRow", tab);
        info.anchorMin = new Vector2(0f, 1f);
        info.anchorMax = new Vector2(1f, 1f);
        info.pivot = new Vector2(0.5f, 1f);
        info.sizeDelta = new Vector2(0f, 90f);
        info.anchoredPosition = new Vector2(0f, -400f);

        var cards = Block("Chip_Cards", info, White, TopLeft(30f, 0f, 300f, 80f));
        Block("ICON_Card", cards, "#E58A4A", Anchored(new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(44f, 44f)));
        var cardText = Label("TXT_Cards", cards, "0", 34, TextDark, true, TextAlignmentOptions.Right, false);
        SetOffsets(cardText.rectTransform, new Vector2(70f, 0f), new Vector2(-16f, 0f));

        var sort = ButtonBlock("BTN_Sort", info, White, "등급순", 28, TextDark, true, TopRight(-30f, 0f, 220f, 80f));

        var area = Block("IMG_GridArea", tab, "#F5F7F4", null);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(20f, 10f);
        area.offsetMax = new Vector2(-20f, -500f);

        var viewport = NewRect("Viewport", area);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        var grid = NewRect("Grid", viewport);
        grid.anchorMin = new Vector2(0f, 1f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.sizeDelta = Vector2.zero;
        grid.anchoredPosition = Vector2.zero;

        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.cellSize = new Vector2(230f, 290f);
        layout.spacing = new Vector2(15f, 20f);
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.childAlignment = TextAnchor.UpperCenter;

        var fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        var view = tab.gameObject.AddComponent<HunterTab>();
        var so = new SerializedObject(view);
        so.FindProperty("_grid").objectReferenceValue = grid;
        so.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
        so.FindProperty("_cardsText").objectReferenceValue = cardText;
        so.FindProperty("_sortButton").objectReferenceValue = sort.GetComponent<Button>();
        so.FindProperty("_sortLabel").objectReferenceValue = sort.Find("TXT_Label").GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return tab;
    }

    private static HunterCard BuildHunterCardPrefab()
    {
        const string cardPath = PrefabFolder + "/HunterCard.prefab";

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        var root = NewRect("HunterCard", null);
        root.sizeDelta = new Vector2(230f, 290f);

        var hit = AddImage(root, "#FFFFFF");
        hit.color = new Color(1f, 1f, 1f, 0f);
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;

        var borderRect = Block("FRAME_Border", root, "#A79F8F", Anchored(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(230f, 240f), new Vector2(0.5f, 1f)));
        var fillRect = Block("IMG_Fill", borderRect, "#BFB7A6", null);
        fillRect.offsetMin = new Vector2(8f, 8f);
        fillRect.offsetMax = new Vector2(-8f, -8f);

        var face = Block("ICON_Face", fillRect, "#FFFFFF", Anchored(new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(120f, 110f), new Vector2(0.5f, 1f)));
        face.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
        Label("TXT_FaceHint", face, "[얼굴]", 22, "#5E665F", false, TextAlignmentOptions.Center, true);

        var level = Label("TXT_Level", fillRect, "Lv.1", 34, White, true, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(200f, 44f), new Vector2(0.5f, 0f)));

        var progressRoot = Block("Progress", fillRect, "#000000", Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(200f, 38f), new Vector2(0.5f, 0f)));
        progressRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        var progressFill = Block("IMG_ProgressFill", progressRoot, "#3FA85A", null);
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = new Vector2(0f, 1f);
        progressFill.offsetMin = Vector2.zero;
        progressFill.offsetMax = Vector2.zero;
        var progressText = Label("TXT_Progress", progressRoot, "0 / 10", 22, White, true, TextAlignmentOptions.Center, true);

        var badge = Block("ICON_UpgradeBadge", borderRect, "#3FA85A", Anchored(new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(46f, 46f), new Vector2(1f, 1f)));
        Label("TXT_Badge", badge, "▲", 26, White, true, TextAlignmentOptions.Center, true);

        var lockOverlay = Block("Lock", fillRect, "#2A2E2B", null);
        lockOverlay.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.17f, 0.25f);
        Label("TXT_Lock", lockOverlay, "잠금", 30, "#5E665F", true, TextAlignmentOptions.Center, true);

        var name = Label("TXT_Name", root, "이름", 28, TextDark, false, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(230f, 44f), new Vector2(0.5f, 0f)));

        var card = root.gameObject.AddComponent<HunterCard>();
        var so = new SerializedObject(card);
        so.FindProperty("_border").objectReferenceValue = borderRect.GetComponent<Image>();
        so.FindProperty("_fill").objectReferenceValue = fillRect.GetComponent<Image>();
        so.FindProperty("_nameText").objectReferenceValue = name;
        so.FindProperty("_levelText").objectReferenceValue = level;
        so.FindProperty("_progressText").objectReferenceValue = progressText;
        so.FindProperty("_progressFill").objectReferenceValue = progressFill;
        so.FindProperty("_progressRoot").objectReferenceValue = progressRoot.gameObject;
        so.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay.gameObject;
        so.FindProperty("_upgradeBadge").objectReferenceValue = badge.gameObject;
        so.FindProperty("_button").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, cardPath);
        Object.DestroyImmediate(root.gameObject);

        return saved.GetComponent<HunterCard>();
    }

    private static void BuildTabBar(RectTransform parent, GameObject battlePanel, GameObject hunterPanel)
    {
        var bar = NewRect("TabBar", parent);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(0f, 200f);
        bar.anchoredPosition = Vector2.zero;
        AddImage(bar, Bar);

        string[] names = { "상점", "헌터", "전투", "컨텐츠1", "컨텐츠2" };
        bool[] locked = { true, false, false, true, true };
        GameObject[] panels = { null, hunterPanel, battlePanel, null, null };

        var tabs = bar.gameObject.AddComponent<LobbyTabs>();
        var so = new SerializedObject(tabs);
        var list = so.FindProperty("_tabs");
        list.arraySize = names.Length;

        for (int i = 0; i < names.Length; i++)
        {
            var button = NewRect("Tab_" + names[i], bar);
            button.anchorMin = new Vector2(i / 5f, 0f);
            button.anchorMax = new Vector2((i + 1) / 5f, 1f);
            button.offsetMin = new Vector2(4f, 4f);
            button.offsetMax = new Vector2(-4f, -4f);

            var image = AddImage(button, Bar);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.transition = Selectable.Transition.None;

            var icon = Block("ICON_" + names[i], button, Placeholder, Anchored(new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(64f, 64f), new Vector2(0.5f, 1f)));
            icon = null;
            var label = Label("TXT_Label", button, names[i], 30, TextMuted, false, TextAlignmentOptions.Center, false, Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(0f, 50f), new Vector2(0.5f, 0f)));
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.offsetMin = new Vector2(0f, 24f);
            label.rectTransform.offsetMax = new Vector2(0f, 74f);

            var element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_button").objectReferenceValue = btn;
            element.FindPropertyRelative("_panel").objectReferenceValue = panels[i];
            element.FindPropertyRelative("_background").objectReferenceValue = image;
            element.FindPropertyRelative("_label").objectReferenceValue = label;
            element.FindPropertyRelative("_locked").boolValue = locked[i];
        }

        so.FindProperty("_defaultIndex").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PlaceInScene(GameObject prefab)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var safeArea = FindInScene(scene, "SafeArea");
        if (safeArea == null)
        {
            Debug.Log("[LobbyUiBuilder] SafeArea를 찾지 못했습니다.");
            return;
        }

        var existing = safeArea.Find("LobbyMain");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, safeArea);
        instance.name = "LobbyMain";

        var bg = safeArea.Find("Bg");
        if (bg != null)
            instance.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);

        RewireStartButtons(safeArea, instance.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // 기존 개발용 시작 버튼은 지우지 않고 비활성화하며, LobbyStartButton의 참조를 새 버튼으로 옮긴다.
    private static void RewireStartButtons(Transform safeArea, Transform lobbyMain)
    {
        var lobbyStart = safeArea.Find("LobbyStart");
        if (lobbyStart == null)
            return;

        var component = lobbyStart.GetComponent<LobbyStartButton>();
        var start = lobbyMain.Find("Content/BattleTab/BTN_Start")?.GetComponent<Button>();
        var devTest = lobbyMain.Find("Content/BattleTab/BTN_DevTest")?.GetComponent<Button>();
        var devGame2 = lobbyMain.Find("Content/BattleTab/BTN_DevGame2")?.GetComponent<Button>();

        if (component != null)
        {
            var so = new SerializedObject(component);
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.FindProperty("_testStartButton").objectReferenceValue = devTest;
            so.FindProperty("_game2StartButton").objectReferenceValue = devGame2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (Transform child in lobbyStart)
            child.gameObject.SetActive(false);

        var devGame = lobbyMain.Find("Content/BattleTab/BTN_DevGame")?.gameObject;
        if (devGame != null)
            devGame.SetActive(false);
    }

    private static Transform FindInScene(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name)
            return t;

        foreach (Transform child in t)
        {
            var found = FindRecursive(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent, false);

        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    private static Color ParseColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }

    private static Image AddImage(RectTransform rt, string hex)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color = ParseColor(hex);
        return image;
    }

    // 위치 지정용 값: anchor/offset/size/pivot을 묶어 전달한다.
    private readonly struct Placement
    {
        public readonly Vector2 AnchorMin;
        public readonly Vector2 AnchorMax;
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly Vector2 Pivot;

        public Placement(Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            Position = position;
            Size = size;
            Pivot = pivot;
        }
    }

    private static Placement? TopLeft(float x, float y, float w, float h)
    {
        return new Placement(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(w, h), new Vector2(0f, 1f));
    }

    private static Placement? TopRight(float x, float y, float w, float h)
    {
        return new Placement(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(x, y), new Vector2(w, h), new Vector2(1f, 1f));
    }

    private static Placement? TopCenter(float x, float y, float w, float h)
    {
        return new Placement(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(w, h), new Vector2(0.5f, 1f));
    }

    private static Placement? BottomLeft(float x, float y, float w, float h)
    {
        return new Placement(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y), new Vector2(w, h), new Vector2(0f, 0f));
    }

    private static Placement? Anchored(Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        return new Placement(anchor, anchor, position, size, pivot ?? new Vector2(0.5f, 0.5f));
    }

    private static void Apply(RectTransform rt, Placement? placement)
    {
        if (!placement.HasValue)
            return;

        var p = placement.Value;
        rt.anchorMin = p.AnchorMin;
        rt.anchorMax = p.AnchorMax;
        rt.pivot = p.Pivot;
        rt.sizeDelta = p.Size;
        rt.anchoredPosition = p.Position;
    }

    private static RectTransform Block(string name, Transform parent, string hex, Placement? placement)
    {
        var rt = NewRect(name, parent);
        AddImage(rt, hex);

        if (placement.HasValue)
            Apply(rt, placement);
        else
            Stretch(rt);

        return rt;
    }

    private static TMP_Text Label(string name, Transform parent, string text, float size, string hex, bool bold,
        TextAlignmentOptions align, bool stretch, Placement? placement = null)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = ParseColor(hex);
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;

        if (_font != null)
            tmp.font = _font;

        if (placement.HasValue)
            Apply(rt, placement);
        else
            Stretch(rt);

        return tmp;
    }

    private static RectTransform ButtonBlock(string name, Transform parent, string hex, string text, float size,
        string textHex, bool bold, Placement? placement)
    {
        var rt = Block(name, parent, hex, placement);
        var image = rt.GetComponent<Image>();
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        Label("TXT_Label", rt, text, size, textHex, bold, TextAlignmentOptions.Center, true);
        return rt;
    }
}
