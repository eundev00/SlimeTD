using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class KoreanCharsetBuilder
{
    private const string OutputPath = "Doc/korean_charset.txt";
    private const string ThirdPartyFolder = "Assets/ThirdParty";

    private static readonly string[] DataFolders = { "Assets/Datas/Sheets", "Assets/Datas/Characters" };
    private static readonly string[] CodeFolders = { "Assets/Scripts/Lobby", "Assets/Scripts/Game/Hud" };

    private static readonly Regex TextField = new Regex(@"m_text: (""(?:[^""\\\n]|\\.)*""|.*)", RegexOptions.Compiled);
    private static readonly Regex UnicodeEscape = new Regex(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);
    private static readonly Regex StringLiteral = new Regex(@"""((?:[^""\\\n]|\\.)*)""", RegexOptions.Compiled);
    private static readonly Regex LineComment = new Regex(@"//.*", RegexOptions.Compiled);

    [MenuItem("SlimeTD/Data/Build Korean Charset")]
    public static void Build()
    {
        var used = new SortedSet<char>();

        CollectFromAssets(used);
        CollectFromData(used);
        CollectFromCode(used);

        var builder = new StringBuilder();
        for (char c = ' '; c <= '~'; c++)
            builder.Append(c);
        foreach (char extra in used.Where(x => x > '~'))
            builder.Append(extra);

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
        File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));

        int hangulCount = used.Count(IsHangul);
        Debug.Log($"[KoreanCharsetBuilder] 문자셋 생성 완료: 한글 {hangulCount}자, 전체 {builder.Length}자 ({OutputPath})");
    }

    // 프리팹/씬의 m_text는 \uXXXX 이스케이프로 저장된다
    private static void CollectFromAssets(SortedSet<char> used)
    {
        foreach (string path in EnumerateFiles(new[] { "Assets" }, "*.prefab", "*.unity"))
        {
            foreach (Match match in TextField.Matches(File.ReadAllText(path)))
            {
                string value = match.Groups[1].Value;
                if (value.StartsWith("\""))
                    value = Unescape(value.Substring(1, value.Length - 2));

                AddNonAscii(used, value);
            }
        }
    }

    private static void CollectFromData(SortedSet<char> used)
    {
        foreach (string path in EnumerateFiles(DataFolders, "*.csv", "*.asset"))
            AddNonAscii(used, Unescape(File.ReadAllText(path)), hangulOnly: true);
    }

    // 로그([태그] 시작)는 화면에 나오지 않으므로 제외한다
    private static void CollectFromCode(SortedSet<char> used)
    {
        foreach (string path in EnumerateFiles(CodeFolders, "*.cs"))
        {
            string code = LineComment.Replace(File.ReadAllText(path), string.Empty);
            foreach (Match match in StringLiteral.Matches(code))
            {
                string value = match.Groups[1].Value;
                if (!value.StartsWith("["))
                    AddNonAscii(used, value, hangulOnly: true);
            }
        }
    }

    private static IEnumerable<string> EnumerateFiles(string[] folders, params string[] patterns)
    {
        foreach (string folder in folders)
        {
            foreach (string pattern in patterns)
            {
                foreach (string path in Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories))
                {
                    if (!path.Replace('\\', '/').StartsWith(ThirdPartyFolder))
                        yield return path;
                }
            }
        }
    }

    private static string Unescape(string value)
    {
        return UnicodeEscape.Replace(value, m => ((char)System.Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }

    private static void AddNonAscii(SortedSet<char> used, string value, bool hangulOnly = false)
    {
        foreach (char c in value)
        {
            if (c <= '~' || char.IsWhiteSpace(c))
                continue;

            if (hangulOnly && !IsHangul(c))
                continue;

            used.Add(c);
        }
    }

    private static bool IsHangul(char c)
    {
        return (c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㆎ');
    }
}
