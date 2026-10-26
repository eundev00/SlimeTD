using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SkillSheetImporter
{
    private const string LogTag = "SkillSheetImporter";
    private const string SheetFolder = "Assets/Datas/Sheets";
    private const string SkillsFile = "Skills.csv";
    private const string EffectsFile = "SkillEffects.csv";
    private const string SkillFolder = "Assets/Datas/Skills";
    private const string CatalogPath = "Assets/Datas/SkillCatalog.asset";
    private const string TowerDataPrefix = "TowerData_";

    [MenuItem("SlimeTD/Data/Import Skill Sheets")]
    public static void Import()
    {
        if (!SheetReader.TryReadTable($"{SheetFolder}/{SkillsFile}", LogTag, out var skillRows)
            || !SheetReader.TryReadTable($"{SheetFolder}/{EffectsFile}", LogTag, out var effectRows))
            return;

        var towers = CollectTowers();
        var effects = ParseEffects(effectRows);
        var assets = CollectSkillAssets();
        var usedIds = new HashSet<int>();
        var imported = new List<TowerSkillData>();
        int created = 0;

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

            string codeName = SheetReader.Get(row, "codeName");
            if (string.IsNullOrEmpty(codeName))
            {
                Debug.Log($"[{LogTag}] 코드명이 없는 스킬을 건너뜁니다: {id}");
                continue;
            }

            if (!SheetReader.TryGetInt(row, "towerId", out int towerId) || !towers.TryGetValue(towerId, out string towerCode))
            {
                Debug.Log($"[{LogTag}] 스킬 {id}({codeName})의 타워를 찾을 수 없습니다. towerId: {SheetReader.Get(row, "towerId")}");
                continue;
            }

            if (!TryParseEnum(row, "trigger", out SkillTrigger trigger, id))
                continue;

            if (!assets.TryGetValue(id, out var skill))
            {
                skill = CreateAsset(towerCode, codeName);
                if (skill == null)
                    continue;

                created++;
            }

            effects.TryGetValue(id, out var skillEffects);
            if (skillEffects == null || skillEffects.Count == 0)
                Debug.Log($"[{LogTag}] 효과가 없는 스킬입니다: {id}({codeName})", skill);

            Apply(skill, id, towerId, trigger, row, skillEffects ?? new List<EffectRow>());
            imported.Add(skill);
        }

        foreach (int effectSkillId in effects.Keys)
        {
            if (!usedIds.Contains(effectSkillId))
                Debug.Log($"[{LogTag}] 스킬 시트에 없는 스킬의 효과 행입니다: {effectSkillId}");
        }

        foreach (var pair in assets)
        {
            if (!usedIds.Contains(pair.Key))
                Debug.Log($"[{LogTag}] 시트에 없는 스킬 에셋입니다: {AssetDatabase.GetAssetPath(pair.Value)}");
        }

        ValidateUpgrades(imported);
        UpdateCatalog(imported);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[{LogTag}] 임포트 완료. 스킬 {usedIds.Count}개, 신규 {created}개");
    }

    private static void Apply(TowerSkillData skill, int id, int towerId, SkillTrigger trigger, Dictionary<string, string> row, List<EffectRow> effects)
    {
        var so = new SerializedObject(skill);

        so.FindProperty("_id").intValue = id;
        so.FindProperty("_codeName").stringValue = SheetReader.Get(row, "codeName");
        so.FindProperty("_displayName").stringValue = SheetReader.Get(row, "name");
        so.FindProperty("_description").stringValue = SheetReader.Get(row, "description");
        so.FindProperty("_towerId").intValue = towerId;
        so.FindProperty("_unlockLevel").intValue = SheetReader.TryGetInt(row, "unlockLevel", out int level) ? Mathf.Max(1, level) : 1;
        so.FindProperty("_upgradeOfId").intValue = SheetReader.TryGetInt(row, "upgradeOf", out int upgradeOf) ? upgradeOf : 0;
        so.FindProperty("_trigger").enumValueIndex = (int)trigger;
        so.FindProperty("_nth").intValue = SheetReader.TryGetInt(row, "nth", out int nth) ? nth : 0;
        so.FindProperty("_chance").floatValue = SheetReader.TryGetFloat(row, "chance", out float chance) ? Mathf.Clamp(chance, 0f, 100f) : 0f;
        so.FindProperty("_cooldown").floatValue = SheetReader.TryGetFloat(row, "cooldown", out float cooldown) ? Mathf.Max(0f, cooldown) : 0f;

        if (trigger == SkillTrigger.EveryNthAttack && so.FindProperty("_nth").intValue < 2)
            Debug.Log($"[{LogTag}] N번째 공격 스킬인데 nth가 2 미만입니다: {id}", skill);

        var list = so.FindProperty("_effects");
        list.arraySize = effects.Count;

        for (int i = 0; i < effects.Count; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_type").enumValueIndex = (int)effects[i].Type;
            element.FindPropertyRelative("_value").floatValue = effects[i].Value;
            element.FindPropertyRelative("_valueType").enumValueIndex = (int)effects[i].ValueType;
            element.FindPropertyRelative("_duration").floatValue = effects[i].Duration;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
    }

    // 대체 대상은 같은 타워의 더 낮은 해금 레벨 스킬이어야 한다. 아니면 대체가 의도대로 동작하지 않는다.
    private static void ValidateUpgrades(List<TowerSkillData> skills)
    {
        var byId = skills.ToDictionary(s => s.Id);

        foreach (var skill in skills.Where(s => s.IsUpgrade))
        {
            if (!byId.TryGetValue(skill.UpgradeOfId, out var target))
            {
                Debug.Log($"[{LogTag}] upgradeOf 대상 스킬이 시트에 없습니다: {skill.Id} -> {skill.UpgradeOfId}", skill);
                continue;
            }

            if (target.TowerId != skill.TowerId)
                Debug.Log($"[{LogTag}] 다른 타워의 스킬을 대체할 수 없습니다: {skill.Id} -> {skill.UpgradeOfId}", skill);

            if (target.UnlockLevel >= skill.UnlockLevel)
                Debug.Log($"[{LogTag}] 대체 스킬의 해금 레벨이 대상보다 높아야 합니다: {skill.Id}(Lv.{skill.UnlockLevel}) -> {target.Id}(Lv.{target.UnlockLevel})", skill);
        }
    }

    // 카탈로그는 시트에 있는 스킬만 담는다. 타워 에셋은 스킬을 알지 못한다.
    private static void UpdateCatalog(List<TowerSkillData> skills)
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
            array.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static Dictionary<int, List<EffectRow>> ParseEffects(List<Dictionary<string, string>> rows)
    {
        var result = new Dictionary<int, List<EffectRow>>();

        foreach (var row in rows)
        {
            if (!SheetReader.TryGetInt(row, "skillId", out int skillId))
            {
                Debug.Log($"[{LogTag}] 효과 행의 skillId가 숫자가 아니라 건너뜁니다.");
                continue;
            }

            if (!TryParseEnum(row, "effectType", out SkillEffectType type, skillId)
                || !TryParseEnum(row, "valueType", out SkillValueType valueType, skillId))
                continue;

            if (!SkillEffectRules.IsValueTypeAllowed(type, valueType))
            {
                Debug.Log($"[{LogTag}] {type}은(는) {valueType} 단위를 지원하지 않아 건너뜁니다. 스킬: {skillId}");
                continue;
            }

            if (!SheetReader.TryGetFloat(row, "value", out float value))
            {
                Debug.Log($"[{LogTag}] 효과 수치(value)를 읽지 못해 건너뜁니다. 스킬: {skillId}, 효과: {type}");
                continue;
            }

            float duration = SheetReader.TryGetFloat(row, "duration", out float parsed) ? Mathf.Max(0f, parsed) : 0f;

            if (!result.TryGetValue(skillId, out var list))
            {
                list = new List<EffectRow>();
                result.Add(skillId, list);
            }

            list.Add(new EffectRow(type, value, valueType, duration));
        }

        return result;
    }

    private static bool TryParseEnum<T>(Dictionary<string, string> row, string key, out T value, int skillId) where T : struct
    {
        string text = SheetReader.Get(row, key);
        if (Enum.TryParse(text, true, out value) && Enum.IsDefined(typeof(T), value))
            return true;

        Debug.Log($"[{LogTag}] {key} 값을 해석하지 못해 건너뜁니다. 스킬: {skillId}, 값: '{text}', 가능한 값: {string.Join("/", Enum.GetNames(typeof(T)))}");
        return false;
    }

    // 타워 ID -> 코드명. 스킬 에셋 폴더 이름과 시트의 towerId 검증에 쓴다.
    private static Dictionary<int, string> CollectTowers()
    {
        var result = new Dictionary<int, string>();

        foreach (string guid in AssetDatabase.FindAssets("t:TowerData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<TowerData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null)
                continue;

            result[data.Id] = data.name.StartsWith(TowerDataPrefix) ? data.name.Substring(TowerDataPrefix.Length) : data.name;
        }

        return result;
    }

    private static Dictionary<int, TowerSkillData> CollectSkillAssets()
    {
        var result = new Dictionary<int, TowerSkillData>();

        foreach (string guid in AssetDatabase.FindAssets("t:TowerSkillData"))
        {
            var skill = AssetDatabase.LoadAssetAtPath<TowerSkillData>(AssetDatabase.GUIDToAssetPath(guid));
            if (skill != null)
                result[skill.Id] = skill;
        }

        return result;
    }

    private static TowerSkillData CreateAsset(string towerCode, string codeName)
    {
        if (!AssetDatabase.IsValidFolder(SkillFolder))
            AssetDatabase.CreateFolder("Assets/Datas", "Skills");

        string folder = $"{SkillFolder}/{towerCode}";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(SkillFolder, towerCode);

        string path = $"{folder}/Skill_{codeName}.asset";
        var skill = ScriptableObject.CreateInstance<TowerSkillData>();
        AssetDatabase.CreateAsset(skill, path);

        Debug.Log($"[{LogTag}] 새 스킬 에셋을 만들었습니다: {path}", skill);
        return skill;
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
