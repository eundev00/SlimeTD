// TODO: 임시 이넘. SlimeTD-Data/Sheets/Enums.csv를 옮겨 적었고, 서버 프로토콜 코드가 생기면 그쪽 이넘으로 바꾼다

public enum GameMode
{
    None = 0,
    Single = 1,
    Multi = 2,
}

public enum Difficulty
{
    None = 0,
    Normal = 1,
    Hard = 2,
    Hell = 3,
}

public enum ItemType
{
    None = 0,
    Currency = 1,
    HunterCardPack = 2,
}

public enum ItemScope
{
    None = 0,
    Account = 1,
    Battle = 2,
}

public enum OpenMode
{
    None = 0,
    OnAcquire = 1,
}

public enum SlimeKind
{
    None = 0,
    Normal = 1,
    Boss = 2,
}

public enum SlimeRenderGroup
{
    Normal = 0,
    Boss = 1,
}

public enum SkillTrigger
{
    Always = 0,
    OnAttack = 1,
    OnHit = 2,
    OnKill = 3,
    EveryNthAttack = 4,
}

public enum SkillEffectType
{
    CritChance = 0,
    CritBonus = 1,
    AttackSpeed = 2,
    Attack = 3,
    MultiShot = 4,
    Pierce = 5,
    Slow = 6,
    JellyGain = 7,
    DamageMultiplier = 8,
    Splash = 9,
    Stun = 10,
}

public enum VfxAnchor
{
    Root = 0,
    Center = 1,
    Head = 2,
    Front = 3,
}

public enum TowerAttackType
{
    Melee = 0,
    Projectile = 1,
    Beam = 2,
}
