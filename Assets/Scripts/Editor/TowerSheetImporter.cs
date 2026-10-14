using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TowerSheetImporter
{
    private const string SheetFolder = "Assets/Datas/Sheets";
    private const string GradesFile = "Grades.csv";
    private const string TowersFile = "Towers.csv";
    private const string CharacterFolder = "Assets/Datas/Characters";
    private const int GradeDivisor = 100;

    [MenuItem("SlimeTD/Data/Import Tower Sheets")]
    public static void Import()
    {
        if (!TryReadTable(GradesFile, out var gradeRows) || !TryReadTable(TowersFile, out var towerRows))
            return;

        var grades = ParseGrades(gradeRows);
        var assets = CollectTowerAssets();
        var usedIds = new HashSet<int>();
        int updated = 0;
        int created = 0;

        foreach (var row in towerRows)
        {
            if (!TryGetInt(row, "towerId", out int id))
            {
                Debug.Log("[TowerSheetImporter] towerId가 숫자가 아닌 행을 건너뜁니다.");
                continue;
            }

            if (!usedIds.Add(id))
            {
                Debug.Log($"[TowerSheetImporter] 중복된 타워 ID입니다: {id}");
                continue;
            }

            string codeName = Get(row, "codeName");
            int gradeId = id / GradeDivisor;

            if (!grades.TryGetValue(gradeId, out var grade))
            {
                Debug.Log($"[TowerSheetImporter] 등급 {gradeId}을 찾을 수 없습니다. 타워 ID: {id}");
                continue;
            }

            var data = FindAsset(assets, id, codeName);
            if (data == null)
            {
                data = CreateAsset(codeName);
                if (data == null)
                    continue;

                created++;
            }

            Apply(data, id, row, grade);
            updated++;
        }

        foreach (var pair in assets)
        {
            if (!usedIds.Contains(GetId(pair.Value)))
                Debug.Log($"[TowerSheetImporter] 시트에 없는 TowerData입니다: {pair.Key}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TowerSheetImporter] 임포트 완료. 갱신 {updated}개, 신규 {created}개");
    }

    private static void Apply(TowerData data, int id, Dictionary<string, string> row, GradeInfo grade)
    {
        var so = new SerializedObject(data);

        so.FindProperty("_id").intValue = id;
        so.FindProperty("_towerName").stringValue = Get(row, "name");
        so.FindProperty("_baseAttack").floatValue = grade.BaseAttack;
        so.FindProperty("_attackGrowthRate").floatValue = grade.AttackGrowthRate;

        SetFloat(so, "_attackRange", row, "attackRange");
        SetFloat(so, "_attackCooldown", row, "attackCooldown");
        SetFloat(so, "_critChance", row, "critChance");
        SetFloat(so, "_critBonus", row, "critBonus");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void SetFloat(SerializedObject so, string property, Dictionary<string, string> row, string key)
    {
        if (TryGetFloat(row, key, out float value))
            so.FindProperty(property).floatValue = value;
        else
            Debug.Log($"[TowerSheetImporter] {key} 값을 읽지 못했습니다: {Get(row, "codeName")}");
    }

    private static Dictionary<string, TowerData> CollectTowerAssets()
    {
        var result = new Dictionary<string, TowerData>();

        foreach (string guid in AssetDatabase.FindAssets("t:TowerData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<TowerData>(path);
            if (data != null)
                result[path] = data;
        }

        return result;
    }

    private static TowerData FindAsset(Dictionary<string, TowerData> assets, int id, string codeName)
    {
        TowerData byName = null;

        foreach (var pair in assets)
        {
            if (GetId(pair.Value) == id)
            {
                CheckCodeName(pair.Value, codeName);
                return pair.Value;
            }

            if (byName == null && pair.Value.name == "TowerData_" + codeName)
                byName = pair.Value;
        }

        return byName;
    }

    private static void CheckCodeName(TowerData data, string codeName)
    {
        if (data.name != "TowerData_" + codeName)
            Debug.Log($"[TowerSheetImporter] 에셋 이름과 코드명이 다릅니다. 에셋: {data.name}, 코드명: {codeName}", data);
    }

    private static TowerData CreateAsset(string codeName)
    {
        if (string.IsNullOrEmpty(codeName))
        {
            Debug.Log("[TowerSheetImporter] 코드명이 없어 새 TowerData를 만들 수 없습니다.");
            return null;
        }

        string folder = $"{CharacterFolder}/{codeName}";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(CharacterFolder, codeName);

        string path = $"{folder}/TowerData_{codeName}.asset";
        var data = ScriptableObject.CreateInstance<TowerData>();
        AssetDatabase.CreateAsset(data, path);

        Debug.Log($"[TowerSheetImporter] 새 TowerData를 만들었습니다. 프리팹과 기본 공격을 연결하세요: {path}", data);
        return data;
    }

    private static int GetId(TowerData data)
    {
        var so = new SerializedObject(data);
        return so.FindProperty("_id").intValue;
    }

    private static Dictionary<int, GradeInfo> ParseGrades(List<Dictionary<string, string>> rows)
    {
        var result = new Dictionary<int, GradeInfo>();

        foreach (var row in rows)
        {
            if (!TryGetInt(row, "gradeId", out int gradeId)
                || !TryGetFloat(row, "baseAttack", out float baseAttack)
                || !TryGetFloat(row, "attackGrowthRate", out float growthRate))
            {
                Debug.Log("[TowerSheetImporter] 등급 행을 읽지 못해 건너뜁니다.");
                continue;
            }

            result[gradeId] = new GradeInfo(baseAttack, growthRate);
        }

        return result;
    }

    // 1줄은 영문 키, 2줄은 한글 설명이라 건너뛴다.
    private static bool TryReadTable(string fileName, out List<Dictionary<string, string>> rows)
    {
        rows = new List<Dictionary<string, string>>();

        string path = $"{SheetFolder}/{fileName}";
        if (!File.Exists(path))
        {
            Debug.Log($"[TowerSheetImporter] 시트를 찾을 수 없습니다: {path}");
            return false;
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.Log($"[TowerSheetImporter] 시트가 비어 있습니다: {path}");
            return false;
        }

        string[] keys = SplitLine(lines[0]);
        for (int i = 2; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cells = SplitLine(lines[i]);
            var row = new Dictionary<string, string>();

            for (int c = 0; c < keys.Length; c++)
                row[keys[c].Trim()] = c < cells.Length ? cells[c].Trim() : string.Empty;

            rows.Add(row);
        }

        return true;
    }

    private static string[] SplitLine(string line)
    {
        return line.TrimStart('﻿').Split(',');
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static bool TryGetInt(Dictionary<string, string> row, string key, out int value)
    {
        return int.TryParse(Get(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetFloat(Dictionary<string, string> row, string key, out float value)
    {
        return float.TryParse(Get(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private readonly struct GradeInfo
    {
        public readonly float BaseAttack;
        public readonly float AttackGrowthRate;

        public GradeInfo(float baseAttack, float attackGrowthRate)
        {
            BaseAttack = baseAttack;
            AttackGrowthRate = attackGrowthRate;
        }
    }
}
