using System.Collections.Generic;

public static class PrefabKeys
{
    public const string Archer = "Archer";
    public const string ArcherArrow = "ArcherArrow";
    public const string Crossbow = "Crossbow";
    public const string CrossbowBolt = "CrossbowBolt";
    public const string Knight = "Knight";
    public const string Lumberjack = "Lumberjack";
    public const string SlimeTier1_Blue = "SlimeTier1_Blue";
    public const string SlimeTier1_Orange = "SlimeTier1_Orange";
    public const string SlimeTier1_White = "SlimeTier1_White";
    public const string Swordsman = "Swordsman";

    public static readonly Dictionary<string, string> PrefabPaths = new Dictionary<string, string>()
    {
        { Archer, "Assets/Prefabs/Game/Characters/Archer.prefab" },
        { ArcherArrow, "Assets/Prefabs/Game/Characters/ArcherArrow.prefab" },
        { Crossbow, "Assets/Prefabs/Game/Characters/Crossbow.prefab" },
        { CrossbowBolt, "Assets/Prefabs/Game/Characters/CrossbowBolt.prefab" },
        { Knight, "Assets/Prefabs/Game/Characters/Knight.prefab" },
        { Lumberjack, "Assets/Prefabs/Game/Characters/Lumberjack.prefab" },
        { SlimeTier1_Blue, "Assets/Prefabs/Game/Slime/SlimeTier1_Blue.prefab" },
        { SlimeTier1_Orange, "Assets/Prefabs/Game/Slime/SlimeTier1_Orange.prefab" },
        { SlimeTier1_White, "Assets/Prefabs/Game/Slime/SlimeTier1_White.prefab" },
        { Swordsman, "Assets/Prefabs/Game/Characters/Swordsman.prefab" },
    };

    public static string GetPrefabPath(string key)
    {
        return PrefabPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
