using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TowerSheetImporter
{
    private const string GradesFile = "Grades.csv";
    private const string TowersFile = "Towers.csv";
    private const string LevelUpCostsFile = "LevelUpCosts.csv";
    private const string ProjectilesFile = "TowerProjectiles.csv";
    private const string CharacterFolder = "Assets/Datas/Characters";
    private const string CatalogPath = "Assets/Datas/TowerCatalog.asset";
    private const string GradeStylePath = "Assets/Datas/GradeStyle.asset";
    private const int GradeDivisor = 100;

    [MenuItem("SlimeTD/Data/Import Tower Sheets")]
    public static void Import()
    {
        if (!TryReadTable(GradesFile, out var gradeRows)
            || !TryReadTable(TowersFile, out var towerRows)
            || !TryReadTable(LevelUpCostsFile, out var costRows)
            || !TryReadTable(ProjectilesFile, out var projectileRows))
            return;

        var grades = ParseGrades(gradeRows, costRows);
        var projectiles = new Dictionary<int, Dictionary<string, string>>();
        foreach (var row in projectileRows)
        {
            if (TryGetInt(row, "towerId", out int towerId))
                projectiles[towerId] = row;
        }
        var assets = CollectTowerAssets();
        var usedIds = new HashSet<int>();
        var imported = new SortedDictionary<int, TowerData>();
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

            if (!TryGetInt(row, "gradeId", out int sheetGradeId))
            {
                Debug.Log($"[TowerSheetImporter] gradeId를 읽지 못했습니다. ID 대역({gradeId})을 사용합니다: {id}");
            }
            else
            {
                if (sheetGradeId != gradeId)
                    Debug.Log($"[TowerSheetImporter] 등급 열({sheetGradeId})과 ID 대역({gradeId})이 다릅니다: {id}");

                gradeId = sheetGradeId;
            }

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

            projectiles.TryGetValue(id, out var projectileRow);
            Apply(data, id, gradeId, row, grade, projectileRow);
            imported[id] = data;
            updated++;
        }

        UpdateCatalog(imported);
        UpdateGradeStyle(grades);

        foreach (var pair in assets)
        {
            if (!usedIds.Contains(GetId(pair.Value)))
                Debug.Log($"[TowerSheetImporter] 시트에 없는 TowerData입니다: {pair.Key}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TowerSheetImporter] 임포트 완료. 갱신 {updated}개, 신규 {created}개");
    }

    private static void Apply(TowerData data, int id, int gradeId, Dictionary<string, string> row, GradeInfo grade, Dictionary<string, string> projectileRow)
    {
        var so = new SerializedObject(data);

        string attackText = Get(row, "attackType");
        if (!Enum.TryParse(attackText, false, out TowerAttackType attackType) || !Enum.IsDefined(typeof(TowerAttackType), attackType))
        {
            Debug.Log($"[TowerSheetImporter] attackType을 읽지 못해 근접으로 둡니다: {id} '{attackText}'");
            attackType = TowerAttackType.Melee;
        }

        so.FindProperty("_attackType").intValue = (int)attackType;
        if (attackType == TowerAttackType.Projectile && projectileRow == null)
            Debug.Log($"[TowerSheetImporter] 투사체 타워인데 {ProjectilesFile}에 행이 없습니다: {id}");

        so.FindProperty("_projectileSpeed").floatValue = projectileRow != null && TryGetFloat(projectileRow, "speed", out float speed) ? speed : 0f;
        so.FindProperty("_projectileHitRadius").floatValue = projectileRow != null && TryGetFloat(projectileRow, "hitRadius", out float hitRadius) ? hitRadius : 0f;
        so.FindProperty("_projectileLifetime").floatValue = projectileRow != null && TryGetFloat(projectileRow, "lifetime", out float lifetime) ? lifetime : 0f;

        so.FindProperty("_id").intValue = id;
        so.FindProperty("_gradeId").intValue = gradeId;
        so.FindProperty("_codeName").stringValue = Get(row, "codeName");
        so.FindProperty("_enabled").boolValue = !TryGetInt(row, "enabled", out int enabled) || enabled != 0;
        so.FindProperty("_towerName").stringValue = Get(row, "name");
        so.FindProperty("_baseAttack").floatValue = grade.BaseAttack;
        so.FindProperty("_attackGrowthRate").floatValue = grade.AttackGrowthRate;
        bool hasUnlock = TryGetInt(row, "startUnlocked", out int startUnlocked);
        if (!hasUnlock)
            Debug.Log($"[TowerSheetImporter] startUnlocked가 비어 있어 잠금으로 둡니다: {id}");
        so.FindProperty("_startUnlocked").boolValue = hasUnlock && startUnlocked != 0;
        so.FindProperty("_unlockGemPrice").intValue = TryGetInt(row, "unlockGemPrice", out int gemPrice) ? gemPrice : 0;
        so.FindProperty("_inGacha").boolValue = !TryGetInt(row, "inGacha", out int inGacha) || inGacha != 0;

        SetIntArray(so.FindProperty("_levelUpCost._cards"), grade.Cards);
        SetIntArray(so.FindProperty("_levelUpCost._gold"), grade.Gold);

        SetFloat(so, "_attackRange", row, "attackRange");
        SetFloat(so, "_attackCooldown", row, "attackCooldown");
        SetFloat(so, "_critChance", row, "critChance");
        SetFloat(so, "_critBonus", row, "critBonus");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void UpdateGradeStyle(Dictionary<int, GradeInfo> grades)
    {
        var style = AssetDatabase.LoadAssetAtPath<GradeStyle>(GradeStylePath);
        if (style == null)
        {
            Debug.Log($"[TowerSheetImporter] GradeStyle을 찾을 수 없습니다: {GradeStylePath}");
            return;
        }

        var so = new SerializedObject(style);
        var colors = so.FindProperty("_colors");

        foreach (var pair in grades)
        {
            int index = pair.Key - 1;
            if (index < 0 || index >= colors.arraySize)
            {
                Debug.Log($"[TowerSheetImporter] GradeStyle에 등급 {pair.Key} 칸이 없습니다.", style);
                continue;
            }

            colors.GetArrayElementAtIndex(index).FindPropertyRelative("_name").stringValue = pair.Value.Name;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(style);
    }

    private static void UpdateCatalog(SortedDictionary<int, TowerData> towers)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<TowerCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<TowerCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            Debug.Log($"[TowerSheetImporter] TowerCatalog를 만들었습니다: {CatalogPath}", catalog);
        }

        var so = new SerializedObject(catalog);
        var list = so.FindProperty("_towers");
        list.arraySize = towers.Count;

        int index = 0;
        foreach (var pair in towers)
            list.GetArrayElementAtIndex(index++).objectReferenceValue = pair.Value;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void SetIntArray(SerializedProperty array, int[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).intValue = values[i];
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

    private static Dictionary<int, GradeInfo> ParseGrades(List<Dictionary<string, string>> rows, List<Dictionary<string, string>> costRows)
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

            ParseLevelUpCosts(costRows, gradeId, out int[] cards, out int[] gold);

            result[gradeId] = new GradeInfo(Get(row, "name"), baseAttack, growthRate, cards, gold);
        }

        return result;
    }

    // level은 올리기 전 레벨이다. 등급마다 1부터 (최대 레벨 - 1)까지 빠짐없이 있어야 한다.
    private static void ParseLevelUpCosts(List<Dictionary<string, string>> rows, int gradeId, out int[] cards, out int[] gold)
    {
        int count = TowerLevelService.MaxLevel - TowerLevelService.MinLevel;
        cards = new int[count];
        gold = new int[count];
        var filled = new bool[count];

        foreach (var row in rows)
        {
            if (!TryGetInt(row, "gradeId", out int rowGradeId) || rowGradeId != gradeId)
                continue;

            if (!TryGetInt(row, "level", out int level))
                continue;

            int index = level - TowerLevelService.MinLevel;
            if (index < 0 || index >= count)
            {
                Debug.Log($"[TowerSheetImporter] {LevelUpCostsFile}의 레벨 범위를 벗어난 행입니다. 등급 {gradeId}, 레벨 {level}");
                continue;
            }

            if (filled[index])
                Debug.Log($"[TowerSheetImporter] {LevelUpCostsFile}에 중복된 행이 있어 아래 행을 씁니다. 등급 {gradeId}, 레벨 {level}");

            if (!TryGetInt(row, "cards", out cards[index]) || !TryGetInt(row, "gold", out gold[index]))
                Debug.Log($"[TowerSheetImporter] {LevelUpCostsFile}의 cards 또는 gold를 읽지 못했습니다. 등급 {gradeId}, 레벨 {level}");

            filled[index] = true;
        }

        for (int i = 0; i < count; i++)
        {
            if (!filled[i])
                Debug.Log($"[TowerSheetImporter] {LevelUpCostsFile}에 등급 {gradeId}, 레벨 {i + TowerLevelService.MinLevel} 행이 없습니다.");
        }
    }

    // 1줄은 영문 키다. 열 설명은 시트 폴더의 README.md에 있다.
    private static bool TryReadTable(string fileName, out List<Dictionary<string, string>> rows)
    {
        rows = new List<Dictionary<string, string>>();

        string path = SheetReader.PathOf(fileName);
        if (!File.Exists(path))
        {
            Debug.Log($"[TowerSheetImporter] 시트를 찾을 수 없습니다: {path}");
            return false;
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 1)
        {
            Debug.Log($"[TowerSheetImporter] 시트가 비어 있습니다: {path}");
            return false;
        }

        string[] keys = SplitLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
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
        public readonly string Name;
        public readonly float BaseAttack;
        public readonly float AttackGrowthRate;
        public readonly int[] Cards;
        public readonly int[] Gold;

        public GradeInfo(string name, float baseAttack, float attackGrowthRate, int[] cards, int[] gold)
        {
            Name = name;
            BaseAttack = baseAttack;
            AttackGrowthRate = attackGrowthRate;
            Cards = cards;
            Gold = gold;
        }
    }
}
