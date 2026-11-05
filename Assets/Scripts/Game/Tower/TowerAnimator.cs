using System.Collections.Generic;
using UnityEngine;

public class TowerAnimator : MonoBehaviour
{
    private static readonly int AttackSpeedParameter = Animator.StringToHash("AttackSpeed");

    private readonly Dictionary<string, int> _stateHashes = new Dictionary<string, int>();
    private HashSet<string> _parameterNames;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            Debug.Log("[TowerAnimator] Animator 컴포넌트가 없습니다.", this);
        }
    }

    public void Play(string state)
    {
        if (_animator == null || string.IsNullOrEmpty(state))
            return;

        if (!_stateHashes.TryGetValue(state, out var hash))
        {
            hash = Animator.StringToHash(state);
            _stateHashes.Add(state, hash);
        }

        _animator.Play(hash);
    }

    public void SetTrigger(string trigger)
    {
        if (_animator == null || string.IsNullOrEmpty(trigger))
            return;

        if (!_stateHashes.TryGetValue(trigger, out var hash))
        {
            hash = Animator.StringToHash(trigger);
            _stateHashes.Add(trigger, hash);
        }

        _animator.SetTrigger(hash);
    }

    public bool HasParameter(string name)
    {
        if (_animator == null || string.IsNullOrEmpty(name))
            return false;

        if (_parameterNames == null)
        {
            _parameterNames = new HashSet<string>();
            foreach (var parameter in _animator.parameters)
                _parameterNames.Add(parameter.name);
        }

        return _parameterNames.Contains(name);
    }

    public void SetAttackSpeed(float multiplier)
    {
        if (_animator == null)
            return;

        _animator.SetFloat(AttackSpeedParameter, multiplier);
    }

    public void ResetTrigger(string trigger)
    {
        if (_animator == null || string.IsNullOrEmpty(trigger))
            return;

        if (_stateHashes.TryGetValue(trigger, out var hash))
            _animator.ResetTrigger(hash);
    }

    public void PlayIdle()
    {
        Play("Idle");
    }

    public void PlaySpawn()
    {
        Play("Spawn");
    }
}
