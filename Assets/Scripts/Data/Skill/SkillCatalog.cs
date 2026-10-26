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

    // 레벨에 해금되었고, 해금된 업그레이드 스킬에 대체되지 않은 스킬만 buffer에 담는다.
    public void GetActiveSkills(int towerId, int level, List<TowerSkillData> buffer)
    {
        buffer.Clear();

        var all = GetSkills(towerId);
        for (int i = 0; i < all.Count; i++)
        {
            var skill = all[i];
            if (skill.UnlockLevel <= level && !IsReplaced(all, skill, level))
                buffer.Add(skill);
        }
    }

    // 계열(upgradeOf로 이어진 스킬들) 하나가 UI 슬롯 하나다. 계열의 대표는 대체 대상이 없는 스킬이다.
    public void GetSlots(int towerId, int level, List<SkillSlot> buffer)
    {
        buffer.Clear();

        var all = GetSkills(towerId);
        var chain = new List<TowerSkillData>();

        for (int i = 0; i < all.Count; i++)
        {
            var root = all[i];
            if (!IsRoot(all, root))
                continue;

            BuildChain(all, root, chain);

            TowerSkillData current = null;
            TowerSkillData next = null;

            for (int c = 0; c < chain.Count; c++)
            {
                if (chain[c].UnlockLevel <= level)
                    current = chain[c];
                else if (next == null)
                    next = chain[c];
            }

            buffer.Add(new SkillSlot(root, current, next));
        }
    }

    // 대체 대상이 목록에 없는 스킬도 대표로 본다. 그래야 잘못된 데이터가 슬롯을 숨기지 않는다.
    private static bool IsRoot(IReadOnlyList<TowerSkillData> all, TowerSkillData skill)
    {
        if (!skill.IsUpgrade)
            return true;

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Id == skill.UpgradeOfId)
                return false;
        }

        return true;
    }

    private static void BuildChain(IReadOnlyList<TowerSkillData> all, TowerSkillData root, List<TowerSkillData> chain)
    {
        chain.Clear();
        chain.Add(root);

        var tail = root;
        while (true)
        {
            TowerSkillData successor = null;
            for (int i = 0; i < all.Count; i++)
            {
                var candidate = all[i];
                if (candidate.UpgradeOfId == tail.Id && !chain.Contains(candidate))
                {
                    successor = candidate;
                    break;
                }
            }

            if (successor == null)
                return;

            chain.Add(successor);
            tail = successor;
        }
    }

    // 업그레이드가 여러 단계로 이어져도 가장 높게 해금된 단계만 남는다.
    private static bool IsReplaced(IReadOnlyList<TowerSkillData> all, TowerSkillData skill, int level)
    {
        for (int i = 0; i < all.Count; i++)
        {
            var other = all[i];
            if (other.UpgradeOfId == skill.Id && other.UnlockLevel <= level)
                return true;
        }

        return false;
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
