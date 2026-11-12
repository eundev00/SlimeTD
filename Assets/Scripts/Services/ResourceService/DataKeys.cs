using System.Collections.Generic;

public static class DataKeys
{
    public const string GameConfig = "GameConfig";
    public const string GradeStyle = "GradeStyle";
    public const string GridMapData1 = "GridMapData1";
    public const string GridMapData2 = "GridMapData2";
    public const string HunterCardGachaConfig = "HunterCardGachaConfig";
    public const string ServerConfig = "ServerConfig";
    public const string SkillCatalog = "SkillCatalog";
    public const string SlimeData_Boss_01 = "SlimeData_Boss_01";
    public const string SlimeData_Boss_02 = "SlimeData_Boss_02";
    public const string SlimeData_Boss_03 = "SlimeData_Boss_03";
    public const string SlimeData_Boss_04 = "SlimeData_Boss_04";
    public const string SlimeData_Boss_05 = "SlimeData_Boss_05";
    public const string SlimeData_Slime_01 = "SlimeData_Slime_01";
    public const string SlimeData_Slime_02 = "SlimeData_Slime_02";
    public const string SlimeData_Slime_03 = "SlimeData_Slime_03";
    public const string SlimeData_Slime_04 = "SlimeData_Slime_04";
    public const string SlimeData_Slime_05 = "SlimeData_Slime_05";
    public const string SlimeDepthBucketSettings = "SlimeDepthBucketSettings";
    public const string Stage_SingleNormal = "Stage_SingleNormal";
    public const string TowerCatalog = "TowerCatalog";
    public const string TowerData_Archer = "TowerData_Archer";
    public const string TowerData_Crossbow = "TowerData_Crossbow";
    public const string TowerData_GoodSlime = "TowerData_GoodSlime";
    public const string TowerData_Knight = "TowerData_Knight";
    public const string TowerData_Lumberjack = "TowerData_Lumberjack";
    public const string TowerData_Monolith = "TowerData_Monolith";
    public const string TowerData_Ranger = "TowerData_Ranger";
    public const string TowerData_Swordsman = "TowerData_Swordsman";

    public static readonly Dictionary<string, string> DataPaths = new Dictionary<string, string>()
    {
        { GameConfig, "Assets/Datas/GameConfig.asset" },
        { GradeStyle, "Assets/Datas/GradeStyle.asset" },
        { GridMapData1, "Assets/Datas/Grid/GridMapData1.asset" },
        { GridMapData2, "Assets/Datas/Grid/GridMapData2.asset" },
        { HunterCardGachaConfig, "Assets/Datas/HunterCardGachaConfig.asset" },
        { ServerConfig, "Assets/Datas/ServerConfig.asset" },
        { SkillCatalog, "Assets/Datas/SkillCatalog.asset" },
        { SlimeData_Boss_01, "Assets/Datas/Slimes/SlimeData_Boss_01.asset" },
        { SlimeData_Boss_02, "Assets/Datas/Slimes/SlimeData_Boss_02.asset" },
        { SlimeData_Boss_03, "Assets/Datas/Slimes/SlimeData_Boss_03.asset" },
        { SlimeData_Boss_04, "Assets/Datas/Slimes/SlimeData_Boss_04.asset" },
        { SlimeData_Boss_05, "Assets/Datas/Slimes/SlimeData_Boss_05.asset" },
        { SlimeData_Slime_01, "Assets/Datas/Slimes/SlimeData_Slime_01.asset" },
        { SlimeData_Slime_02, "Assets/Datas/Slimes/SlimeData_Slime_02.asset" },
        { SlimeData_Slime_03, "Assets/Datas/Slimes/SlimeData_Slime_03.asset" },
        { SlimeData_Slime_04, "Assets/Datas/Slimes/SlimeData_Slime_04.asset" },
        { SlimeData_Slime_05, "Assets/Datas/Slimes/SlimeData_Slime_05.asset" },
        { SlimeDepthBucketSettings, "Assets/Datas/SlimeDepthBucketSettings.asset" },
        { Stage_SingleNormal, "Assets/Datas/Stages/Stage_SingleNormal.asset" },
        { TowerCatalog, "Assets/Datas/TowerCatalog.asset" },
        { TowerData_Archer, "Assets/Datas/Characters/Archer/TowerData_Archer.asset" },
        { TowerData_Crossbow, "Assets/Datas/Characters/Crossbow/TowerData_Crossbow.asset" },
        { TowerData_GoodSlime, "Assets/Datas/Characters/GoodSlime/TowerData_GoodSlime.asset" },
        { TowerData_Knight, "Assets/Datas/Characters/Knight/TowerData_Knight.asset" },
        { TowerData_Lumberjack, "Assets/Datas/Characters/Lumberjack/TowerData_Lumberjack.asset" },
        { TowerData_Monolith, "Assets/Datas/Characters/Monolith/TowerData_Monolith.asset" },
        { TowerData_Ranger, "Assets/Datas/Characters/Ranger/TowerData_Ranger.asset" },
        { TowerData_Swordsman, "Assets/Datas/Characters/Swordsman/TowerData_Swordsman.asset" },
    };

    public static string GetDataPath(string key)
    {
        return DataPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
