using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TowerSkillData
{
    [SerializeField] private int _id;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _towerId;
    [SerializeField] private int _unlockLevel = 1;
    [SerializeField] private int _upgradeOfId;

    [Header("발동 조건")]
    [SerializeField] private SkillTrigger _trigger;
    [SerializeField] private int _nth;
    [SerializeField] private float _chance;
    [SerializeField] private float _cooldown;

    [Header("효과")]
    [SerializeField] private List<SkillEffectEntry> _effects = new List<SkillEffectEntry>();

    public int Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int TowerId => _towerId;
    public int UnlockLevel => _unlockLevel;
    public int UpgradeOfId => _upgradeOfId;
    public bool IsUpgrade => _upgradeOfId != 0;
    public SkillTrigger Trigger => _trigger;
    public int Nth => _nth;
    public float Chance => _chance;
    public float Cooldown => _cooldown;
    public IReadOnlyList<SkillEffectEntry> Effects => _effects;
}
