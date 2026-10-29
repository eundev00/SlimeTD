using System.Collections.Generic;

public static class AtlasKeys
{
    public const string SkillIconAtlas = "SkillIconAtlas";
    public const string ThumbnailAtlas = "ThumbnailAtlas";

    public static readonly Dictionary<string, string> AtlasPaths = new Dictionary<string, string>()
    {
        { SkillIconAtlas, "Assets/Art/Atlas/SkillIconAtlas.spriteatlasv2" },
        { ThumbnailAtlas, "Assets/Art/Atlas/ThumbnailAtlas.spriteatlasv2" },
    };

    public static string GetAtlasPath(string key)
    {
        return AtlasPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
