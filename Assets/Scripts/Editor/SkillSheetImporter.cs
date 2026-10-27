using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SkillSheetImporter
{
    private const string LogTag = "SkillSheetImporter";
    private const string SheetFolder = "Assets/Datas/Sheets";
    private const string SkillsFile = "Skills.csv";
    private const string CatalogPath = "Assets/Datas/SkillCatalog.asset";
    private const string IconFolder = "Assets/Art/UI/SkillIcon";
    private const string IconPrefix = "SkillIcon_";

    [MenuItem("SlimeTD/Data/Import Skill Sheets")]
    public static void Import()
    {
        if (!SheetReader.TryReadTable($"{SheetFolder}/{SkillsFile}", LogTag, out var skillRows))
            return;

        var towerIds = CollectTowerIds();
        var usedIds = new HashSet<int>();
        var imported = new List<SkillRow>();

        foreach (var row in skillRows)
        {
            if (!SheetReader.TryGetInt(row, "skillId", out int id))
            {
                Debug.Log($"[{LogTag}] skillId가 숫자가 아닌 행을 건너뜁니다.");
                continue;
            }

            if (!usedIds.Add(id))
            {
                Debug.Log($"[{LogTag}] 중복된 스킬 ID입니다: {id}");
                continue;
            }

            if (!SheetReader.TryGetInt(row, "towerId", out int towerId) || !towerIds.Contains(towerId))
            {
                Debug.Log($"[{LogTag}] 스킬 {id}의 타워를 찾을 수 없습니다. towerId: {SheetReader.Get(row, "towerId")}");
                continue;
            }

            if (!TryParseEnum(row, "trigger", out SkillTrigger trigger, id))
                continue;

            if (!TryParseEffect(row, id, out var effect))
                continue;

            imported.Add(new SkillRow(id, towerId, trigger, row, effect));
        }

        ValidateUpgrades(imported);
        UpdateCatalog(imported);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[{LogTag}] 임포트 완료. 스킬 {imported.Count}개");
    }

    // 수치 칸에 %가 붙으면 퍼센트, 없으면 고정값이다.
    private static bool TryParseEffect(Dictionary<string, string> row, int skillId, out EffectRow effect)
    {
        effect = default;

        if (string.IsNullOrEmpty(SheetReader.Get(row, "effectType")))
        {
            Debug.Log($"[{LogTag}] 효과가 없는 스킬을 건너뜁니다: {skillId}");
            return false;
        }

        if (!TryParseEnum(row, "effectType", out SkillEffectType type, skillId))
            return false;

        string text = SheetReader.Get(row, "value").Trim();
        var valueType = text.EndsWith("%") ? SkillValueType.Percent : SkillValueType.Fixed;
        string number = valueType == SkillValueType.Percent ? text.Substring(0, text.Length - 1) : text;

        if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            Debug.Log($"[{LogTag}] 효과 수치(value)를 읽지 못해 건너뜁니다. 스킬: {skillId}, 값: '{text}'");
            return false;
        }

        if (!SkillEffectRules.IsValueTypeAllowed(type, valueType))
        {
            Debug.Log($"[{LogTag}] {type}은(는) {valueType} 단위를 지원하지 않아 건너뜁니다. 스킬: {skillId}, 값: '{text}'");
            return false;
        }

        float duration = SheetReader.TryGetFloat(row, "duration", out float parsed) ? Mathf.Max(0f, parsed) : 0f;
        effect = new EffectRow(type, value, valueType, duration);
        return true;
    }

    // 대체 대상은 같은 타워의 더 낮은 해금 레벨 스킬이어야 한다. 아니면 대체가 의도대로 동작하지 않는다.
    private static void ValidateUpgrades(List<SkillRow> skills)
    {
        var byId = skills.ToDictionary(s => s.Id);

        foreach (var skill in skills)
        {
            if (skill.UpgradeOf == 0)
                continue;

            if (!byId.TryGetValue(skill.UpgradeOf, out var target))
            {
                Debug.Log($"[{LogTag}] upgradeOf 대상 스킬이 시트에 없습니다: {skill.Id} -> {skill.UpgradeOf}");
                continue;
            }

            if (target.TowerId != skill.TowerId)
                Debug.Log($"[{LogTag}] 다른 타워의 스킬을 대체할 수 없습니다: {skill.Id} -> {skill.UpgradeOf}");

            if (target.UnlockLevel >= skill.UnlockLevel)
                Debug.Log($"[{LogTag}] 대체 스킬의 해금 레벨이 대상보다 높아야 합니다: {skill.Id}(Lv.{skill.UnlockLevel}) -> {target.Id}(Lv.{target.UnlockLevel})");
        }
    }

    // 카탈로그가 스킬 데이터를 직접 갖는다. 타워 에셋은 스킬을 알지 못한다.
    private static void UpdateCatalog(List<SkillRow> skills)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SkillCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            Debug.Log($"[{LogTag}] SkillCatalog를 만들었습니다: {CatalogPath}", catalog);
        }

        var ordered = skills
            .OrderBy(s => s.TowerId)
            .ThenBy(s => s.UnlockLevel)
            .ThenBy(s => s.Id)
            .ToList();

        var so = new SerializedObject(catalog);
        var array = so.FindProperty("_skills");
        array.arraySize = ordered.Count;

        for (int i = 0; i < ordered.Count; i++)
            Write(array.GetArrayElementAtIndex(i), ordered[i]);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void Write(SerializedProperty element, SkillRow skill)
    {
        var row = skill.Row;

        element.FindPropertyRelative("_id").intValue = skill.Id;
        element.FindPropertyRelative("_displayName").stringValue = SheetReader.Get(row, "name");
        element.FindPropertyRelative("_description").stringValue = SheetReader.Get(row, "description");
        element.FindPropertyRelative("_icon").objectReferenceValue = LoadIcon(skill.Id);
        element.FindPropertyRelative("_towerId").intValue = skill.TowerId;
        element.FindPropertyRelative("_unlockLevel").intValue = skill.UnlockLevel;
        element.FindPropertyRelative("_upgradeOfId").intValue = skill.UpgradeOf;
        element.FindPropertyRelative("_trigger").enumValueIndex = (int)skill.Trigger;
        element.FindPropertyRelative("_nth").intValue = SheetReader.TryGetInt(row, "nth", out int nth) ? nth : 0;
        element.FindPropertyRelative("_chance").floatValue = SheetReader.TryGetFloat(row, "chance", out float chance) ? Mathf.Clamp(chance, 0f, 100f) : 0f;
        element.FindPropertyRelative("_cooldown").floatValue = SheetReader.TryGetFloat(row, "cooldown", out float cooldown) ? Mathf.Max(0f, cooldown) : 0f;

        if (skill.Trigger == SkillTrigger.EveryNthAttack && element.FindPropertyRelative("_nth").intValue < 2)
            Debug.Log($"[{LogTag}] N번째 공격 스킬인데 nth가 2 미만입니다: {skill.Id}");

        var effects = element.FindPropertyRelative("_effects");
        effects.arraySize = 1;

        var effect = effects.GetArrayElementAtIndex(0);
        effect.FindPropertyRelative("_type").enumValueIndex = (int)skill.Effect.Type;
        effect.FindPropertyRelative("_value").floatValue = skill.Effect.Value;
        effect.FindPropertyRelative("_valueType").enumValueIndex = (int)skill.Effect.ValueType;
        effect.FindPropertyRelative("_duration").floatValue = skill.Effect.Duration;
    }

    // 아이콘은 파일 이름의 스킬 ID로 연결한다. 파일이 없으면 비워 둔다.
    private static Sprite LoadIcon(int skillId)
    {
        string path = $"{IconFolder}/{IconPrefix}{skillId}.png";
        if (!File.Exists(path))
            return null;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool TryParseEnum<T>(Dictionary<string, string> row, string key, out T value, int skillId) where T : struct
    {
        string text = SheetReader.Get(row, key);
        if (Enum.TryParse(text, true, out value) && Enum.IsDefined(typeof(T), value))
            return true;

        Debug.Log($"[{LogTag}] {key} 값을 해석하지 못해 건너뜁니다. 스킬: {skillId}, 값: '{text}', 가능한 값: {string.Join("/", Enum.GetNames(typeof(T)))}");
        return false;
    }

    private static HashSet<int> CollectTowerIds()
    {
        var result = new HashSet<int>();

        foreach (string guid in AssetDatabase.FindAssets("t:TowerData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<TowerData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null)
                result.Add(data.Id);
        }

        return result;
    }

    private readonly struct SkillRow
    {
        public readonly int Id;
        public readonly int TowerId;
        public readonly SkillTrigger Trigger;
        public readonly Dictionary<string, string> Row;
        public readonly EffectRow Effect;

        public int UnlockLevel => SheetReader.TryGetInt(Row, "unlockLevel", out int level) ? Mathf.Max(1, level) : 1;
        public int UpgradeOf => SheetReader.TryGetInt(Row, "upgradeOf", out int upgradeOf) ? upgradeOf : 0;

        public SkillRow(int id, int towerId, SkillTrigger trigger, Dictionary<string, string> row, EffectRow effect)
        {
            Id = id;
            TowerId = towerId;
            Trigger = trigger;
            Row = row;
            Effect = effect;
        }
    }

    private readonly struct EffectRow
    {
        public readonly SkillEffectType Type;
        public readonly float Value;
        public readonly SkillValueType ValueType;
        public readonly float Duration;

        public EffectRow(SkillEffectType type, float value, SkillValueType valueType, float duration)
        {
            Type = type;
            Value = value;
            ValueType = valueType;
            Duration = duration;
        }
    }
}
