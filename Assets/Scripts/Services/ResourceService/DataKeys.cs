using System.Collections.Generic;

public static class DataKeys
{
    public const string Attack_Archer_Basic = "Attack_Archer_Basic";
    public const string Attack_Crossbow_Basic = "Attack_Crossbow_Basic";
    public const string Attack_Knight_Basic = "Attack_Knight_Basic";
    public const string Attack_Lumberjack_Basic = "Attack_Lumberjack_Basic";
    public const string Attack_Ranger_Basic = "Attack_Ranger_Basic";
    public const string Attack_Swordsman_Basic = "Attack_Swordsman_Basic";
    public const string Boss_01 = "Boss_01";
    public const string Boss_02 = "Boss_02";
    public const string Boss_03 = "Boss_03";
    public const string Boss_04 = "Boss_04";
    public const string Boss_05 = "Boss_05";
    public const string GameConfig = "GameConfig";
    public const string GridMapData1 = "GridMapData1";
    public const string GridMapData1_Old = "GridMapData1_Old";
    public const string GridMapDataTemp = "GridMapDataTemp";
    public const string GridMapDataTest = "GridMapDataTest";
    public const string GridMapDataTest2 = "GridMapDataTest2";
    public const string SlimeDepthBucketSettings = "SlimeDepthBucketSettings";
    public const string Slime_01 = "Slime_01";
    public const string Slime_02 = "Slime_02";
    public const string Slime_03 = "Slime_03";
    public const string Slime_04 = "Slime_04";
    public const string Slime_05 = "Slime_05";
    public const string TowerData_Archer = "TowerData_Archer";
    public const string TowerData_Crossbow = "TowerData_Crossbow";
    public const string TowerData_Knight = "TowerData_Knight";
    public const string TowerData_Lumberjack = "TowerData_Lumberjack";
    public const string TowerData_Ranger = "TowerData_Ranger";
    public const string TowerData_Swordsman = "TowerData_Swordsman";
    public const string TowerTierTable = "TowerTierTable";
    public const string TowerTier_01 = "TowerTier_01";
    public const string TowerTier_02 = "TowerTier_02";
    public const string WaveEasyTable = "WaveEasyTable";

    public static readonly Dictionary<string, string> DataPaths = new Dictionary<string, string>()
    {
        { Attack_Archer_Basic, "Assets/Datas/Characters/Archer/Attack_Archer_Basic.asset" },
        { Attack_Crossbow_Basic, "Assets/Datas/Characters/Crossbow/Attack_Crossbow_Basic.asset" },
        { Attack_Knight_Basic, "Assets/Datas/Characters/Knight/Attack_Knight_Basic.asset" },
        { Attack_Lumberjack_Basic, "Assets/Datas/Characters/Lumberjack/Attack_Lumberjack_Basic.asset" },
        { Attack_Ranger_Basic, "Assets/Datas/Characters/Ranger/Attack_Ranger_Basic.asset" },
        { Attack_Swordsman_Basic, "Assets/Datas/Characters/Swordsman/Attack_Swordsman_Basic.asset" },
        { Boss_01, "Assets/Datas/Slimes/Boss_01.asset" },
        { Boss_02, "Assets/Datas/Slimes/Boss_02.asset" },
        { Boss_03, "Assets/Datas/Slimes/Boss_03.asset" },
        { Boss_04, "Assets/Datas/Slimes/Boss_04.asset" },
        { Boss_05, "Assets/Datas/Slimes/Boss_05.asset" },
        { GameConfig, "Assets/Datas/GameConfig.asset" },
        { GridMapData1, "Assets/Datas/Grid/GridMapData1.asset" },
        { GridMapData1_Old, "Assets/Datas/Grid/GridMapData1_Old.asset" },
        { GridMapDataTemp, "Assets/Datas/Grid/GridMapDataTemp.asset" },
        { GridMapDataTest, "Assets/Datas/Grid/GridMapDataTest.asset" },
        { GridMapDataTest2, "Assets/Datas/Grid/GridMapDataTest2.asset" },
        { SlimeDepthBucketSettings, "Assets/Datas/SlimeDepthBucketSettings.asset" },
        { Slime_01, "Assets/Datas/Slimes/Slime_01.asset" },
        { Slime_02, "Assets/Datas/Slimes/Slime_02.asset" },
        { Slime_03, "Assets/Datas/Slimes/Slime_03.asset" },
        { Slime_04, "Assets/Datas/Slimes/Slime_04.asset" },
        { Slime_05, "Assets/Datas/Slimes/Slime_05.asset" },
        { TowerData_Archer, "Assets/Datas/Characters/Archer/TowerData_Archer.asset" },
        { TowerData_Crossbow, "Assets/Datas/Characters/Crossbow/TowerData_Crossbow.asset" },
        { TowerData_Knight, "Assets/Datas/Characters/Knight/TowerData_Knight.asset" },
        { TowerData_Lumberjack, "Assets/Datas/Characters/Lumberjack/TowerData_Lumberjack.asset" },
        { TowerData_Ranger, "Assets/Datas/Characters/Ranger/TowerData_Ranger.asset" },
        { TowerData_Swordsman, "Assets/Datas/Characters/Swordsman/TowerData_Swordsman.asset" },
        { TowerTierTable, "Assets/Datas/TowerTierTable.asset" },
        { TowerTier_01, "Assets/Datas/TowerTier_01.asset" },
        { TowerTier_02, "Assets/Datas/TowerTier_02.asset" },
        { WaveEasyTable, "Assets/Datas/Waves/Easy/WaveEasyTable.asset" },
    };

    public static string GetDataPath(string key)
    {
        return DataPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
