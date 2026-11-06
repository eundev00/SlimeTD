using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int Gold;
    public int Gem;
    public int BestWave;
    public int HunterCardGachaPity;
    public List<HunterRecord> Hunters = new List<HunterRecord>();
}

[Serializable]
public class HunterRecord
{
    public int TowerId;
    public int Level = 1;
    public bool Unlocked;
    public int Cards;
}
