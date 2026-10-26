using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class SheetReader
{
    // 1줄은 영문 키, 2줄은 한글 설명이라 건너뛴다.
    public static bool TryReadTable(string path, string logTag, out List<Dictionary<string, string>> rows)
    {
        rows = new List<Dictionary<string, string>>();

        if (!File.Exists(path))
        {
            Debug.Log($"[{logTag}] 시트를 찾을 수 없습니다: {path}");
            return false;
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.Log($"[{logTag}] 시트가 비어 있습니다: {path}");
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

    public static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : string.Empty;
    }

    public static bool TryGetInt(Dictionary<string, string> row, string key, out int value)
    {
        return int.TryParse(Get(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryGetFloat(Dictionary<string, string> row, string key, out float value)
    {
        return float.TryParse(Get(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    // 엑셀이 쉼표가 든 칸을 따옴표로 감싸 저장하므로 따옴표 안의 쉼표는 구분자로 보지 않는다.
    private static string[] SplitLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;

        foreach (char c in line.TrimStart('\uFEFF'))
        {
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (c == ',' && !quoted)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        cells.Add(current.ToString());
        return cells.ToArray();
    }
}
