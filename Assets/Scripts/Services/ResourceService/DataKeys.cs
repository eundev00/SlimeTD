using System.Collections.Generic;

public static class DataKeys
{
    public const string Attack_Archer_Basic = "Attack_Archer_Basic";
    public const string Attack_Crossbow_Basic = "Attack_Crossbow_Basic";
    public const string Attack_Knight_Basic = "Attack_Knight_Basic";
    public const string Attack_Lumberjack_Basic = "Attack_Lumberjack_Basic";
    public const string Attack_Ranger_Basic = "Attack_Ranger_Basic";
    public const string Attack_Swordsman_Basic = "Attack_Swordsman_Basic";
    public const string Auto_Orange = "Auto_Orange";
    public const string Auto_White = "Auto_White";
    public const string GameConfig = "GameConfig";
    public const string GridMapData1 = "GridMapData1";
    public const string GridMapData1_Old = "GridMapData1_Old";
    public const string GridMapDataTemp = "GridMapDataTemp";
    public const string GridMapDataTest = "GridMapDataTest";
    public const string GridMapDataTest2 = "GridMapDataTest2";
    public const string Indexed_W1 = "Indexed_W1";
    public const string Indexed_W10 = "Indexed_W10";
    public const string Indexed_W2 = "Indexed_W2";
    public const string Indexed_W3 = "Indexed_W3";
    public const string Indexed_W4 = "Indexed_W4";
    public const string Indexed_W5 = "Indexed_W5";
    public const string Indexed_W6 = "Indexed_W6";
    public const string Indexed_W7 = "Indexed_W7";
    public const string Indexed_W8 = "Indexed_W8";
    public const string Indexed_W9 = "Indexed_W9";
    public const string SlimeData_Blue = "SlimeData_Blue";
    public const string SlimeData_Boss1 = "SlimeData_Boss1";
    public const string SlimeData_Boss2 = "SlimeData_Boss2";
    public const string SlimeData_Orange = "SlimeData_Orange";
    public const string SlimeData_White = "SlimeData_White";
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
        { Auto_Orange, "Assets/Datas/Waves/Easy/Auto_Orange.asset" },
        { Auto_White, "Assets/Datas/Waves/Easy/Auto_White.asset" },
        { GameConfig, "Assets/Datas/GameConfig.asset" },
        { GridMapData1, "Assets/Datas/Grid/GridMapData1.asset" },
        { GridMapData1_Old, "Assets/Datas/Grid/GridMapData1_Old.asset" },
        { GridMapDataTemp, "Assets/Datas/Grid/GridMapDataTemp.asset" },
        { GridMapDataTest, "Assets/Datas/Grid/GridMapDataTest.asset" },
        { GridMapDataTest2, "Assets/Datas/Grid/GridMapDataTest2.asset" },
        { Indexed_W1, "Assets/Datas/Waves/Easy/Indexed_W1.asset" },
        { Indexed_W10, "Assets/Datas/Waves/Easy/Indexed_W10.asset" },
        { Indexed_W2, "Assets/Datas/Waves/Easy/Indexed_W2.asset" },
        { Indexed_W3, "Assets/Datas/Waves/Easy/Indexed_W3.asset" },
        { Indexed_W4, "Assets/Datas/Waves/Easy/Indexed_W4.asset" },
        { Indexed_W5, "Assets/Datas/Waves/Easy/Indexed_W5.asset" },
        { Indexed_W6, "Assets/Datas/Waves/Easy/Indexed_W6.asset" },
        { Indexed_W7, "Assets/Datas/Waves/Easy/Indexed_W7.asset" },
        { Indexed_W8, "Assets/Datas/Waves/Easy/Indexed_W8.asset" },
        { Indexed_W9, "Assets/Datas/Waves/Easy/Indexed_W9.asset" },
        { SlimeData_Blue, "Assets/Datas/Slimes/SlimeData_Blue.asset" },
        { SlimeData_Boss1, "Assets/Datas/Slimes/SlimeData_Boss1.asset" },
        { SlimeData_Boss2, "Assets/Datas/Slimes/SlimeData_Boss2.asset" },
        { SlimeData_Orange, "Assets/Datas/Slimes/SlimeData_Orange.asset" },
        { SlimeData_White, "Assets/Datas/Slimes/SlimeData_White.asset" },
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
