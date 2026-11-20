using System;
using UnityEngine;

[Serializable]
public struct LevelUpCostRule
{
    [SerializeField] private int[] _cards;
    [SerializeField] private int[] _coin;

    // level은 올리기 전의 현재 레벨이다. [0]이 레벨 1 -> 2 비용이다.
    public int GetCards(int level)
    {
        return Get(_cards, level);
    }

    public int GetCoin(int level)
    {
        return Get(_coin, level);
    }

    private static int Get(int[] table, int level)
    {
        if (table == null || table.Length == 0)
            return 0;

        return table[Mathf.Clamp(level - 1, 0, table.Length - 1)];
    }
}
