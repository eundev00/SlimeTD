using System.Collections.Generic;

public static class PrefabKeys
{
    public const string Archer = "Archer";
    public const string ArcherArrow = "ArcherArrow";
    public const string ArcherBow = "ArcherBow";
    public const string Crossbow = "Crossbow";
    public const string CrossbowBolt = "CrossbowBolt";
    public const string CrossbowBow = "CrossbowBow";
    public const string DamageText = "DamageText";
    public const string Knight = "Knight";
    public const string KnightShield = "KnightShield";
    public const string KnightSword = "KnightSword";
    public const string Lumberjack = "Lumberjack";
    public const string LumberjackAxe = "LumberjackAxe";
    public const string Ranger = "Ranger";
    public const string RangerArrow = "RangerArrow";
    public const string RangerBow = "RangerBow";
    public const string SlimeTier1_Blue = "SlimeTier1_Blue";
    public const string SlimeTier1_Green = "SlimeTier1_Green";
    public const string SlimeTier1_Orange = "SlimeTier1_Orange";
    public const string SlimeTier1_White = "SlimeTier1_White";
    public const string SlimeTier2_Pink = "SlimeTier2_Pink";
    public const string SlimeTier3_BossSlime = "SlimeTier3_BossSlime";
    public const string Sword = "Sword";
    public const string Swordsman = "Swordsman";

    public static readonly Dictionary<string, string> PrefabPaths = new Dictionary<string, string>()
    {
        { Archer, "Assets/Prefabs/Game/Characters/Archer.prefab" },
        { ArcherArrow, "Assets/Prefabs/Game/Characters/ArcherArrow.prefab" },
        { ArcherBow, "Assets/Prefabs/Game/Characters/ArcherBow.prefab" },
        { Crossbow, "Assets/Prefabs/Game/Characters/Crossbow.prefab" },
        { CrossbowBolt, "Assets/Prefabs/Game/Characters/CrossbowBolt.prefab" },
        { CrossbowBow, "Assets/Prefabs/Game/Characters/CrossbowBow.prefab" },
        { DamageText, "Assets/Prefabs/Game/UI/DamageText.prefab" },
        { Knight, "Assets/Prefabs/Game/Characters/Knight.prefab" },
        { KnightShield, "Assets/Prefabs/Game/Characters/KnightShield.prefab" },
        { KnightSword, "Assets/Prefabs/Game/Characters/KnightSword.prefab" },
        { Lumberjack, "Assets/Prefabs/Game/Characters/Lumberjack.prefab" },
        { LumberjackAxe, "Assets/Prefabs/Game/Characters/LumberjackAxe.prefab" },
        { Ranger, "Assets/Prefabs/Game/Characters/Ranger.prefab" },
        { RangerArrow, "Assets/Prefabs/Game/Characters/RangerArrow.prefab" },
        { RangerBow, "Assets/Prefabs/Game/Characters/RangerBow.prefab" },
        { SlimeTier1_Blue, "Assets/Prefabs/Game/Slime/SlimeTier1_Blue.prefab" },
        { SlimeTier1_Green, "Assets/Prefabs/Game/Slime/SlimeTier1_Green.prefab" },
        { SlimeTier1_Orange, "Assets/Prefabs/Game/Slime/SlimeTier1_Orange.prefab" },
        { SlimeTier1_White, "Assets/Prefabs/Game/Slime/SlimeTier1_White.prefab" },
        { SlimeTier2_Pink, "Assets/Prefabs/Game/Slime/SlimeTier2_Pink.prefab" },
        { SlimeTier3_BossSlime, "Assets/Prefabs/Game/Slime/SlimeTier3_BossSlime.prefab" },
        { Sword, "Assets/Prefabs/Game/Characters/Sword.prefab" },
        { Swordsman, "Assets/Prefabs/Game/Characters/Swordsman.prefab" },
    };

    public static string GetPrefabPath(string key)
    {
        return PrefabPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
