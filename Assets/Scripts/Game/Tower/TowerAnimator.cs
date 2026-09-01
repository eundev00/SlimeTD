using System.Collections.Generic;
using UnityEngine;

public class TowerAnimator : MonoBehaviour
{
    private readonly Dictionary<string, int> _stateHashes = new Dictionary<string, int>();

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

    public void PlayIdle()
    {
        Play("Idle");
    }

    public void PlaySpawn()
    {
        Play("Spawn");
    }
}
