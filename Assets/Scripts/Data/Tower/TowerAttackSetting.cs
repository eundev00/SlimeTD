using System;
using UnityEngine;

[Serializable]
public class TowerAttackSetting
{
    [SerializeField] private TowerAttackType _type = TowerAttackType.Projectile;
    [SerializeField] private string[] _attackStates;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int _poolCapacity = 10;
    [SerializeField] private int _poolMaxSize = 50;

    public TowerAttackType Type => _type;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public int PoolCapacity => _poolCapacity;
    public int PoolMaxSize => _poolMaxSize;

    // 여러 타워가 같은 설정을 공유하지 않으므로 순환 인덱스는 공격 행동 인스턴스가 갖는다.
    public string GetAttackState(int index)
    {
        if (_attackStates == null || _attackStates.Length == 0)
            return string.Empty;

        return _attackStates[index % _attackStates.Length];
    }

    public int GetNextAttackStateIndex(int index)
    {
        if (_attackStates == null || _attackStates.Length == 0)
            return 0;

        return (index + 1) % _attackStates.Length;
    }
}
