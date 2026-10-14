using System;
using UnityEngine;

[Serializable]
public class TowerAttackSetting
{
    [SerializeField] private TowerAttackType _type = TowerAttackType.Projectile;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int _poolCapacity = 10;
    [SerializeField] private int _poolMaxSize = 50;

    public TowerAttackType Type => _type;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public int PoolCapacity => _poolCapacity;
    public int PoolMaxSize => _poolMaxSize;
}
