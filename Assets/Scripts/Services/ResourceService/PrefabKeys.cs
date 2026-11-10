using System.Collections.Generic;

public static class PrefabKeys
{
    public const string Archer = "Archer";
    public const string ArcherArrow = "ArcherArrow";
    public const string ArcherBow = "ArcherBow";
    public const string BushRound = "BushRound";
    public const string BushTall = "BushTall";
    public const string CriticalDamageText = "CriticalDamageText";
    public const string Crossbow = "Crossbow";
    public const string CrossbowBolt = "CrossbowBolt";
    public const string CrossbowBow = "CrossbowBow";
    public const string CubeGrass = "CubeGrass";
    public const string CubeGround = "CubeGround";
    public const string CubeSand = "CubeSand";
    public const string CurrencyGem = "CurrencyGem";
    public const string CurrencyGold = "CurrencyGold";
    public const string DamageText = "DamageText";
    public const string GameResultPopup = "GameResultPopup";
    public const string GoodSlime = "GoodSlime";
    public const string GoodSlimeBall = "GoodSlimeBall";
    public const string GradeBadge = "GradeBadge";
    public const string GrassPlant = "GrassPlant";
    public const string GrassTuft = "GrassTuft";
    public const string GroundDirtPath = "GroundDirtPath";
    public const string GroundGrassBorder = "GroundGrassBorder";
    public const string GroundGrassDark = "GroundGrassDark";
    public const string GroundGrassLight = "GroundGrassLight";
    public const string HunterCard = "HunterCard";
    public const string HunterCardGachaPopup = "HunterCardGachaPopup";
    public const string HunterCardRevealPopup = "HunterCardRevealPopup";
    public const string HunterDetailPopup = "HunterDetailPopup";
    public const string Knight = "Knight";
    public const string KnightShield = "KnightShield";
    public const string KnightSword = "KnightSword";
    public const string LeafPile01 = "LeafPile01";
    public const string LeafPile02 = "LeafPile02";
    public const string LeafPile03 = "LeafPile03";
    public const string LobbyMain = "LobbyMain";
    public const string LobbyProfile = "LobbyProfile";
    public const string LobbyTab = "LobbyTab";
    public const string Lumberjack = "Lumberjack";
    public const string LumberjackAxe = "LumberjackAxe";
    public const string Monolith = "Monolith";
    public const string Mushroom01 = "Mushroom01";
    public const string Mushroom02 = "Mushroom02";
    public const string Ranger = "Ranger";
    public const string RangerArrow = "RangerArrow";
    public const string RangerBow = "RangerBow";
    public const string RockCliff01 = "RockCliff01";
    public const string RockCliff02 = "RockCliff02";
    public const string RockSmall = "RockSmall";
    public const string SkillSlotItem = "SkillSlotItem";
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
    public const string SortButton = "SortButton";
    public const string StoneFlat01 = "StoneFlat01";
    public const string StoneFlat02 = "StoneFlat02";
    public const string StoneGate = "StoneGate";
    public const string StoneWall01 = "StoneWall01";
    public const string StoneWall02 = "StoneWall02";
    public const string StoneWall03 = "StoneWall03";
    public const string Sword = "Sword";
    public const string Swordsman = "Swordsman";
    public const string TempleBlock = "TempleBlock";
    public const string TempleBlockAlt = "TempleBlockAlt";
    public const string TempleBrick = "TempleBrick";
    public const string TempleDeco = "TempleDeco";
    public const string TempleFlowerRed = "TempleFlowerRed";
    public const string TempleFlowerYellow = "TempleFlowerYellow";
    public const string TempleGrass = "TempleGrass";
    public const string TempleLeaf = "TempleLeaf";
    public const string TemplePost = "TemplePost";
    public const string TempleRail = "TempleRail";
    public const string TempleStairs = "TempleStairs";
    public const string TreeGreen01 = "TreeGreen01";
    public const string TreeGreen02 = "TreeGreen02";
    public const string TreeOrange = "TreeOrange";
    public const string TreePine = "TreePine";
    public const string VfxSwordSlash = "VfxSwordSlash";
    public const string VfxSwordSlashBlue = "VfxSwordSlashBlue";
    public const string VfxSwordSlashCrush = "VfxSwordSlashCrush";
    public const string WaveForest = "WaveForest";

    public static readonly Dictionary<string, string> PrefabPaths = new Dictionary<string, string>()
    {
        { Archer, "Assets/Prefabs/Game/Characters/Archer.prefab" },
        { ArcherArrow, "Assets/Prefabs/Game/Characters/ArcherArrow.prefab" },
        { ArcherBow, "Assets/Prefabs/Game/Characters/ArcherBow.prefab" },
        { BushRound, "Assets/Prefabs/Game/Env/BushRound.prefab" },
        { BushTall, "Assets/Prefabs/Game/Env/BushTall.prefab" },
        { CriticalDamageText, "Assets/Prefabs/Game/UI/CriticalDamageText.prefab" },
        { Crossbow, "Assets/Prefabs/Game/Characters/Crossbow.prefab" },
        { CrossbowBolt, "Assets/Prefabs/Game/Characters/CrossbowBolt.prefab" },
        { CrossbowBow, "Assets/Prefabs/Game/Characters/CrossbowBow.prefab" },
        { CubeGrass, "Assets/Prefabs/Game/Ground/CubeGrass.prefab" },
        { CubeGround, "Assets/Prefabs/Game/Ground/CubeGround.prefab" },
        { CubeSand, "Assets/Prefabs/Game/Ground/CubeSand.prefab" },
        { CurrencyGem, "Assets/Prefabs/Common/CurrencyGem.prefab" },
        { CurrencyGold, "Assets/Prefabs/Common/CurrencyGold.prefab" },
        { DamageText, "Assets/Prefabs/Game/UI/DamageText.prefab" },
        { GameResultPopup, "Assets/Prefabs/Game/UI/Popup/GameResultPopup.prefab" },
        { GoodSlime, "Assets/Prefabs/Game/Characters/GoodSlime.prefab" },
        { GoodSlimeBall, "Assets/Prefabs/Game/Characters/GoodSlimeBall.prefab" },
        { GradeBadge, "Assets/Prefabs/Common/GradeBadge.prefab" },
        { GrassPlant, "Assets/Prefabs/Game/Env/GrassPlant.prefab" },
        { GrassTuft, "Assets/Prefabs/Game/Env/GrassTuft.prefab" },
        { GroundDirtPath, "Assets/Prefabs/Game/Ground/GroundDirtPath.prefab" },
        { GroundGrassBorder, "Assets/Prefabs/Game/Ground/GroundGrassBorder.prefab" },
        { GroundGrassDark, "Assets/Prefabs/Game/Ground/GroundGrassDark.prefab" },
        { GroundGrassLight, "Assets/Prefabs/Game/Ground/GroundGrassLight.prefab" },
        { HunterCard, "Assets/Prefabs/Lobby/LobbyMain/HunterCard.prefab" },
        { HunterCardGachaPopup, "Assets/Prefabs/Lobby/HunterCardGachaPopup.prefab" },
        { HunterCardRevealPopup, "Assets/Prefabs/Lobby/HunterCardRevealPopup.prefab" },
        { HunterDetailPopup, "Assets/Prefabs/Lobby/HunterDetailPopup.prefab" },
        { Knight, "Assets/Prefabs/Game/Characters/Knight.prefab" },
        { KnightShield, "Assets/Prefabs/Game/Characters/KnightShield.prefab" },
        { KnightSword, "Assets/Prefabs/Game/Characters/KnightSword.prefab" },
        { LeafPile01, "Assets/Prefabs/Game/Env/LeafPile01.prefab" },
        { LeafPile02, "Assets/Prefabs/Game/Env/LeafPile02.prefab" },
        { LeafPile03, "Assets/Prefabs/Game/Env/LeafPile03.prefab" },
        { LobbyMain, "Assets/Prefabs/Lobby/LobbyMain/LobbyMain.prefab" },
        { LobbyProfile, "Assets/Prefabs/Lobby/LobbyProfile.prefab" },
        { LobbyTab, "Assets/Prefabs/Lobby/LobbyMain/LobbyTab.prefab" },
        { Lumberjack, "Assets/Prefabs/Game/Characters/Lumberjack.prefab" },
        { LumberjackAxe, "Assets/Prefabs/Game/Characters/LumberjackAxe.prefab" },
        { Monolith, "Assets/Prefabs/Game/Characters/Monolith.prefab" },
        { Mushroom01, "Assets/Prefabs/Game/Env/Mushroom01.prefab" },
        { Mushroom02, "Assets/Prefabs/Game/Env/Mushroom02.prefab" },
        { Ranger, "Assets/Prefabs/Game/Characters/Ranger.prefab" },
        { RangerArrow, "Assets/Prefabs/Game/Characters/RangerArrow.prefab" },
        { RangerBow, "Assets/Prefabs/Game/Characters/RangerBow.prefab" },
        { RockCliff01, "Assets/Prefabs/Game/Env/RockCliff01.prefab" },
        { RockCliff02, "Assets/Prefabs/Game/Env/RockCliff02.prefab" },
        { RockSmall, "Assets/Prefabs/Game/Env/RockSmall.prefab" },
        { SkillSlotItem, "Assets/Prefabs/Common/SkillSlotItem.prefab" },
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
        { SortButton, "Assets/Prefabs/Lobby/LobbyMain/SortButton.prefab" },
        { StoneFlat01, "Assets/Prefabs/Game/Env/StoneFlat01.prefab" },
        { StoneFlat02, "Assets/Prefabs/Game/Env/StoneFlat02.prefab" },
        { StoneGate, "Assets/Prefabs/Game/Env/StoneGate.prefab" },
        { StoneWall01, "Assets/Prefabs/Game/Env/StoneWall01.prefab" },
        { StoneWall02, "Assets/Prefabs/Game/Env/StoneWall02.prefab" },
        { StoneWall03, "Assets/Prefabs/Game/Env/StoneWall03.prefab" },
        { Sword, "Assets/Prefabs/Game/Characters/Sword.prefab" },
        { Swordsman, "Assets/Prefabs/Game/Characters/Swordsman.prefab" },
        { TempleBlock, "Assets/Prefabs/Game/Temple/TempleBlock.prefab" },
        { TempleBlockAlt, "Assets/Prefabs/Game/Temple/TempleBlockAlt.prefab" },
        { TempleBrick, "Assets/Prefabs/Game/Temple/TempleBrick.prefab" },
        { TempleDeco, "Assets/Prefabs/Game/Temple/TempleDeco.prefab" },
        { TempleFlowerRed, "Assets/Prefabs/Game/Temple/TempleFlowerRed.prefab" },
        { TempleFlowerYellow, "Assets/Prefabs/Game/Temple/TempleFlowerYellow.prefab" },
        { TempleGrass, "Assets/Prefabs/Game/Temple/TempleGrass.prefab" },
        { TempleLeaf, "Assets/Prefabs/Game/Temple/TempleLeaf.prefab" },
        { TemplePost, "Assets/Prefabs/Game/Temple/TemplePost.prefab" },
        { TempleRail, "Assets/Prefabs/Game/Temple/TempleRail.prefab" },
        { TempleStairs, "Assets/Prefabs/Game/Temple/TempleStairs.prefab" },
        { TreeGreen01, "Assets/Prefabs/Game/Env/TreeGreen01.prefab" },
        { TreeGreen02, "Assets/Prefabs/Game/Env/TreeGreen02.prefab" },
        { TreeOrange, "Assets/Prefabs/Game/Env/TreeOrange.prefab" },
        { TreePine, "Assets/Prefabs/Game/Env/TreePine.prefab" },
        { VfxSwordSlash, "Assets/Prefabs/Game/Vfx/VfxSwordSlash.prefab" },
        { VfxSwordSlashBlue, "Assets/Prefabs/Game/Vfx/VfxSwordSlashBlue.prefab" },
        { VfxSwordSlashCrush, "Assets/Prefabs/Game/Vfx/VfxSwordSlashCrush.prefab" },
        { WaveForest, "Assets/Prefabs/Lobby/LobbyMain/WaveForest.prefab" },
    };

    public static string GetPrefabPath(string key)
    {
        return PrefabPaths.TryGetValue(key, out var path) ? path : string.Empty;
    }
}
