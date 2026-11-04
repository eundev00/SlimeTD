using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GachaConfig", menuName = "SlimeTD/Gacha Config", order = 66)]
public class GachaConfig : ScriptableObject
{
    [SerializeField] private GachaPackData[] _packs;
    [SerializeField] private GachaGradeWeight[] _gradeWeights;
    [SerializeField] private float _lockedCardChance;
    [SerializeField] private int _pityCount;
    [SerializeField] private int _pityGradeId;
    [SerializeField] private int _maxLevelRefundGold;

    public IReadOnlyList<GachaPackData> Packs => _packs ?? Array.Empty<GachaPackData>();
    public IReadOnlyList<GachaGradeWeight> GradeWeights => _gradeWeights ?? Array.Empty<GachaGradeWeight>();
    public float LockedCardChance => _lockedCardChance;
    public int PityCount => _pityCount;
    public int PityGradeId => _pityGradeId;
    public int MaxLevelRefundGold => _maxLevelRefundGold;

    public bool TryGetPack(int packId, out GachaPackData pack)
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
public struct GachaPackData
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
public struct GachaGradeWeight
{
    [SerializeField] private int _gradeId;
    [SerializeField] private float _weight;

    public int GradeId => _gradeId;
    public float Weight => _weight;
}
