using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HunterCardGachaSheetImporter
{
    private const string LogTag = "HunterCardGachaSheetImporter";
    private const string SheetFolder = "Assets/Datas/Sheets";
    private const string PacksFile = "HunterCardGachaPacks.csv";
    private const string SettingsFile = "HunterCardGachaSettings.csv";
    private const string GradesFile = "Grades.csv";
    private const string ConfigPath = "Assets/Datas/HunterCardGachaConfig.asset";

    [MenuItem("SlimeTD/Data/Import Hunter Card Gacha Sheets")]
    public static void Import()
    {
        if (!SheetReader.TryReadTable($"{SheetFolder}/{PacksFile}", LogTag, out var packRows)
            || !SheetReader.TryReadTable($"{SheetFolder}/{SettingsFile}", LogTag, out var settingRows)
            || !SheetReader.TryReadTable($"{SheetFolder}/{GradesFile}", LogTag, out var gradeRows))
            return;

        var config = AssetDatabase.LoadAssetAtPath<HunterCardGachaConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<HunterCardGachaConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            Debug.Log($"[{LogTag}] HunterCardGachaConfig를 만들었습니다: {ConfigPath}", config);
        }

        var so = new SerializedObject(config);
        int packCount = WritePacks(so.FindProperty("_packs"), packRows);
        WriteGradeWeights(so.FindProperty("_gradeWeights"), gradeRows);
        WriteSettings(so, ToSettings(settingRows));

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[{LogTag}] 임포트 완료. 팩 {packCount}개", config);
    }

    private static int WritePacks(SerializedProperty list, List<Dictionary<string, string>> rows)
    {
        var usedIds = new HashSet<int>();
        var valid = new List<Dictionary<string, string>>();

        foreach (var row in rows)
        {
            if (!SheetReader.TryGetInt(row, "packId", out int id))
            {
                Debug.Log($"[{LogTag}] packId가 숫자가 아닌 행을 건너뜁니다.");
                continue;
            }

            if (!usedIds.Add(id))
            {
                Debug.Log($"[{LogTag}] 중복된 팩 ID입니다: {id}");
                continue;
            }

            if (!SheetReader.TryGetInt(row, "cardCount", out int cardCount) || cardCount <= 0
                || !SheetReader.TryGetInt(row, "price", out int price) || price < 0)
            {
                Debug.Log($"[{LogTag}] 팩 {id}의 cardCount 또는 price를 읽지 못했습니다.");
                continue;
            }

            valid.Add(row);
        }

        list.arraySize = valid.Count;
        for (int i = 0; i < valid.Count; i++)
        {
            var row = valid[i];
            var element = list.GetArrayElementAtIndex(i);
            SheetReader.TryGetInt(row, "packId", out int id);
            SheetReader.TryGetInt(row, "cardCount", out int cardCount);
            SheetReader.TryGetInt(row, "price", out int price);
            SheetReader.TryGetInt(row, "guaranteeGradeId", out int guaranteeGradeId);

            element.FindPropertyRelative("_id").intValue = id;
            element.FindPropertyRelative("_cardCount").intValue = cardCount;
            element.FindPropertyRelative("_price").intValue = price;
            element.FindPropertyRelative("_guaranteeGradeId").intValue = guaranteeGradeId;
        }

        return valid.Count;
    }

    private static void WriteGradeWeights(SerializedProperty list, List<Dictionary<string, string>> rows)
    {
        var weights = new List<(int gradeId, float weight)>();

        foreach (var row in rows)
        {
            if (!SheetReader.TryGetInt(row, "gradeId", out int gradeId))
                continue;

            if (!SheetReader.TryGetFloat(row, "gachaWeight", out float weight) || weight < 0f)
            {
                Debug.Log($"[{LogTag}] 등급 {gradeId}의 gachaWeight를 읽지 못해 0으로 둡니다.");
                weight = 0f;
            }

            weights.Add((gradeId, weight));
        }

        list.arraySize = weights.Count;
        for (int i = 0; i < weights.Count; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_gradeId").intValue = weights[i].gradeId;
            element.FindPropertyRelative("_weight").floatValue = weights[i].weight;
        }
    }

    private static Dictionary<string, string> ToSettings(List<Dictionary<string, string>> rows)
    {
        var settings = new Dictionary<string, string>();
        foreach (var row in rows)
        {
            string key = SheetReader.Get(row, "key");
            if (!string.IsNullOrEmpty(key))
                settings[key] = SheetReader.Get(row, "value");
        }

        return settings;
    }

    private static void WriteSettings(SerializedObject so, Dictionary<string, string> settings)
    {
        so.FindProperty("_lockedCardChance").floatValue = ReadFloat(settings, "lockedCardChance");
        so.FindProperty("_pityCount").intValue = ReadInt(settings, "pityCount");
        so.FindProperty("_pityGradeId").intValue = ReadInt(settings, "pityGradeId");
        so.FindProperty("_maxLevelRefundGold").intValue = ReadInt(settings, "maxLevelRefundGold");
    }

    private static int ReadInt(Dictionary<string, string> settings, string key)
    {
        if (SheetReader.TryGetInt(settings, key, out int value))
            return value;

        Debug.Log($"[{LogTag}] {SettingsFile}의 {key} 값을 읽지 못해 0으로 둡니다.");
        return 0;
    }

    private static float ReadFloat(Dictionary<string, string> settings, string key)
    {
        if (SheetReader.TryGetFloat(settings, key, out float value))
            return value;

        Debug.Log($"[{LogTag}] {SettingsFile}의 {key} 값을 읽지 못해 0으로 둡니다.");
        return 0f;
    }
}
