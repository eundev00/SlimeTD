using System.Collections.Generic;

public static class DataKeys
{
    public const string Boss_01 = "Boss_01";
    public const string Boss_02 = "Boss_02";
    public const string Boss_03 = "Boss_03";
    public const string Boss_04 = "Boss_04";
    public const string Boss_05 = "Boss_05";
    public const string GameConfig = "GameConfig";
    public const string GradeStyle = "GradeStyle";
    public const string GridMapData1 = "GridMapData1";
    public const string GridMapData2 = "GridMapData2";
    public const string SkillCatalog = "SkillCatalog";
    public const string SlimeDepthBucketSettings = "SlimeDepthBucketSettings";
    public const string Slime_01 = "Slime_01";
    public const string Slime_02 = "Slime_02";
    public const string Slime_03 = "Slime_03";
    public const string Slime_04 = "Slime_04";
    public const string Slime_05 = "Slime_05";
    public const string TowerCatalog = "TowerCatalog";
    public const string TowerData_Archer = "TowerData_Archer";
    public const string TowerData_Crossbow = "TowerData_Crossbow";
    public const string TowerData_GoodSlime = "TowerData_GoodSlime";
    public const string TowerData_Knight = "TowerData_Knight";
    public const string TowerData_Lumberjack = "TowerData_Lumberjack";
    public const string TowerData_Monolith = "TowerData_Monolith";
    public const string TowerData_Ranger = "TowerData_Ranger";
    public const string TowerData_Swordsman = "TowerData_Swordsman";
    public const string WaveEasyTable = "WaveEasyTable";

    public static readonly Dictionary<string, string> DataPaths = new Dictionary<string, string>()
    {
        { Boss_01, "Assets/Datas/Slimes/Boss_01.asset" },
        { Boss_02, "Assets/Datas/Slimes/Boss_02.asset" },
        { Boss_03, "Assets/Datas/Slimes/Boss_03.asset" },
        { Boss_04, "Assets/Datas/Slimes/Boss_04.asset" },
        { Boss_05, "Assets/Datas/Slimes/Boss_05.asset" },
        { GameConfig, "Assets/Datas/GameConfig.asset" },
        { GradeStyle, "Assets/Datas/GradeStyle.asset" },
        { GridMapData1, "Assets/Datas/Grid/GridMapData1.asset" },
        { GridMapData2, "Assets/Datas/Grid/GridMapData2.asset" },
        { SkillCatalog, "Assets/Datas/SkillCatalog.asset" },
        { SlimeDepthBucketSettings, "Assets/Datas/SlimeDepthBucketSettings.asset" },
        { Slime_01, "Assets/Datas/Slimes/Slime_01.asset" },
        { Slime_02, "Assets/Datas/Slimes/Slime_02.asset" },
        { Slime_03, "Assets/Datas/Slimes/Slime_03.asset" },
        { Slime_04, "Assets/Datas/Slimes/Slime_04.asset" },
        { Slime_05, "Assets/Datas/Slimes/Slime_05.asset" },
        { TowerCatalog, "Assets/Datas/TowerCatalog.asset" },
        { TowerData_Archer, "Assets/Datas/Characters/Archer/TowerData_Archer.asset" },
        { TowerData_Crossbow, "Assets/Datas/Characters/Crossbow/TowerData_Crossbow.asset" },
        { TowerData_GoodSlime, "Assets/Datas/Characters/GoodSlime/TowerData_GoodSlime.asset" },
        { TowerData_Knight, "Assets/Datas/Characters/Knight/TowerData_Knight.asset" },
        { TowerData_Lumberjack, "Assets/Datas/Characters/Lumberjack/TowerData_Lumberjack.asset" },
        { TowerData_Monolith, "Assets/Datas/Characters/Monolith/TowerData_Monolith.asset" },
        { TowerData_Ranger, "Assets/Datas/Characters/Ranger/TowerData_Ranger.asset" },
        { TowerData_Swordsman, "Assets/Datas/Characters/Swordsman/TowerData_Swordsman.asset" },
        { WaveEasyTable, "Assets/Datas/Waves/Easy/WaveEasyTable.asset" },
    };

    public static string GetDataPath(string key)
    {
        return DataPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
