using System;
using UnityEngine;

[Serializable]
public struct LevelUpCostRule
{
    [SerializeField] private int _cardBase;
    [SerializeField] private int _cardPerLevel;
    [SerializeField] private int _goldBase;
    [SerializeField] private int _goldPerLevel;

    // level은 올리기 전의 현재 레벨이다. 레벨 1 -> 2가 기본값이고 레벨마다 증가분이 붙는다.
    public int GetCards(int level)
    {
        return _cardBase + _cardPerLevel * Mathf.Max(0, level - 1);
    }

    public int GetGold(int level)
    {
        return _goldBase + _goldPerLevel * Mathf.Max(0, level - 1);
    }
}