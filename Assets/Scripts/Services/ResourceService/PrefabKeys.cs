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
    public const string GameResultPopup = "GameResultPopup";
    public const string Knight = "Knight";
    public const string KnightShield = "KnightShield";
    public const string KnightSword = "KnightSword";
    public const string Lumberjack = "Lumberjack";
    public const string LumberjackAxe = "LumberjackAxe";
    public const string Ranger = "Ranger";
    public const string RangerArrow = "RangerArrow";
    public const string RangerBow = "RangerBow";
    public const string SlimeBoss_01 = "SlimeBoss_01";
    public const string SlimeBoss_02 = "SlimeBoss_02";
    public const string SlimeBoss_03 = "SlimeBoss_03";
    public const string SlimeBoss_04 = "SlimeBoss_04";
    public const string SlimeBoss_05 = "SlimeBoss_05";
    public const string SlimeNormal_01 = "SlimeNormal_01";
    public const string SlimeNormal_02 = "SlimeNormal_02";
    public const string SlimeNormal_03 = "SlimeNormal_03";
    public const string SlimeNormal_04 = "SlimeNormal_04";
    public const string SlimeNormal_05 = "SlimeNormal_05";
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
        { GameResultPopup, "Assets/Prefabs/Game/UI/Popup/GameResultPopup.prefab" },
        { Knight, "Assets/Prefabs/Game/Characters/Knight.prefab" },
        { KnightShield, "Assets/Prefabs/Game/Characters/KnightShield.prefab" },
        { KnightSword, "Assets/Prefabs/Game/Characters/KnightSword.prefab" },
        { Lumberjack, "Assets/Prefabs/Game/Characters/Lumberjack.prefab" },
        { LumberjackAxe, "Assets/Prefabs/Game/Characters/LumberjackAxe.prefab" },
        { Ranger, "Assets/Prefabs/Game/Characters/Ranger.prefab" },
        { RangerArrow, "Assets/Prefabs/Game/Characters/RangerArrow.prefab" },
        { RangerBow, "Assets/Prefabs/Game/Characters/RangerBow.prefab" },
        { SlimeBoss_01, "Assets/Prefabs/Game/Slime/SlimeBoss_01.prefab" },
        { SlimeBoss_02, "Assets/Prefabs/Game/Slime/SlimeBoss_02.prefab" },
        { SlimeBoss_03, "Assets/Prefabs/Game/Slime/SlimeBoss_03.prefab" },
        { SlimeBoss_04, "Assets/Prefabs/Game/Slime/SlimeBoss_04.prefab" },
        { SlimeBoss_05, "Assets/Prefabs/Game/Slime/SlimeBoss_05.prefab" },
        { SlimeNormal_01, "Assets/Prefabs/Game/Slime/SlimeNormal_01.prefab" },
        { SlimeNormal_02, "Assets/Prefabs/Game/Slime/SlimeNormal_02.prefab" },
        { SlimeNormal_03, "Assets/Prefabs/Game/Slime/SlimeNormal_03.prefab" },
        { SlimeNormal_04, "Assets/Prefabs/Game/Slime/SlimeNormal_04.prefab" },
        { SlimeNormal_05, "Assets/Prefabs/Game/Slime/SlimeNormal_05.prefab" },
        { Sword, "Assets/Prefabs/Game/Characters/Sword.prefab" },
        { Swordsman, "Assets/Prefabs/Game/Characters/Swordsman.prefab" },
    };

    public static string GetPrefabPath(string key)
    {
        return PrefabPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
