using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

// 형식: SlimeTD-Server/docs/map-format.md (formatVersion 1)
public static class BattleMapExporter
{
    private const int FormatVersion = 1;
    private const float SampleStep = 0.05f;
    private const string MapFolder = "Assets/Prefabs/Game/Map";

    [MenuItem("SlimeTD/Data/Export Battle Maps")]
    public static void ExportAll()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "SlimeTD-Data", "Maps"));
        if (!Directory.Exists(Path.GetDirectoryName(outDir)))
        {
            Debug.LogError($"[BattleMapExporter] 데이터 저장소를 찾을 수 없습니다: {outDir}");
            return;
        }

        Directory.CreateDirectory(outDir);

        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { MapFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var map = prefab != null ? prefab.GetComponent<GameMap>() : null;
            if (map == null)
                continue;

            if (!TryBuildJson(map, prefab.name, out string json))
                continue;

            string file = Path.Combine(outDir, prefab.name + ".json");
            File.WriteAllText(file, json, new UTF8Encoding(false));
            Debug.Log($"[BattleMapExporter] 내보냄: {file}");
            count++;
        }

        Debug.Log($"[BattleMapExporter] 완료: {count}개");
    }

    private static bool TryBuildJson(GameMap map, string mapKey, out string json)
    {
        json = null;
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append($"  \"formatVersion\": {FormatVersion},\n");
        sb.Append($"  \"mapKey\": \"{mapKey}\",\n");

        sb.Append("  \"zones\": [");
        for (int i = 0; i < map.Zones.Count; i++)
        {
            var zone = map.Zones[i];
            var grid = zone != null ? zone.GridMapData : null;
            if (grid == null)
            {
                Debug.LogError($"[BattleMapExporter] {mapKey}: 구역 {i}에 GridMapData가 없습니다.", map);
                return false;
            }

            sb.Append(i == 0 ? "\n" : ",\n");
            sb.Append("    {\n");
            sb.Append($"      \"slot\": {zone.Slot},\n");
            sb.Append($"      \"center\": {Vec(grid.CenterPosition)},\n");
            sb.Append($"      \"width\": {grid.Width},\n");
            sb.Append($"      \"height\": {grid.Height},\n");
            sb.Append("      \"rows\": [");
            for (int y = 0; y < grid.Height; y++)
            {
                var row = new StringBuilder(grid.Width);
                for (int x = 0; x < grid.Width; x++)
                    row.Append((int)grid.GetCellState(x, y));

                sb.Append(y == 0 ? "" : ", ").Append('"').Append(row).Append('"');
            }
            sb.Append("]\n    }");
        }
        sb.Append("\n  ],\n");

        sb.Append("  \"paths\": [");
        for (int i = 0; i < map.Paths.Count; i++)
        {
            var path = map.Paths[i];
            var container = path != null ? path.Spline : null;
            if (container == null || container.Splines.Count != 1)
            {
                Debug.LogError($"[BattleMapExporter] {mapKey}: 경로 {i}의 스플라인은 정확히 1개여야 합니다.", map);
                return false;
            }

            sb.Append(i == 0 ? "\n" : ",\n");
            sb.Append($"    {{ \"entrance\": {path.Entrance}, \"points\": [");
            AppendSamples(sb, container);
            sb.Append("] }");
        }
        sb.Append("\n  ]\n}\n");

        json = sb.ToString();
        return true;
    }

    // 스플라인 로컬 길이와 월드 길이가 다를 수 있어(스케일) 월드 간격을 로컬 거리로 바꿔 샘플링한다
    private static void AppendSamples(StringBuilder sb, SplineContainer container)
    {
        var spline = container.Splines[0];
        float worldLength = container.CalculateLength(0);
        float localLength = spline.GetLength();
        float localPerWorld = worldLength > 0f ? localLength / worldLength : 1f;

        int steps = Mathf.FloorToInt(worldLength / SampleStep);
        bool first = true;
        for (int i = 0; i <= steps; i++)
        {
            float t = spline.ConvertIndexUnit(i * SampleStep * localPerWorld, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            if (i > 0 && t >= 1f)
                break;

            AppendPoint(sb, container.EvaluatePosition(0, i == 0 ? 0f : t), ref first);
        }

        AppendPoint(sb, container.EvaluatePosition(0, 1f), ref first);
    }

    private static void AppendPoint(StringBuilder sb, Vector3 position, ref bool first)
    {
        sb.Append(first ? "" : ", ").Append(Vec(position));
        first = false;
    }

    private static string Vec(Vector3 v)
    {
        return $"[{Num(v.x)}, {Num(v.y)}, {Num(v.z)}]";
    }

    private static string Num(float value)
    {
        double rounded = System.Math.Round(value, 4);
        if (rounded == 0d)
            rounded = 0d;
        return rounded.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
