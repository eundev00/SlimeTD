using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ResourceKeysGenerator
{
    private const string OutputFolder = "Assets/Scripts/Services/ResourceService";

    [MenuItem("Tools/SlimeTD/Resource Keys/Create Data Keys")]
    public static void CreateDataKeys()
    {
        Generate("DataKeys", "DataPaths", "GetDataPath", "t:ScriptableObject", new[] { "Assets/Datas" });
    }

    [MenuItem("Tools/SlimeTD/Resource Keys/Create Prefab Keys")]
    public static void CreatePrefabKeys()
    {
        Generate("PrefabKeys", "PrefabPaths", "GetPrefabPath", "t:prefab", new[] { "Assets/Prefabs" });
    }

    [MenuItem("Tools/SlimeTD/Resource Keys/Create Atlas Keys")]
    public static void CreateAtlasKeys()
    {
        Generate("AtlasKeys", "AtlasPaths", "GetAtlasPath", "t:SpriteAtlas", new[] { "Assets/Art/Images" });
    }

    private static void Generate(string className, string dictionaryName, string methodName, string filter, string[] searchFolders)
    {
        var validFolders = new List<string>();
        foreach (var folder in searchFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
                validFolders.Add(folder);
        }

        if (validFolders.Count == 0)
        {
            Debug.Log($"[ResourceKeysGenerator] {className}: 검색 폴더가 없다 ({string.Join(", ", searchFolders)})");
            return;
        }

        try
        {
            var entries = CollectEntries(filter, validFolders.ToArray(), className);
            if (entries == null)
                return;

            var path = $"{OutputFolder}/{className}.cs";
            File.WriteAllText(path, BuildContents(className, dictionaryName, methodName, entries));
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[ResourceKeysGenerator] {className} 생성 완료: {entries.Count}개");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ResourceKeysGenerator] {className} 생성 실패: {e}");
        }
    }

    private static List<(string Key, string Path)> CollectEntries(string filter, string[] searchFolders, string className)
    {
        var assetGuids = AssetDatabase.FindAssets(filter, searchFolders);
        var entries = new List<(string Key, string Path)>();
        var usedKeys = new Dictionary<string, string>();

        foreach (var guid in assetGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var key = ToIdentifier(Path.GetFileNameWithoutExtension(assetPath));

            // 키가 겹치면 const/Dictionary가 중복 정의되어 컴파일이 깨진다
            if (usedKeys.TryGetValue(key, out var existingPath))
            {
                Debug.LogError($"[ResourceKeysGenerator] {className}: 키 중복으로 생성을 중단한다: {key}\n{existingPath}\n{assetPath}");
                return null;
            }

            usedKeys.Add(key, assetPath);
            entries.Add((key, assetPath));
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        return entries;
    }

    private static string BuildContents(string className, string dictionaryName, string methodName, List<(string Key, string Path)> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        foreach (var entry in entries)
        {
            sb.AppendLine($"    public const string {entry.Key} = \"{entry.Key}\";");
        }

        sb.AppendLine();
        sb.AppendLine($"    public static readonly Dictionary<string, string> {dictionaryName} = new Dictionary<string, string>()");
        sb.AppendLine("    {");

        foreach (var entry in entries)
        {
            sb.AppendLine($"        {{ {entry.Key}, \"{entry.Path}\" }},");
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine($"    public static string {methodName}(string key)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return {dictionaryName}.TryGetValue(key, out var path) ? path : string.Empty;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ToIdentifier(string fileName)
    {
        var sanitized = Regex.Replace(fileName, @"[^A-Za-z0-9_]", "_");
        return char.IsDigit(sanitized[0]) ? "_" + sanitized : sanitized;
    }
}
