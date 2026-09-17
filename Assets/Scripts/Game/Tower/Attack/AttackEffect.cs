using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AttackEffect : MonoBehaviour
{
    [NotNull][SerializeField] private ParticleSystem _effect;

    private void Awake()
    {
        Stop();
    }

    public async void Play()
    {
        if (_effect == null)
            return;

        _effect.Clear();
        _effect.Play();
    }

    public void Stop()
    {
        if (_effect != null)
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
