using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HunterCardGachaConfig", menuName = "SlimeTD/Hunter Card Gacha Config", order = 66)]
public class HunterCardGachaConfig : ScriptableObject
{
    // 확률과 환급은 서버가 굴린다. 팩은 화면 표시용 사본이다 (가격 = 팩 티켓)
    [SerializeField] private HunterCardGachaPack[] _packs;

    public IReadOnlyList<HunterCardGachaPack> Packs => _packs ?? Array.Empty<HunterCardGachaPack>();

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
