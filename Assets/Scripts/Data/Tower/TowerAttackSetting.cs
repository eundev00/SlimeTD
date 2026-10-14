using System;
using UnityEngine;

[Serializable]
public class TowerAttackSetting
{
    [SerializeField] private TowerAttackType _type = TowerAttackType.Projectile;
    [SerializeField] private GameObject _projectilePrefab;

    public TowerAttackType Type => _type;
    public GameObject ProjectilePrefab => _projectilePrefab;
}
