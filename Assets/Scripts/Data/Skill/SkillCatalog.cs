using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillCatalog", menuName = "SlimeTD/Skill Catalog", order = 65)]
public class SkillCatalog : ScriptableObject
{
    private static readonly IReadOnlyList<TowerSkillData> Empty = Array.Empty<TowerSkillData>();

    [SerializeField] private TowerSkillData[] _skills;

    private readonly Dictionary<int, List<TowerSkillData>> _byTower = new Dictionary<int, List<TowerSkillData>>();
    private bool _indexed;

    public IReadOnlyList<TowerSkillData> Skills => _skills ?? Array.Empty<TowerSkillData>();

    private void OnEnable()
    {
        _indexed = false;
    }

    // 해금 레벨 순, 같으면 스킬 ID 순이다. 해금 레벨에 따른 활성화는 호출한 쪽이 한다.
    public IReadOnlyList<TowerSkillData> GetSkills(int towerId)
    {
        EnsureIndex();
        return _byTower.TryGetValue(towerId, out var list) ? list : Empty;
    }

    private void EnsureIndex()
    {
        if (_indexed)
            return;

        _byTower.Clear();
        _indexed = true;

        if (_skills == null)
            return;

        for (int i = 0; i < _skills.Length; i++)
        {
            var skill = _skills[i];
            if (skill == null)
                continue;

            if (!_byTower.TryGetValue(skill.TowerId, out var list))
            {
                list = new List<TowerSkillData>();
                _byTower.Add(skill.TowerId, list);
            }

            list.Add(skill);
        }

        foreach (var list in _byTower.Values)
        {
            list.Sort((a, b) =>
            {
                int byLevel = a.UnlockLevel.CompareTo(b.UnlockLevel);
                return byLevel != 0 ? byLevel : a.Id.CompareTo(b.Id);
            });
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _indexed = false;
    }
#endif
}
