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

        var cardsChip = BuildChip(chips, "Chip_Cards", "ICON_Card", "#E58A4A", out var cardsChipText);
        var goldChip = BuildChip(chips, "Chip_Gold", "ICON_Gold", "#E0B54A", out var goldChipText);
        var gemChip = BuildChip(chips, "Chip_Gem", "ICON_Gem", "#6E8FD6", out var gemChipText);

        var group = NewRect("Group", root);
        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.sizeDelta = new Vector2(900f, 1670f);
        group.anchoredPosition = Vector2.zero;

        var panel = Block("FRAME_Panel", group, "#5E665F", TopCenter(0f, 0f, 900f, 1500f));
        var fill = Block("IMG_PanelFill", panel, "#2F3530", null);
        SetOffsets(fill, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        Block("IMG_HeroShadow", fill, "#222723", TopCenter(0f, -395f, 380f, 56f));
        var hero = Block("IMG_Hero", fill, "#8C968C", TopCenter(0f, -30f, 270f, 370f));
        Label("TXT_HeroHint", hero, "[헌터 이미지]", 28, "#C3CBC2", false, TextAlignmentOptions.Center, true);
        var heroBadge = Block("FRAME_GradeBadge", hero, "#BFB7A6", Anchored(new Vector2(0f, 1f), new Vector2(-20f, -12f), new Vector2(56f, 56f), new Vector2(0f, 1f)));
        var heroBadgeText = Label("TXT_GradeBadge", heroBadge, "1", 30, TextDark, true, TextAlignmentOptions.Center, true);

        var nameText = Label("TXT_Name", fill, "궁수", 104, White, true, TextAlignmentOptions.Center, false, TopCenter(0f, -430f, 860f, 120f));

        var gradeTag = Block("IMG_GradeTag", fill, "#BFB7A6", TopCenter(-96f, -562f, 170f, 52f));
        var gradeTagText = Label("TXT_GradeTag", gradeTag, "일반", 28, TextDark, true, TextAlignmentOptions.Center, true);
        var typeTag = Block("IMG_AttackTypeTag", fill, "#454D46", TopCenter(96f, -562f, 170f, 52f));
        var typeTagText = Label("TXT_AttackTypeTag", typeTag, "원거리", 28, White, true, TextAlignmentOptions.Center, true);

        var levelRow = NewRect("LevelRow", fill);
        Apply(levelRow, TopCenter(0f, -650f, 760f, 66f));
        var levelText = Label("TXT_Level", levelRow, "Lv.1/15", 40, White, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(200f, 66f), new Vector2(0f, 0.5f)));
        var bar = Block("IMG_ProgressBg", levelRow, "#222723", null);
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        bar.offsetMin = new Vector2(220f, -25f);
        bar.offsetMax = new Vector2(-76f, 25f);
        var barFill = Block("IMG_ProgressFill", bar, "#3FA85A", null);
        var progressText = Label("TXT_Progress", bar, "35/10", 28, White, true, TextAlignmentOptions.Center, true);
        var arrow = Block("ICON_ArrowBadge", levelRow, "#3FA85A", Anchored(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(56f, 56f), new Vector2(1f, 0.5f)));
        Label("TXT_Arrow", arrow, "▲", 30, White, true, TextAlignmentOptions.Center, true);

        var lockedRow = NewRect("LockedRow", fill);
        Apply(lockedRow, TopCenter(0f, -650f, 760f, 66f));
        Block("IMG_LockedLineLeft", lockedRow, "#5E665F", Anchored(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(240f, 2f), new Vector2(0f, 0.5f)));
        Block("IMG_LockedLineRight", lockedRow, "#5E665F", Anchored(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(240f, 2f), new Vector2(1f, 0.5f)));
        Label("TXT_Locked", lockedRow, "[잠금 아이콘] 잠김", 38, "#C3CBC2", true, TextAlignmentOptions.Center, true);
        lockedRow.gameObject.SetActive(false);

        var attackTile = Block("FRAME_AttackTile", fill, "#5E665F", TopCenter(-194f, -745f, 368f, 150f));
        var attackInner = Block("IMG_AttackTileFill", attackTile, "#222723", null);
        SetOffsets(attackInner, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Block("ICON_Attack", attackInner, "#3A423B", Anchored(new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(80f, 80f), new Vector2(0f, 0.5f)));
        Label("TXT_AttackLabel", attackInner, "공격력", 26, "#C3CBC2", false, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -16f), new Vector2(220f, 40f), new Vector2(0f, 1f)));
        var attackValue = Label("TXT_AttackValue", attackInner, "100", 64, White, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -56f), new Vector2(230f, 80f), new Vector2(0f, 1f)));
        var bonusBadge = Block("ICON_AttackBonus", attackInner, "#3FA85A", Anchored(new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(96f, 44f), new Vector2(1f, 1f)));
        var bonusText = Label("TXT_AttackBonus", bonusBadge, "+5", 28, White, true, TextAlignmentOptions.Center, true);

        var speedTile = Block("FRAME_SpeedTile", fill, "#5E665F", TopCenter(194f, -745f, 368f, 150f));
        var speedInner = Block("IMG_SpeedTileFill", speedTile, "#222723", null);
        SetOffsets(speedInner, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Block("ICON_Speed", speedInner, "#3A423B", Anchored(new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(80f, 80f), new Vector2(0f, 0.5f)));
        Label("TXT_SpeedLabel", speedInner, "공격 속도", 26, "#C3CBC2", false, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -16f), new Vector2(220f, 40f), new Vector2(0f, 1f)));
        var speedValue = Label("TXT_SpeedValue", speedInner, "1.0", 64, White, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0f, 1f), new Vector2(124f, -56f), new Vector2(230f, 80f), new Vector2(0f, 1f)));

        Label("TXT_SkillHeader", fill, "기본 능력 · 스킬", 30, "#C3CBC2", true, TextAlignmentOptions.Center, false, TopCenter(0f, -930f, 860f, 45f));
        for (int i = 0; i < 3; i++)
        {
            var slot = Block("IMG_SkillSlot_" + (i + 1), fill, "#3A423B", TopCenter((i - 1) * 200f, -990f, 160f, 176f));
            Label("TXT_SkillHint", slot, "[스킬]", 24, "#C3CBC2", false, TextAlignmentOptions.Center, true);
        }

        Label("TXT_SkillNote", fill, "스킬 준비 중", 26, "#9FA89F", false, TextAlignmentOptions.Center, false, TopCenter(0f, -1180f, 860f, 40f));

        var action = Block("BTN_Action", fill, "#E8BE4A", TopCenter(0f, -1280f, 760f, 170f));
        var actionImage = action.GetComponent<Image>();
        var actionButton = action.gameObject.AddComponent<Button>();
        actionButton.targetGraphic = actionImage;
        var actionLabel = Label("TXT_ActionLabel", action, "레벨업", 60, TextDark, true, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(700f, 80f), new Vector2(0.5f, 1f)));
        var costIcon = Block("ICON_ActionCost", action, TextDark, Anchored(new Vector2(0.5f, 0f), new Vector2(-70f, 24f), new Vector2(36f, 36f), new Vector2(0.5f, 0f)));
        var costText = Label("TXT_ActionCost", action, "100", 44, TextDark, true, TextAlignmentOptions.Left, false,
            Anchored(new Vector2(0.5f, 0f), new Vector2(-20f, 16f), new Vector2(240f, 56f), new Vector2(0f, 0f)));

        var lockedNotice = Label("TXT_LockedNotice", fill, "잠김 · 획득 방법 미정", 40, "#C3CBC2", true, TextAlignmentOptions.Center, false, TopCenter(0f, -1320f, 760f, 90f));
        lockedNotice.gameObject.SetActive(false);

        var closeFrame = Block("BTN_Close", group, "#E8E2E2", Anchored(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(130f, 130f), new Vector2(0.5f, 0f)));
        var closeInner = Block("IMG_CloseFill", closeFrame, "#B23A3A", null);
        SetOffsets(closeInner, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Label("TXT_Close", closeInner, "X", 80, White, true, TextAlignmentOptions.Center, true);
        var closeButton = closeFrame.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeFrame.GetComponent<Image>();

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
        so.FindProperty("_gradeTagText").objectReferenceValue = gradeTagText;
        so.FindProperty("_attackTypeTagText").objectReferenceValue = typeTagText;
        so.FindProperty("_levelRow").objectReferenceValue = levelRow.gameObject;
        so.FindProperty("_levelText").objectReferenceValue = levelText;
        so.FindProperty("_progressFill").objectReferenceValue = barFill;
        so.FindProperty("_progressText").objectReferenceValue = progressText;
        so.FindProperty("_arrowBadge").objectReferenceValue = arrow.gameObject;
        so.FindProperty("_lockedRow").objectReferenceValue = lockedRow.gameObject;
        so.FindProperty("_attackTileBorder").objectReferenceValue = attackTile.GetComponent<Image>();
        so.FindProperty("_attackValueText").objectReferenceValue = attackValue;
        so.FindProperty("_attackBonusBadge").objectReferenceValue = bonusBadge.gameObject;
        so.FindProperty("_attackBonusText").objectReferenceValue = bonusText;
        so.FindProperty("_speedValueText").objectReferenceValue = speedValue;
        so.FindProperty("_actionButton").objectReferenceValue = actionButton;
        so.FindProperty("_actionBackground").objectReferenceValue = actionImage;
        so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
        so.FindProperty("_actionCostText").objectReferenceValue = costText;
        so.FindProperty("_actionCostIcon").objectReferenceValue = costIcon.GetComponent<Image>();
        so.FindProperty("_lockedNoticeText").objectReferenceValue = lockedNotice;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, popupPath);
        Object.DestroyImmediate(root.gameObject);
    }

    private static RectTransform BuildChip(Transform parent, string name, string iconName, string iconHex, out TMP_Text text)
    {
        var chip = Block(name, parent, White, null);
        var element = chip.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 220f;
        element.preferredHeight = 80f;

        Block(iconName, chip, iconHex, Anchored(new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(40f, 40f), new Vector2(0f, 0.5f)));
        text = Label("TXT_Value", chip, "0", 40, TextDark, true, TextAlignmentOptions.Right, false);
        SetOffsets(text.rectTransform, new Vector2(70f, 0f), new Vector2(-18f, 0f));
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

        var stamina = Block("Chip_Stamina", header, White, TopLeft(380f, -40f, 200f, 70f));
        SetSprite(stamina.GetComponent<Image>(), "ResourceBar_Bg", "Theme_Light", new Color(1f, 1f, 1f, 0.45f));
        Label("TXT_Stamina", stamina, "[스태미나 자리]", 22, BrownDark, false, TextAlignmentOptions.Center, true);

        ResourceChip("Chip_Gold", header, "ResourceBar_Icon_Gold", "Gold", TopLeft(595f, -40f, 210f, 70f), out var goldText);
        ResourceChip("Chip_Gem", header, "ResourceBar_Icon_Gem", "Gem", TopLeft(820f, -40f, 210f, 70f), out var gemText);

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

        SkinButton("Button_01_OffWhite", "BTN_Mail", tab, "우편", TopRight(-340f, -20f, 150f, 80f), BrownDark);
        SkinButton("Button_01_OffWhite", "BTN_Quest", tab, "퀘스트", TopRight(-180f, -20f, 150f, 80f), BrownDark);
        SkinButton("Button_01_OffWhite", "BTN_Setting", tab, "설정", TopRight(-20f, -20f, 150f, 80f), BrownDark);

        Label("TXT_StageLabel", tab, "STAGE 1", 30, "#7A4A22", true, TextAlignmentOptions.Center, false, TopCenter(0f, -130f, 600f, 44f));
        Label("TXT_StageName", tab, "말랑 숲", 72, BrownDark, true, TextAlignmentOptions.Center, false, TopCenter(0f, -180f, 800f, 100f));

        var boss = FrameBlock("IMG_BossShowcase", tab, Cream, "#C9B48C", TopCenter(0f, -310f, 700f, 520f));
        var island = Block("IMG_StageSample", boss, White, Anchored(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 500f)));
        island.GetComponent<Image>().raycastTarget = false;
        SetSprite(island.GetComponent<Image>(), "Image_Map_Forest", "Demo_Image");

        var prev = Skin("ArrowButton_01_Prev", "BTN_PrevStage", tab, Anchored(new Vector2(0f, 1f), new Vector2(60f, -520f), new Vector2(90f, 90f), new Vector2(0f, 1f)));
        EnsureButton(prev);
        var next = Skin("ArrowButton_01_Next", "BTN_NextStage", tab, Anchored(new Vector2(1f, 1f), new Vector2(-60f, -520f), new Vector2(90f, 90f), new Vector2(1f, 1f)));
        EnsureButton(next);

        var dots = NewRect("PageDots", tab);
        Apply(dots, TopCenter(0f, -842f, 120f, 20f));
        for (int i = 0; i < 3; i++)
        {
            var dot = Block("IMG_Dot_" + (i + 1), dots, White, Anchored(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 40f, 0f), new Vector2(20f, 20f)));
            SetSprite(dot.GetComponent<Image>(), i == 0 ? "Status_Dot_01_On" : "Status_Dot_01_Off", "Theme_Light");
        }

        var record = Skin("BasicFrame_Rectangle_01_Border_Ivory", "IMG_BestRecord", tab, TopCenter(0f, -880f, 960f, 90f));
        var recordLabel = Label("TXT_RecordLabel", record, "최고 기록", 30, "#7A4A22", true, TextAlignmentOptions.Left, false);
        SetOffsets(recordLabel.rectTransform, new Vector2(30f, 0f), new Vector2(-30f, 0f));
        var recordText = Label("TXT_RecordValue", record, "WAVE 0 / 50", 36, BrownDark, true, TextAlignmentOptions.Right, false);
        SetOffsets(recordText.rectTransform, new Vector2(30f, 0f), new Vector2(-30f, 0f));

        ButtonBlock("BTN_DevGame1", tab, "#D9CBA8", "DEV 게임1", 24, BrownDark, false, BottomLeft(30f, 190f, 300f, 70f));
        ButtonBlock("BTN_DevTest", tab, "#D9CBA8", "DEV 테스트", 24, BrownDark, false, BottomLeft(350f, 190f, 300f, 70f));

        var start = SkinButton("Button_03_Red", "BTN_Start", tab, "전투 시작", null);
        start.anchorMin = new Vector2(0f, 0f);
        start.anchorMax = new Vector2(1f, 0f);
        start.pivot = new Vector2(0.5f, 0f);
        start.offsetMin = new Vector2(30f, 30f);
        start.offsetMax = new Vector2(-30f, 160f);

        var startLabel = start.GetComponentInChildren<TMP_Text>(true);
        if (startLabel != null)
        {
            startLabel.fontSizeMax = 60f;
            startLabel.rectTransform.anchoredPosition += new Vector2(0f, 22f);
        }

        Label("TXT_StaminaCost", start, "스태미나 5 소모", 28, White, false, TextAlignmentOptions.Center, false,
            Anchored(new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(500f, 40f), new Vector2(0.5f, 0f)));

        return tab;
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

        // 드롭다운은 Selectable이라 Button을 같이 못 붙인다. 열리지 않게 끄고 클릭은 덮개 버튼으로 받는다.
        var sort = Skin("Dropdown_01_Arrow", "Dropdown_Sort", info, TopRight(-30f, 0f, 220f, 80f));
        foreach (var selectable in sort.GetComponents<Selectable>())
            selectable.enabled = false;

        var template = FindRecursive(sort, "Template");
        if (template != null)
            template.gameObject.SetActive(false);

        var sortHit = Block("BTN_Sort", sort, White, null);
        sortHit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        var sortButton = sortHit.gameObject.AddComponent<Button>();
        sortButton.targetGraphic = sortHit.GetComponent<Image>();
        TMP_Text sortLabel = null;
        foreach (var tmp in sort.GetComponentsInChildren<TMP_Text>(true))
        {
            if (sortLabel == null || tmp.name == "Label")
                sortLabel = tmp;
        }

        if (sortLabel != null)
            sortLabel.text = "등급순";

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
        so.FindProperty("_sortLabel").objectReferenceValue = sortLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        return tab;
    }

    // 등급 인덱스는 gradeId - 1: 일반(갈색) 희귀(파랑) 영웅(보라) 전설(주황)
    private static readonly string[] GradeFrames =
    {
        "CardFrame_02_Brown", "CardFrame_02_Blue", "CardFrame_02_Plum", "CardFrame_02_Yellow",
    };

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
        so.FindProperty("_upgradeBar").objectReferenceValue = upgradeBar != null ? upgradeBar.gameObject : null;
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
