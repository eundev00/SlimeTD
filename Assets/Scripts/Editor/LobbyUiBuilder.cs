using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private const string Sky = "#86E1FF";
    private const string Cream = "#FBF3E4";
    private const string Brown = "#4B3A2A";
    private const string BrownText = "#7A4A22";

    private const string ThemeFolder = "Assets/ThirdParty/Layer Lab/GUI Pro-MinimalGame";
    private const string ThemePrefabFolder = ThemeFolder + "/Theme_Light/Prefabs";

    private static TMP_FontAsset _font;

    private static T FindAsset<T>(string name, string folder, string pathHint = null) where T : Object
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:" + typeof(T).Name, new[] { folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != name)
                continue;

            if (pathHint != null && !path.Contains(pathHint))
                continue;

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        Debug.Log($"[LobbyUiBuilder] 에셋을 찾지 못했습니다: {name}");
        return null;
    }

    private static Image SetSprite(Image image, string spriteName, string pathHint = null, Color? tint = null)
    {
        var sprite = FindAsset<Sprite>(spriteName, ThemeFolder, pathHint);
        if (sprite == null)
            return image;

        image.sprite = sprite;
        image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = image.type == Image.Type.Simple;
        image.color = tint ?? Color.white;
        return image;
    }

    private static Image SetIcon(RectTransform rt, string spriteName, string pathHint = "PictoIcon/128")
    {
        var image = rt.GetComponent<Image>();
        return SetSprite(image, spriteName, pathHint);
    }

    // 둥근 사각 배경 + 테두리 스프라이트로 만든 패널. 에셋이 흰색이라 색은 틴트로 준다.
    private static RectTransform FrameBlock(string name, Transform parent, string fillHex, string borderHex, Placement? placement)
    {
        var rt = Block(name, parent, fillHex, placement);
        SetSprite(rt.GetComponent<Image>(), "BasicFrame_Rectangle_01~04_White_Bg", "Sprite_Common", ParseColor(fillHex));

        var border = Block("IMG_Border", rt, borderHex, null);
        border.GetComponent<Image>().raycastTarget = false;
        SetSprite(border.GetComponent<Image>(), "BasicFrame_Rectangle_01~04_White_Border1", "Sprite_Common", ParseColor(borderHex));
        return rt;
    }

    private static RectTransform Skin(string prefabName, string name, Transform parent, Placement? placement)
    {
        var prefab = FindAsset<GameObject>(prefabName, ThemePrefabFolder);
        if (prefab == null)
            return Block(name, parent, Placeholder, placement);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;

        var rt = (RectTransform)go.transform;
        if (placement.HasValue)
            Apply(rt, placement);
        else
            Stretch(rt);

        // 에셋 폰트에는 한글이 없다
        if (_font != null)
        {
            foreach (var tmp in rt.GetComponentsInChildren<TMP_Text>(true))
                tmp.font = _font;
        }

        return rt;
    }

    private static TMP_Text SetLabel(RectTransform rt, string text)
    {
        var tmp = rt.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
            return null;

        tmp.text = text;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = tmp.fontSize;
        tmp.fontSizeMin = 20f;
        return tmp;
    }

    private static Button EnsureButton(RectTransform rt)
    {
        var button = rt.GetComponent<Button>();
        if (button == null)
            button = rt.gameObject.AddComponent<Button>();

        if (button.targetGraphic == null)
            button.targetGraphic = rt.GetComponentInChildren<Image>(true);

        return button;
    }

    private static RectTransform SkinButton(string prefabName, string name, Transform parent, string text, Placement? placement, string textHex = null)
    {
        var rt = Skin(prefabName, name, parent, placement);
        EnsureButton(rt);
        var label = SetLabel(rt, text);
        if (label != null && textHex != null)
            label.color = ParseColor(textHex);

        return rt;
    }

    [MenuItem("SlimeTD/Lobby/Build Lobby UI")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
            Debug.Log($"[LobbyUiBuilder] 폰트를 찾지 못했습니다: {FontPath}");

        var prefab = BuildPrefab();
        BuildHunterDetailPopupPrefab();
        PlaceInScene(prefab);
        Debug.Log("[LobbyUiBuilder] 로비 UI 생성 완료");
    }

    private static void BuildHunterDetailPopupPrefab()
    {
        const string popupPath = PrefabFolder + "/HunterDetailPopup.prefab";
        const string tanHex = "#D8C7A3";

        var root = NewRect("HunterDetailPopup", null);
        Stretch(root);

        var dim = Block("IMG_Dim", root, "#141815", null);
        dim.GetComponent<Image>().color = new Color(0.078f, 0.094f, 0.082f, 0.86f);

        var chips = NewRect("Chips", root);
        chips.anchorMin = new Vector2(1f, 1f);
        chips.anchorMax = new Vector2(1f, 1f);
        chips.pivot = new Vector2(1f, 1f);
        chips.anchoredPosition = new Vector2(-30f, -40f);
        var chipLayout = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
        chipLayout.spacing = 16f;
        chipLayout.childAlignment = TextAnchor.UpperRight;
        chipLayout.childControlWidth = true;
        chipLayout.childControlHeight = true;
        chipLayout.childForceExpandWidth = false;
        chipLayout.childForceExpandHeight = false;
        var chipFitter = chips.gameObject.AddComponent<ContentSizeFitter>();
        chipFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        chipFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var cardsChip = BuildPopupChip(chips, "Chip_Cards", "ResourceBar_Icon_Ticket", "Cards", out var cardsChipText);
        var goldChip = BuildPopupChip(chips, "Chip_Gold", "ResourceBar_Icon_Gold", "Gold", out var goldChipText);
        var gemChip = BuildPopupChip(chips, "Chip_Gem", "ResourceBar_Icon_Gem", "Gem", out var gemChipText);

        var group = NewRect("Group", root);
        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.sizeDelta = new Vector2(900f, 1670f);
        group.anchoredPosition = Vector2.zero;

        var fill = FrameBlock("FRAME_Panel", group, "#F6EBD0", "#8A6A45", TopCenter(0f, 0f, 900f, 1500f));

        var shadow = Block("IMG_HeroShadow", fill, White, TopCenter(0f, -395f, 380f, 56f));
        shadow.GetComponent<Image>().raycastTarget = false;
        SetSprite(shadow.GetComponent<Image>(), "Image_Circle_256", "Demo_Image", new Color(0.55f, 0.43f, 0.27f, 0.3f));
        shadow.GetComponent<Image>().preserveAspect = false;

        var hero = Block("IMG_Hero", fill, White, TopCenter(0f, -30f, 270f, 370f));
        hero.GetComponent<Image>().raycastTarget = false;
        SetSprite(hero.GetComponent<Image>(), "Sample_Cha02_l", "Demo_Character");
        var heroBadge = Skin("Badge_Crimped_01_Yellow", "FRAME_GradeBadge", hero, Anchored(new Vector2(0f, 1f), new Vector2(-10f, -6f), new Vector2(76f, 76f), new Vector2(0f, 1f)));
        var heroBadgeText = heroBadge.GetComponentInChildren<TMP_Text>(true);
        if (heroBadgeText == null)
            heroBadgeText = Label("TXT_GradeBadge", heroBadge, "1", 34, BrownDark, true, TextAlignmentOptions.Center, true);

        foreach (var extra in heroBadge.GetComponentsInChildren<TMP_Text>(true))
        {
            if (extra != heroBadgeText)
                extra.gameObject.SetActive(false);
        }

        heroBadgeText.enableAutoSizing = true;
        heroBadgeText.fontSizeMax = 40f;
        heroBadgeText.fontSizeMin = 20f;

        var nameText = Label("TXT_Name", fill, "궁수", 104, BrownDark, true, TextAlignmentOptions.Center, false, TopCenter(0f, -430f, 860f, 120f));

        var gradeTag = FrameBlock("IMG_GradeTag", fill, White, White, TopCenter(0f, -562f, 190f, 52f));
        var gradeTagBorder = gradeTag.Find("IMG_Border").GetComponent<Image>();
        var gradeTagText = Label("TXT_GradeTag", gradeTag, "일반", 28, BrownDark, true, TextAlignmentOptions.Center, true);

        var levelRow = NewRect("LevelRow", fill);
        Apply(levelRow, TopCenter(0f, -650f, 760f, 66f));
        var levelText = Label("TXT_Level", levelRow, "Lv.1/15", 40, BrownDark, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(200f, 66f), new Vector2(0f, 0.5f)));

        var bar = NewRect("Bar", levelRow);
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        bar.offsetMin = new Vector2(220f, -25f);
        bar.offsetMax = new Vector2(-76f, 25f);
        var slider = Skin("Slider_Upgrade_01", "Slider", bar, null);
        StyleGauge(slider);
        var innerOrange = FindRecursive(slider, "Slider_02_Orange");
        if (innerOrange != null)
        {
            var innerText = innerOrange.GetComponentInChildren<TMP_Text>(true);
            if (innerText != null)
                innerText.gameObject.SetActive(false);
        }

        var barFill = FindRecursive(slider, "Fill") as RectTransform;
        var upgradeBar = FindRecursive(slider, "Upgrade");
        var lockBar = FindRecursive(slider, "Lock");
        var upgradeIcon = FindRecursive(slider, "Icon_Upgrade");
        if (upgradeIcon != null)
            upgradeIcon.gameObject.SetActive(false);

        if (lockBar != null)
            lockBar.gameObject.SetActive(false);

        TMP_Text progressText = null;
        foreach (Transform child in slider)
        {
            if (child.name == "Text (TMP)")
                progressText = child.GetComponent<TMP_Text>();
        }

        if (progressText != null)
        {
            progressText.fontSizeMax = 40f;
            progressText.fontSizeMin = 20f;
        }

        if (upgradeBar != null)
            upgradeBar.gameObject.SetActive(false);

        var arrow = Block("ICON_ArrowBadge", levelRow, White, Anchored(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(56f, 52f), new Vector2(1f, 0.5f)));
        arrow.GetComponent<Image>().raycastTarget = false;
        SetSprite(arrow.GetComponent<Image>(), "Icon_Arrow_Up_02", "Theme_Light");

        var lockedRow = NewRect("LockedRow", fill);
        Apply(lockedRow, TopCenter(0f, -650f, 760f, 66f));
        Block("IMG_LockedLineLeft", lockedRow, "#C9B48C", Anchored(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(230f, 4f), new Vector2(0f, 0.5f)));
        Block("IMG_LockedLineRight", lockedRow, "#C9B48C", Anchored(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(230f, 4f), new Vector2(1f, 0.5f)));
        var lockIcon = Block("ICON_Lock", lockedRow, White, Anchored(new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(52f, 52f)));
        lockIcon.GetComponent<Image>().raycastTarget = false;
        SetSprite(lockIcon.GetComponent<Image>(), "UI_Common_Lock_01_Silver", "UniqueIcon/128");
        Label("TXT_Locked", lockedRow, "잠김", 40, "#8A7A5C", true, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(200f, 66f)));
        lockedRow.gameObject.SetActive(false);

        var attackTile = FrameBlock("FRAME_AttackTile", fill, tanHex, "#C9B48C", TopCenter(-194f, -745f, 368f, 150f));
        var attackIcon = Block("ICON_Attack", attackTile, White, Anchored(new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(80f, 80f), new Vector2(0f, 0.5f)));
        attackIcon.GetComponent<Image>().raycastTarget = false;
        SetSprite(attackIcon.GetComponent<Image>(), "Gear_Weapons_Sword_01", "UniqueIcon/128");
        Label("TXT_AttackLabel", attackTile, "공격력", 26, "#7A4A22", false, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -16f), new Vector2(220f, 40f), new Vector2(0f, 1f)));
        var attackValue = Label("TXT_AttackValue", attackTile, "100", 64, BrownDark, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -56f), new Vector2(236f, 80f), new Vector2(0f, 1f)));

        var speedTile = FrameBlock("FRAME_SpeedTile", fill, tanHex, "#C9B48C", TopCenter(194f, -745f, 368f, 150f));
        var speedIcon = Block("ICON_Speed", speedTile, White, Anchored(new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(80f, 80f), new Vector2(0f, 0.5f)));
        speedIcon.GetComponent<Image>().raycastTarget = false;
        SetSprite(speedIcon.GetComponent<Image>(), "atk_spd", "PictoIcon/128", ParseColor("#7A4A22"));
        Label("TXT_SpeedLabel", speedTile, "공격 속도", 26, "#7A4A22", false, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -16f), new Vector2(220f, 40f), new Vector2(0f, 1f)));
        var speedValue = Label("TXT_SpeedValue", speedTile, "1.0", 64, BrownDark, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -56f), new Vector2(230f, 80f), new Vector2(0f, 1f)));

        Label("TXT_SkillHeader", fill, "기본 능력 · 스킬", 30, "#7A4A22", true, TextAlignmentOptions.Center, false, TopCenter(0f, -930f, 860f, 45f));
        for (int i = 0; i < 3; i++)
        {
            var slot = FrameBlock("IMG_SkillSlot_" + (i + 1), fill, tanHex, "#C9B48C", TopCenter((i - 1) * 200f, -990f, 160f, 176f));
            Label("TXT_SkillHint", slot, "[스킬]", 24, "#8A7A5C", false, TextAlignmentOptions.Center, true);
        }

        var action = Skin("Button_01_Yellow", "BTN_Action", fill, TopCenter(0f, -1280f, 760f, 170f));
        var actionButton = EnsureButton(action);
        actionButton.transition = Selectable.Transition.None;
        var actionBg = FindRecursive(action, "Bg");
        var actionImage = actionBg != null ? actionBg.GetComponent<Image>() : action.GetComponentInChildren<Image>(true);
        var actionLabel = action.GetComponentInChildren<TMP_Text>(true);
        actionLabel.text = "레벨업";
        actionLabel.enableAutoSizing = true;
        actionLabel.fontSizeMax = 60f;
        actionLabel.fontSizeMin = 30f;
        Apply(actionLabel.rectTransform, Anchored(new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(700f, 80f), new Vector2(0.5f, 1f)));
        var costPill = Block("IMG_CostPill", action, White, Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(420f, 64f), new Vector2(0.5f, 0f)));
        costPill.GetComponent<Image>().raycastTarget = false;
        SetSprite(costPill.GetComponent<Image>(), "BasicFrame_Rectangle_01~04_White_Bg", "Sprite_Common", new Color(0.1f, 0.1f, 0.12f, 0.9f));
        var costIcon = Block("ICON_ActionCost", costPill, White, Anchored(new Vector2(0.5f, 0.5f), new Vector2(-90f, 0f), new Vector2(48f, 48f)));
        costIcon.GetComponent<Image>().raycastTarget = false;
        SetSprite(costIcon.GetComponent<Image>(), "ResourceBar_Icon_Gold", "Theme_Light");
        var costText = Label("TXT_ActionCost", costPill, "100", 44, White, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0.5f, 0.5f), new Vector2(-50f, 0f), new Vector2(240f, 56f), new Vector2(0f, 0.5f)));

        var lockedNotice = Label("TXT_LockedNotice", fill, "잠김 · 획득 방법 미정", 40, "#8A7A5C", true, TextAlignmentOptions.Center, false, TopCenter(0f, -1320f, 760f, 90f));
        lockedNotice.gameObject.SetActive(false);

        var closeFrame = Skin("Button_Close_Circle_01", "BTN_Close", group, Anchored(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(130f, 130f), new Vector2(0.5f, 0f)));
        var closeButton = EnsureButton(closeFrame);

        var popup = root.gameObject.AddComponent<HunterDetailPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("_cardsChip").objectReferenceValue = cardsChip.gameObject;
        so.FindProperty("_cardsChipText").objectReferenceValue = cardsChipText;
        so.FindProperty("_goldChip").objectReferenceValue = goldChip.gameObject;
        so.FindProperty("_goldChipText").objectReferenceValue = goldChipText;
        so.FindProperty("_gemChip").objectReferenceValue = gemChip.gameObject;
        so.FindProperty("_gemChipText").objectReferenceValue = gemChipText;
        so.FindProperty("_heroImage").objectReferenceValue = hero.GetComponent<Image>();
        so.FindProperty("_heroGradeBadgeText").objectReferenceValue = heroBadgeText;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_gradeTagBackground").objectReferenceValue = gradeTag.GetComponent<Image>();
        so.FindProperty("_gradeTagBorder").objectReferenceValue = gradeTagBorder;
        so.FindProperty("_gradeTagText").objectReferenceValue = gradeTagText;
        so.FindProperty("_levelRow").objectReferenceValue = levelRow.gameObject;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_progressFill").objectReferenceValue = barFill;
        so.FindProperty("_progressText").objectReferenceValue = progressText;
        so.FindProperty("_arrowBadge").objectReferenceValue = arrow.gameObject;
        so.FindProperty("_lockedRow").objectReferenceValue = lockedRow.gameObject;
        so.FindProperty("_attackValueText").objectReferenceValue = attackValue;
        so.FindProperty("_speedValueText").objectReferenceValue = speedValue;
        so.FindProperty("_actionButton").objectReferenceValue = actionButton;
        so.FindProperty("_actionBackground").objectReferenceValue = actionImage;
        so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
        so.FindProperty("_actionCostText").objectReferenceValue = costText;
        so.FindProperty("_actionCostIcon").objectReferenceValue = costIcon.GetComponent<Image>();
        so.FindProperty("_lockedNoticeText").objectReferenceValue = lockedNotice;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton;
        so.FindProperty("_goldCostSprite").objectReferenceValue = FindAsset<Sprite>("ResourceBar_Icon_Gold", ThemeFolder, "Theme_Light");
        so.FindProperty("_gemCostSprite").objectReferenceValue = FindAsset<Sprite>("ResourceBar_Icon_Gem", ThemeFolder, "Theme_Light");
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, popupPath);
        Object.DestroyImmediate(root.gameObject);
    }

    private static RectTransform BuildPopupChip(Transform parent, string name, string iconSprite, string valueName, out TMP_Text value)
    {
        var chip = ResourceChip(name, parent, iconSprite, valueName, null, out value);
        var element = chip.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 240f;
        element.preferredHeight = 80f;
        return chip;
    }

    // 팝업이 붙는 오버레이 Canvas. PopupService가 Container 아래에 팝업을 만든다.
    private static RectTransform EnsurePopupRoot(UnityEngine.SceneManagement.Scene scene)
    {
        var existing = FindInScene(scene, "PopupRoot");
        if (existing == null)
        {
            var go = new GameObject("PopupRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var container = NewRect("Container", go.transform);
            Stretch(container);
            existing = go.transform;
        }

        return (RectTransform)existing.Find("Container");
    }

    private static void WirePopupRoot(UnityEngine.SceneManagement.Scene scene, RectTransform container)
    {
        var scopeTransform = FindInScene(scene, "LobbyLifetimeScope");
        var scope = scopeTransform != null ? scopeTransform.GetComponent<LobbyLifetimeScope>() : null;
        if (scope == null)
        {
            Debug.Log("[LobbyUiBuilder] LobbyLifetimeScope를 찾지 못했습니다.");
            return;
        }

        var so = new SerializedObject(scope);
        so.FindProperty("_popupRoot").objectReferenceValue = container;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private const string Beige = "#EBDEC0";
    private const string BeigeBand = "#E3CDAA";
    private const string BrownDark = "#5A3D2B";
    private const float HeaderHeight = 150f;
    private const float TabBarHeight = 200f;

    private static GameObject BuildPrefab()
    {
        var root = NewRect("LobbyMain", null);
        Stretch(root);

        var background = Block("IMG_Background", root, Beige, null);
        background.GetComponent<Image>().raycastTarget = false;

        BuildHeader(root);
        var content = NewRect("Content", root);
        Stretch(content);
        content.offsetMin = new Vector2(0f, TabBarHeight);
        content.offsetMax = new Vector2(0f, -HeaderHeight);

        var cardPrefab = BuildHunterCardPrefab();
        var battle = BuildBattleTab(content);
        var hunter = BuildHunterTab(content, cardPrefab);
        BuildTabBar(root, battle.gameObject, hunter.gameObject);
        hunter.gameObject.SetActive(false);

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    private static RectTransform ResourceChip(string name, Transform parent, string iconSprite, string valueName, Placement? placement, out TMP_Text value)
    {
        var chip = Block(name, parent, White, placement);
        SetSprite(chip.GetComponent<Image>(), "ResourceBar_Bg", "Theme_Light", new Color(0.4f, 0.34f, 0.27f));
        var icon = Block("ICON_" + valueName, chip, White, Anchored(new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(52f, 52f)));
        icon.GetComponent<Image>().raycastTarget = false;
        SetSprite(icon.GetComponent<Image>(), iconSprite, "Theme_Light");
        value = Label("TXT_" + valueName, chip, "0", 30, White, true, TextAlignmentOptions.Right, false);
        SetOffsets(value.rectTransform, new Vector2(70f, 0f), new Vector2(-14f, 0f));
        return chip;
    }

    private static RectTransform BuildHeader(RectTransform parent)
    {
        var header = NewRect("Header", parent);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderHeight);
        header.anchoredPosition = Vector2.zero;
        Block("IMG_HeaderBand", header, BeigeBand, null).GetComponent<Image>().raycastTarget = false;

        var profile = Skin("ProfileFrame_02_Yellow", "FRAME_Profile", header, TopLeft(20f, -20f, 110f, 110f));
        var levelBadge = FrameBlock("FRAME_LevelBadge", profile, "#F2A33A", Brown, Anchored(new Vector2(1f, 0f), new Vector2(8f, -8f), new Vector2(76f, 40f), new Vector2(1f, 0f)));
        Label("TXT_Level", levelBadge, "LV 1", 24, White, true, TextAlignmentOptions.Center, true);

        Label("TXT_Nickname", header, "[닉네임]", 30, BrownDark, true, TextAlignmentOptions.Left, false, TopLeft(145f, -26f, 220f, 44f));
        var expBg = Block("IMG_ExpBar", header, "#C9B48C", TopLeft(145f, -84f, 200f, 14f));
        var expFill = Block("IMG_ExpFill", expBg, "#F2B83A", null);
        expFill.anchorMin = Vector2.zero;
        expFill.anchorMax = new Vector2(0.4f, 1f);
        expFill.offsetMin = Vector2.zero;
        expFill.offsetMax = Vector2.zero;

        ResourceChip("Chip_Gold", header, "ResourceBar_Icon_Gold", "Gold", TopLeft(595f, -40f, 210f, 70f), out var goldText);
        ResourceChip("Chip_Gem", header, "ResourceBar_Icon_Gem", "Gem", TopLeft(820f, -40f, 210f, 70f), out var gemText);

        var view = header.gameObject.AddComponent<LobbyHeader>();
        var so = new SerializedObject(view);
        so.FindProperty("_goldText").objectReferenceValue = goldText;
        so.FindProperty("_gemText").objectReferenceValue = gemText;
        so.ApplyModifiedPropertiesWithoutUndo();

        return header;
    }

    private const float MapBaseOffset = 330f;
    private const float MapPixelsPerWave = 112f;
    private const int MapMaxWave = 50;
    private const string WaveMapFolder = "Assets/Art/UI/Lobby/WaveMap";
    private const float MapSpriteScale = 1.7f;
    private const float MapGrassEdge = 230f;
    private const float MapTopMargin = 760f;
    private const int MapPropSeed = 20260;
    private const float MapHeavyPropSpacing = 450f;
    private const int PropIndexRockSmall = 2;
    private const int PropIndexGrass = 3;
    private const int PropIndexFlower = 4;

    private static Sprite LoadWaveMapSprite(string name)
    {
        string path = $"{WaveMapFolder}/{name}.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.Log($"[LobbyUiBuilder] 웨이브 맵 스프라이트를 찾지 못했습니다: {path}");
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency || importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform MapSprite(string name, Transform parent, Sprite sprite, Vector2 anchoredPos, Vector2 pivot, float scale, bool flipY = false, bool flipX = false)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = pivot;
        rt.sizeDelta = sprite.rect.size * scale;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return rt;
    }

    private static RectTransform BuildBattleTab(RectTransform parent)
    {
        var tab = NewRect("BattleTab", parent);
        Stretch(tab);

        BuildBattleMap(tab);

        SkinButton("Button_01_OffWhite", "BTN_Mail", tab, "우편", TopRight(-340f, -20f, 150f, 80f), BrownDark);
        SkinButton("Button_01_OffWhite", "BTN_Quest", tab, "퀘스트", TopRight(-180f, -20f, 150f, 80f), BrownDark);
        SkinButton("Button_01_OffWhite", "BTN_Setting", tab, "설정", TopRight(-20f, -20f, 150f, 80f), BrownDark);

        Label("TXT_StageLabel", tab, "STAGE 1", 30, "#7A4A22", true, TextAlignmentOptions.Left, false, TopLeft(40f, -20f, 400f, 44f));
        Label("TXT_StageName", tab, "말랑 숲", 64, BrownDark, true, TextAlignmentOptions.Left, false, TopLeft(40f, -62f, 500f, 90f));

        ButtonBlock("BTN_DevGame1", tab, "#D9CBA8", "DEV 게임1", 24, BrownDark, false, BottomLeft(30f, 190f, 300f, 70f));
        ButtonBlock("BTN_DevTest", tab, "#D9CBA8", "DEV 테스트", 24, BrownDark, false, BottomLeft(350f, 190f, 300f, 70f));

        var start = SkinButton("Button_03_Red", "BTN_Start", tab, "전투 시작", null);
        start.anchorMin = new Vector2(0f, 0f);
        start.anchorMax = new Vector2(1f, 0f);
        start.pivot = new Vector2(0.5f, 0f);
        start.offsetMin = new Vector2(30f, 20f);
        start.offsetMax = new Vector2(-30f, 170f);

        var startLabel = start.GetComponentInChildren<TMP_Text>(true);
        if (startLabel != null)
            startLabel.fontSizeMax = 60f;

        return tab;
    }

    // 10~50웨이브를 10단위 표식으로 깔아 둔 세로 스크롤 맵. 맨 위에 웨이브 완료 표시가 나온다.
    private static void BuildBattleMap(RectTransform tab)
    {
        const string lineHex = "#FFF6DC";
        const string numberHex = "#7A5A35";

        var scrollRoot = NewRect("MapScroll", tab);
        Stretch(scrollRoot);
        var scrollImage = AddImage(scrollRoot, White);
        scrollImage.color = new Color(1f, 1f, 1f, 0f);

        var viewport = NewRect("Viewport", scrollRoot);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        float waveTopY = MapBaseOffset + MapPixelsPerWave * MapMaxWave;
        float contentHeight = waveTopY + MapTopMargin;
        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 0f);
        content.pivot = new Vector2(0.5f, 0f);
        content.sizeDelta = new Vector2(0f, contentHeight);
        content.anchoredPosition = Vector2.zero;

        BuildMapGround(content, contentHeight);

        for (int i = 1; i <= MapMaxWave / 10; i++)
        {
            float y = MapBaseOffset + MapPixelsPerWave * i * 10f;
            var number = Label("TXT_Wave_" + i * 10, content, (i * 10).ToString(), 170, numberHex, true, TextAlignmentOptions.Center, false,
                Anchored(new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(300f, 200f), new Vector2(0.5f, 0.5f)));
            number.alpha = 0.4f;
            number.raycastTarget = false;

            foreach (var side in new[] { -1f, 1f })
            {
                var line = Block("IMG_WaveLine", content, lineHex, Anchored(new Vector2(0.5f, 0f), new Vector2(side * 360f, y), new Vector2(340f, 8f), new Vector2(0.5f, 0.5f)));
                line.GetComponent<Image>().raycastTarget = false;
                line.GetComponent<Image>().color = new Color(1f, 0.96f, 0.86f, 0.6f);
            }
        }

        var marker = NewRect("PlayerMarker", content);
        Apply(marker, Anchored(new Vector2(0.5f, 0f), new Vector2(0f, MapBaseOffset), new Vector2(360f, 330f), new Vector2(0.5f, 0f)));
        var character = Block("IMG_Player", marker, White, Anchored(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(150f, 200f), new Vector2(0.5f, 0f)));
        character.GetComponent<Image>().raycastTarget = false;
        SetSprite(character.GetComponent<Image>(), "Sample_Cha02_s", "Demo_Character");
        var tag = FrameBlock("IMG_BestTag", marker, Cream, "#C9B48C", Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(340f, 70f), new Vector2(0.5f, 0f)));
        var bestText = Label("TXT_BestWave", tag, "도전 시작!", 34, BrownDark, true, TextAlignmentOptions.Center, true);

        var complete = NewRect("WaveComplete", content);
        Apply(complete, Anchored(new Vector2(0.5f, 0f), new Vector2(0f, waveTopY + 330f), new Vector2(620f, 420f), new Vector2(0.5f, 0.5f)));
        var completeGroup = complete.gameObject.AddComponent<CanvasGroup>();
        completeGroup.blocksRaycasts = false;

        var flag = Block("ICON_WaveCompleteFlag", complete, White, Anchored(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(240f, 240f), new Vector2(0.5f, 1f)));
        flag.GetComponent<Image>().raycastTarget = false;
        SetSprite(flag.GetComponent<Image>(), "Item_Flag_01_Finish", "UniqueIcon/256");

        var banner = FrameBlock("FRAME_WaveComplete", complete, Cream, "#C9B48C", Anchored(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(560f, 120f), new Vector2(0.5f, 0f)));
        Label("TXT_WaveComplete", banner, "웨이브 완료", 56, BrownDark, true, TextAlignmentOptions.Center, true);

        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        var map = scrollRoot.gameObject.AddComponent<BattleMap>();
        var so = new SerializedObject(map);
        so.FindProperty("_scroll").objectReferenceValue = scroll;
        so.FindProperty("_content").objectReferenceValue = content;
        so.FindProperty("_playerMarker").objectReferenceValue = marker;
        so.FindProperty("_bestWaveText").objectReferenceValue = bestText;
        so.FindProperty("_completeGroup").objectReferenceValue = completeGroup;
        so.FindProperty("_baseOffset").floatValue = MapBaseOffset;
        so.FindProperty("_pixelsPerWave").floatValue = MapPixelsPerWave;
        so.FindProperty("_maxWave").intValue = MapMaxWave;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 흙길 → 풀밭 → 소품 순으로 쌓아 풀밭이 흙길 위에 그려지게 한다 (풀밭 안쪽 외곽선이 흙길 경계선이 된다).
    private static void BuildMapGround(RectTransform content, float contentHeight)
    {
        var path = LoadWaveMapSprite("Path_Dirt");
        var grassLeft = LoadWaveMapSprite("Bg_Grass_Left");
        var grassRight = LoadWaveMapSprite("Bg_Grass_Right");
        if (path == null || grassLeft == null || grassRight == null)
            return;

        var pathLayer = NewRect("Layer_Path", content);
        Stretch(pathLayer);
        var grassLayer = NewRect("Layer_Grass", content);
        Stretch(grassLayer);
        var propLayer = NewRect("Layer_Props", content);
        Stretch(propLayer);

        float tileHeight = path.rect.height * MapSpriteScale;
        int tileCount = Mathf.CeilToInt(contentHeight / tileHeight);

        // 홀수 번째 타일은 위아래를 뒤집어, 맞닿는 가장자리가 같아져 이음새가 보이지 않게 한다.
        for (int i = 0; i < tileCount; i++)
        {
            bool flipped = i % 2 == 1;
            float edgeY = i * tileHeight;
            float pivotY = flipped ? 1f : 0f;

            MapSprite("IMG_Path_" + i, pathLayer, path, new Vector2(0f, edgeY), new Vector2(0.5f, pivotY), MapSpriteScale, flipped);
            MapSprite("IMG_GrassLeft_" + i, grassLayer, grassLeft, new Vector2(-MapGrassEdge, edgeY), new Vector2(1f, pivotY), MapSpriteScale, flipped);
            MapSprite("IMG_GrassRight_" + i, grassLayer, grassRight, new Vector2(MapGrassEdge, edgeY), new Vector2(0f, pivotY), MapSpriteScale, flipped);
        }

        BuildMapProps(propLayer, contentHeight);
    }

    // 시드를 고정해 다시 빌드해도 같은 배치가 나온다. 웨이브 구분선 근처는 비워 둔다.
    private static void BuildMapProps(RectTransform layer, float contentHeight)
    {
        string[] names = { "Prop_Tree", "Prop_Rock", "Prop_RockSmall", "Prop_Grass", "Prop_Flower" };
        float[] weights = { 0.14f, 0.2f, 0.2f, 0.26f, 0.2f };

        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            sprites[i] = LoadWaveMapSprite(names[i]);
            if (sprites[i] == null)
                return;
        }

        var rng = new System.Random(MapPropSeed);
        int count = 0;
        var heavyYs = new System.Collections.Generic.List<float>();

        foreach (float side in new[] { -1f, 1f })
        {
            float y = 60f;
            int last = -1;
            while (y < contentHeight - 80f)
            {
                if (!NearWaveLine(y))
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

                    if (index == last)
                        index = index == PropIndexGrass ? PropIndexFlower : PropIndexGrass;

                    last = index;
                    if (index <= PropIndexRockSmall)
                        heavyYs.Add(y);

                    float x = side * Mathf.Lerp(310f, 440f, (float)rng.NextDouble());
                    float scale = Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble()) * 1.2f;
                    bool flipX = rng.Next(2) == 0;
                    MapSprite("IMG_" + names[index] + "_" + count++, layer, sprites[index], new Vector2(x, y), new Vector2(0.5f, 0f), scale, false, flipX);
                }

                y += Mathf.Lerp(130f, 280f, (float)rng.NextDouble());
            }
        }
    }

    private static bool IsNearHeavy(System.Collections.Generic.List<float> heavyYs, float y)
    {
        for (int i = 0; i < heavyYs.Count; i++)
        {
            if (Mathf.Abs(y - heavyYs[i]) < MapHeavyPropSpacing)
                return true;
        }

        return false;
    }

    private static bool NearWaveLine(float y)
    {
        for (int i = 1; i <= MapMaxWave / 10; i++)
        {
            if (Mathf.Abs(y - (MapBaseOffset + MapPixelsPerWave * i * 10f)) < 90f)
                return true;
        }

        return false;
    }

    private static RectTransform BuildHunterTab(RectTransform parent, HunterCard cardPrefab)
    {
        var tab = NewRect("HunterTab", parent);
        Stretch(tab);

        var showcase = Block("IMG_HunterShowcase", tab, BeigeBand, null);
        showcase.anchorMin = new Vector2(0f, 1f);
        showcase.anchorMax = new Vector2(1f, 1f);
        showcase.pivot = new Vector2(0.5f, 1f);
        showcase.sizeDelta = new Vector2(0f, 380f);
        showcase.anchoredPosition = Vector2.zero;
        Label("TXT_ShowcaseHint", showcase, "[대표 헌터 쇼케이스]", 30, BrownDark, false, TextAlignmentOptions.Center, true);
        var pet = FrameBlock("IMG_PetSlot", showcase, Cream, "#C9B48C", Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(520f, 80f), new Vector2(0.5f, 0f)));
        Label("TXT_PetHint", pet, "[펫 영역 (예정)]", 24, "#8A7A5C", false, TextAlignmentOptions.Center, true);

        var info = NewRect("InfoRow", tab);
        info.anchorMin = new Vector2(0f, 1f);
        info.anchorMax = new Vector2(1f, 1f);
        info.pivot = new Vector2(0.5f, 1f);
        info.sizeDelta = new Vector2(0f, 90f);
        info.anchoredPosition = new Vector2(0f, -400f);

        ResourceChip("Chip_Cards", info, "ResourceBar_Icon_Ticket", "Cards", TopLeft(30f, 0f, 300f, 80f), out var cardText);

        var sortButton = (SortButton)PrefabUtility.InstantiatePrefab(BuildSortButtonPrefab(), info);
        sortButton.name = "SortButton";
        Apply((RectTransform)sortButton.transform, TopRight(-30f, 0f, 220f, 80f));

        var area = NewRect("IMG_GridArea", tab);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(20f, 10f);
        area.offsetMax = new Vector2(-20f, -500f);
        var areaImage = AddImage(area, White);
        areaImage.color = new Color(1f, 1f, 1f, 0f);

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
        so.FindProperty("_sortButton").objectReferenceValue = sortButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        return tab;
    }

    // 어두운 트랙 위에 밝은 파란 채움으로 칠한다. 레벨업 가능 상태(Upgrade 오브젝트)는 에셋의 초록을 그대로 쓴다.
    private static void StyleGauge(RectTransform slider)
    {
        var orange = FindRecursive(slider, "Slider_02_Orange");
        if (orange == null)
            return;

        var track = FindRecursive(orange, "Bg");
        if (track != null && track.GetComponent<Image>() != null)
            track.GetComponent<Image>().color = ParseColor("#2B1F24");

        var fill = FindRecursive(orange, "Fill");
        if (fill != null && fill.GetComponent<Image>() != null)
            fill.GetComponent<Image>().color = ParseColor("#16EF89");
    }

    private static SortButton BuildSortButtonPrefab()
    {
        const string sortPath = PrefabFolder + "/SortButton.prefab";

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        var root = NewRect("SortButton", null);
        root.sizeDelta = new Vector2(220f, 80f);

        var image = AddImage(root, White);
        SetSprite(image, "Dropdown_01_Bg", "Theme_Light");
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var arrow = Block("ICON_Arrow", root, White, Anchored(new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(36f, 28f)));
        arrow.GetComponent<Image>().raycastTarget = false;
        SetSprite(arrow.GetComponent<Image>(), "Dropdown_01_Icon_Arrow", "Theme_Light", ParseColor(BrownDark));

        var label = Label("TXT_Label", root, "레벨순", 36, BrownDark, true, TextAlignmentOptions.Center, true);
        SetOffsets(label.rectTransform, new Vector2(76f, 0f), new Vector2(-12f, 0f));
        label.enableAutoSizing = true;
        label.fontSizeMax = 36f;
        label.fontSizeMin = 20f;

        var sortButton = root.gameObject.AddComponent<SortButton>();
        var so = new SerializedObject(sortButton);
        so.FindProperty("_button").objectReferenceValue = button;
        so.FindProperty("_label").objectReferenceValue = label;
        so.FindProperty("_arrow").objectReferenceValue = arrow;
        so.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, sortPath);
        Object.DestroyImmediate(root.gameObject);
        return saved.GetComponent<SortButton>();
    }

    // 등급 인덱스는 gradeId - 1: 일반(갈색) 희귀(파랑) 영웅(보라) 전설(주황)
    private static readonly string[] GradeFrames =
    {
        "CardFrame_02_Brown", "CardFrame_02_Blue", "CardFrame_02_Plum", "CardFrame_02_Yellow",
    };

    // 에셋 변형 프레임의 대표색(렌더 실측)과 목표 등급색. 층별 음영은 비율 곱으로 유지한다.
    private static readonly string[] GradeFrameBaseColors = { "#93562A", "#389AC9", "#A338D5", "#FAA41C" };
    private static readonly string[] GradeFrameColors = { "#A87952", "#2588E8", "#9B58D6", "#E7A52E" };

    private static void TintFrame(RectTransform frame, string fromHex, string toHex)
    {
        var from = ParseColor(fromHex);
        var to = ParseColor(toHex);

        foreach (var image in frame.GetComponentsInChildren<Image>(true))
        {
            var c = image.color;
            if (c.r > 0.92f && c.g > 0.92f && c.b > 0.92f)
                continue;

            image.color = new Color(
                Mathf.Clamp01(c.r * to.r / from.r),
                Mathf.Clamp01(c.g * to.g / from.g),
                Mathf.Clamp01(c.b * to.b / from.b),
                c.a);
        }
    }

    private static HunterCard BuildHunterCardPrefab()
    {
        const string cardPath = PrefabFolder + "/HunterCard.prefab";

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        var root = NewRect("HunterCard", null);
        root.sizeDelta = new Vector2(230f, 290f);

        var hit = AddImage(root, White);
        hit.color = new Color(1f, 1f, 1f, 0f);
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;

        var area = NewRect("CardArea", root);
        Apply(area, TopCenter(0f, 0f, 230f, 240f));

        var frames = new GameObject[GradeFrames.Length];
        for (int i = 0; i < GradeFrames.Length; i++)
        {
            var frame = Skin(GradeFrames[i], "FRAME_Grade" + (i + 1), area, null);
            TintFrame(frame, GradeFrameBaseColors[i], GradeFrameColors[i]);
            frame.gameObject.SetActive(i == 0);
            frames[i] = frame.gameObject;
        }

        var lockedFrame = Skin("CardFrame_02_Disable", "FRAME_Locked", area, null);
        lockedFrame.gameObject.SetActive(false);

        var level = Label("TXT_Level", area, "Lv.1", 34, White, true, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(200f, 44f), new Vector2(0.5f, 0f)));

        var badge = Block("ICON_UpgradeBadge", area, White, Anchored(new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(46f, 46f), new Vector2(1f, 1f)));
        badge.GetComponent<Image>().raycastTarget = false;
        SetSprite(badge.GetComponent<Image>(), "Icon_Arrow_Up_02", "Theme_Light");

        var slider = Skin("Slider_Upgrade_01", "Slider", area, Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(200f, 38f), new Vector2(0.5f, 0f)));
        StyleGauge(slider);
        var orange = FindRecursive(slider, "Slider_02_Orange");
        if (orange != null)
        {
            var innerText = orange.GetComponentInChildren<TMP_Text>(true);
            if (innerText != null)
                innerText.gameObject.SetActive(false);
        }

        var fill = FindRecursive(slider, "Fill") as RectTransform;
        var upgradeBar = FindRecursive(slider, "Upgrade");
        var lockedBar = FindRecursive(slider, "Lock");
        var upgradeIcon = FindRecursive(slider, "Icon_Upgrade");
        if (upgradeIcon != null)
            upgradeIcon.gameObject.SetActive(false);

        TMP_Text progressText = null;
        foreach (Transform child in slider)
        {
            if (child.name == "Text (TMP)")
                progressText = child.GetComponent<TMP_Text>();
        }

        if (upgradeBar != null) upgradeBar.gameObject.SetActive(false);
        if (lockedBar != null) lockedBar.gameObject.SetActive(false);

        var name = Label("TXT_Name", root, "이름", 28, BrownDark, true, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(230f, 44f), new Vector2(0.5f, 0f)));

        var card = root.gameObject.AddComponent<HunterCard>();
        var so = new SerializedObject(card);
        var frameList = so.FindProperty("_gradeFrames");
        frameList.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            frameList.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];

        so.FindProperty("_lockedFrame").objectReferenceValue = lockedFrame.gameObject;
        so.FindProperty("_nameText").objectReferenceValue = name;
        so.FindProperty("_levelText").objectReferenceValue = level;
        so.FindProperty("_progressText").objectReferenceValue = progressText;
        so.FindProperty("_progressFill").objectReferenceValue = fill;
        so.FindProperty("_lockedBar").objectReferenceValue = lockedBar != null ? lockedBar.gameObject : null;
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
        bar.sizeDelta = new Vector2(0f, TabBarHeight);
        bar.anchoredPosition = Vector2.zero;

        string[] names = { "상점", "헌터", "전투", "컨텐츠1", "컨텐츠2" };
        string[] keys = { "Shop", "Hunter", "Battle", "Content1", "Content2" };
        bool[] locked = { true, false, false, true, true };
        GameObject[] panels = { null, hunterPanel, battlePanel, null, null };
        (string Name, string Hint)[] iconSprites =
        {
            ("UI_Shop_01_Red", "UniqueIcon/128"),
            ("Gear_Helmet_04", "UniqueIcon/128"),
            ("UI_Play_Battle_01_Color", "UniqueIcon/128"),
            ("UI_Common_Lock_01_Silver", "UniqueIcon/128"),
            ("UI_Common_Lock_01_Silver", "UniqueIcon/128"),
        };

        var tabs = bar.gameObject.AddComponent<LobbyTabs>();
        var so = new SerializedObject(tabs);
        var list = so.FindProperty("_tabs");
        list.arraySize = names.Length;

        for (int i = 0; i < names.Length; i++)
        {
            var button = Skin("Tab_01", "Tab_" + keys[i], bar, null);
            button.anchorMin = new Vector2(i / 5f, 0f);
            button.anchorMax = new Vector2((i + 1) / 5f, 1f);
            button.offsetMin = new Vector2(4f, 4f);
            button.offsetMax = new Vector2(-4f, -4f);

            var btn = EnsureButton(button);
            btn.transition = Selectable.Transition.None;

            var label = SetLabel(button, names[i]);
            var normal = FindRecursive(button, "Normal");
            var focus = FindRecursive(button, "Focus");

            foreach (var iconName in new[] { "Icon", "Icon_Focus" })
            {
                var iconTransform = FindRecursive(button, iconName);
                if (iconTransform == null)
                    continue;

                var iconImage = iconTransform.GetComponent<Image>();
                if (iconImage == null)
                    continue;

                if (iconSprites[i].Hint != null)
                    SetSprite(iconImage, iconSprites[i].Name, iconSprites[i].Hint);

                if (locked[i])
                    iconImage.color = new Color(1f, 1f, 1f, 0.5f);
            }

            var element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_button").objectReferenceValue = btn;
            element.FindPropertyRelative("_panel").objectReferenceValue = panels[i];
            element.FindPropertyRelative("_background").objectReferenceValue = button.GetComponentInChildren<Image>(true);
            element.FindPropertyRelative("_label").objectReferenceValue = label;
            element.FindPropertyRelative("_locked").boolValue = locked[i];
            element.FindPropertyRelative("_normalState").objectReferenceValue = normal != null ? normal.gameObject : null;
            element.FindPropertyRelative("_focusState").objectReferenceValue = focus != null ? focus.gameObject : null;
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
        WirePopupRoot(scene, EnsurePopupRoot(scene));

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
        var devGame1 = lobbyMain.Find("Content/BattleTab/BTN_DevGame1")?.GetComponent<Button>();

        if (component != null)
        {
            var so = new SerializedObject(component);
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.FindProperty("_testStartButton").objectReferenceValue = devTest;
            so.FindProperty("_game1StartButton").objectReferenceValue = devGame1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (Transform child in lobbyStart)
            child.gameObject.SetActive(false);
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
