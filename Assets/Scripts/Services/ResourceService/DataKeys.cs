using System.Collections.Generic;

public static class DataKeys
{
    public const string Auto_Orange = "Auto_Orange";
    public const string Auto_White = "Auto_White";
    public const string GameConfig = "GameConfig";
    public const string GridMapData1 = "GridMapData1";
    public const string GridMapDataTest = "GridMapDataTest";
    public const string Indexed_Boss_W10 = "Indexed_Boss_W10";
    public const string Indexed_Boss_W20 = "Indexed_Boss_W20";
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
    public const string TowerSpawnConfig = "TowerSpawnConfig";
    public const string WaveEasyTable = "WaveEasyTable";

    public static readonly Dictionary<string, string> DataPaths = new Dictionary<string, string>()
    {
        { Auto_Orange, "Assets/Datas/Waves/Easy/Auto_Orange.asset" },
        { Auto_White, "Assets/Datas/Waves/Easy/Auto_White.asset" },
        { GameConfig, "Assets/Datas/GameConfig.asset" },
        { GridMapData1, "Assets/Datas/Grid/GridMapData1.asset" },
        { GridMapDataTest, "Assets/Datas/Grid/GridMapDataTest.asset" },
        { Indexed_Boss_W10, "Assets/Datas/Waves/Easy/Indexed_Boss_W10.asset" },
        { Indexed_Boss_W20, "Assets/Datas/Waves/Easy/Indexed_Boss_W20.asset" },
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
        { TowerSpawnConfig, "Assets/Datas/TowerSpawnConfig.asset" },
        { WaveEasyTable, "Assets/Datas/Waves/Easy/WaveEasyTable.asset" },
    };

    public static string GetDataPath(string key)
    {
        return DataPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
