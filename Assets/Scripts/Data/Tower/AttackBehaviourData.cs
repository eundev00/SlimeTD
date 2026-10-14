using UnityEngine;

public abstract class AttackBehaviourData : ScriptableObject
{
    [SerializeField] private string[] _attackStates;

    // 여러 타워가 같은 에셋을 공유하므로 순환 인덱스는 여기가 아니라 부품 인스턴스가 갖는다.
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

    public abstract IAttackBehaviour CreateBehaviour();
}
