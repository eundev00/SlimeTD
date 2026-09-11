using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerTierTable", menuName = "SlimeTD/Tower Tier Table", order = 62)]
public class TowerTierTable : ScriptableObject
{
    [SerializeField] private TowerTierGroup[] _groups;

    private readonly List<TowerData> _drawBuffer = new List<TowerData>();

    public bool TryGetGroup(int tier, out TowerTierGroup group)
    {
        group = null;

        if (_groups == null)
            return false;

        for (int i = 0; i < _groups.Length; i++)
        {
            if (_groups[i] == null || _groups[i].Tier != tier)
                continue;

            group = _groups[i];
            return true;
        }

        return false;
    }

    public bool TryGetTier(TowerData towerData, out int tier)
    {
        tier = 0;

        if (towerData == null || _groups == null)
            return false;

        for (int i = 0; i < _groups.Length; i++)
        {
            var towers = _groups[i] != null ? _groups[i].Towers : null;
            if (towers == null)
                continue;

            for (int j = 0; j < towers.Length; j++)
            {
                if (towers[j] != towerData)
                    continue;

                tier = _groups[i].Tier;
                return true;
            }
        }

        return false;
    }

    public bool TryDraw(int tier, out TowerData towerData)
    {
        towerData = null;

        if (!TryGetGroup(tier, out var group))
            return false;

        var towers = group.Towers;
        if (towers == null)
            return false;

        _drawBuffer.Clear();

        for (int i = 0; i < towers.Length; i++)
        {
            if (towers[i] == null || towers[i].Prefab == null)
                continue;

            _drawBuffer.Add(towers[i]);
        }

        if (_drawBuffer.Count == 0)
            return false;

        towerData = _drawBuffer[Random.Range(0, _drawBuffer.Count)];
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_groups == null)
            return;

        for (int i = 0; i < _groups.Length; i++)
        {
            if (_groups[i] == null)
            {
                Debug.Log($"[TowerTierTable] {i}번 칸이 비어 있습니다.", this);
                continue;
            }

            for (int j = i + 1; j < _groups.Length; j++)
            {
                if (_groups[j] == null)
                    continue;

                if (_groups[j].Tier == _groups[i].Tier)
                    Debug.Log($"[TowerTierTable] 티어 {_groups[i].Tier} 그룹이 중복되어 있습니다.", this);

                WarnDuplicatedTowers(_groups[i], _groups[j]);
            }
        }
    }

    private void WarnDuplicatedTowers(TowerTierGroup left, TowerTierGroup right)
    {
        var leftTowers = left.Towers;
        var rightTowers = right.Towers;

        if (leftTowers == null || rightTowers == null)
            return;

        for (int i = 0; i < leftTowers.Length; i++)
        {
            if (leftTowers[i] == null)
                continue;

            for (int j = 0; j < rightTowers.Length; j++)
            {
                if (leftTowers[i] == rightTowers[j])
                    Debug.Log($"[TowerTierTable] {leftTowers[i].name}이(가) 티어 {left.Tier}와 {right.Tier}에 모두 있습니다.", this);
            }
        }
    }
#endif
}
