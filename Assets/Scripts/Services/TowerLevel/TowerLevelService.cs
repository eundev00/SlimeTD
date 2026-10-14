using System.Collections.Generic;
using UnityEngine;

public class TowerLevelService : ITowerLevelService
{
    public const int MinLevel = 1;
    public const int MaxLevel = 15;

    private readonly Dictionary<int, int> _levels = new Dictionary<int, int>();
    private int _defaultLevel = MinLevel;

    public int GetLevel(int towerId)
    {
        return _levels.TryGetValue(towerId, out int level) ? level : _defaultLevel;
    }

    public void SetLevel(int towerId, int level)
    {
        _levels[towerId] = Mathf.Clamp(level, MinLevel, MaxLevel);
    }

    public void SetDefaultLevel(int level)
    {
        _defaultLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
    }
}