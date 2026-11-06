using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HunterCardGachaConfig", menuName = "SlimeTD/Hunter Card Gacha Config", order = 66)]
public class HunterCardGachaConfig : ScriptableObject
{
    [SerializeField] private HunterCardGachaPack[] _packs;
    [SerializeField] private HunterCardGachaGradeWeight[] _gradeWeights;
    [SerializeField] private float _lockedCardChance;
    [SerializeField] private int _pityCount;
    [SerializeField] private int _pityGradeId;
    [SerializeField] private int _maxLevelRefundGold;

    public IReadOnlyList<HunterCardGachaPack> Packs => _packs ?? Array.Empty<HunterCardGachaPack>();
    public IReadOnlyList<HunterCardGachaGradeWeight> GradeWeights => _gradeWeights ?? Array.Empty<HunterCardGachaGradeWeight>();
    public float LockedCardChance => _lockedCardChance;
    public int PityCount => _pityCount;
    public int PityGradeId => _pityGradeId;
    public int MaxLevelRefundGold => _maxLevelRefundGold;

    public bool TryGetPack(int packId, out HunterCardGachaPack pack)
    {
        var packs = Packs;
        for (int i = 0; i < packs.Count; i++)
        {
            if (packs[i].Id == packId)
            {
                pack = packs[i];
                return true;
            }
        }

        pack = default;
        return false;
    }
}

[Serializable]
public struct HunterCardGachaPack
{
    [SerializeField] private int _id;
    [SerializeField] private int _cardCount;
    [SerializeField] private int _price;
    [SerializeField] private int _guaranteeGradeId;

    public int Id => _id;
    public int CardCount => _cardCount;
    public int Price => _price;
    public int GuaranteeGradeId => _guaranteeGradeId;
    public bool HasGuarantee => _guaranteeGradeId > 0;
}

[Serializable]
public struct HunterCardGachaGradeWeight
{
    [SerializeField] private int _gradeId;
    [SerializeField] private float _weight;

    public int GradeId => _gradeId;
    public float Weight => _weight;
}
