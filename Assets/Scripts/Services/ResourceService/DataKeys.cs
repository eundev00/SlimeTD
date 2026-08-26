using System.Collections.Generic;

public static class DataKeys
{
    public const string GameConfig = "GameConfig";
    public const string GridMapData1 = "GridMapData1";
    public const string GridMapDataTest = "GridMapDataTest";
    public const string SpawnEntry_01 = "SpawnEntry_01";
    public const string SpawnEntry_02 = "SpawnEntry_02";
    public const string SpawnEntry_03 = "SpawnEntry_03";
    public const string TowerSpawnConfig = "TowerSpawnConfig";
    public const string Wave01 = "Wave01";
    public const string Wave02 = "Wave02";
    public const string Wave03 = "Wave03";
    public const string WaveEasyTable = "WaveEasyTable";

    public static readonly Dictionary<string, string> DataPaths = new Dictionary<string, string>()
    {
        { GameConfig, "Assets/Datas/GameConfig.asset" },
        { GridMapData1, "Assets/Datas/Grid/GridMapData1.asset" },
        { GridMapDataTest, "Assets/Datas/Grid/GridMapDataTest.asset" },
        { SpawnEntry_01, "Assets/Datas/Waves/Easy/SpawnEntry_01.asset" },
        { SpawnEntry_02, "Assets/Datas/Waves/Easy/SpawnEntry_02.asset" },
        { SpawnEntry_03, "Assets/Datas/Waves/Easy/SpawnEntry_03.asset" },
        { TowerSpawnConfig, "Assets/Datas/TowerSpawnConfig.asset" },
        { Wave01, "Assets/Datas/Waves/Easy/Wave01.asset" },
        { Wave02, "Assets/Datas/Waves/Easy/Wave02.asset" },
        { Wave03, "Assets/Datas/Waves/Easy/Wave03.asset" },
        { WaveEasyTable, "Assets/Datas/Waves/Easy/WaveEasyTable.asset" },
    };

    public static string GetDataPath(string key)
    {
        return DataPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
