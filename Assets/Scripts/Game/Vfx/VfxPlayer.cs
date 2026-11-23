using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Services.PoolService;
using UnityEngine;

// 대상(슬라임 등)이 풀에 먼저 반환돼도 끝까지 재생되도록, 연출을 대상과 떼어 따로 풀에서 꺼내 재생한다
public class VfxPlayer : IVfxPlayer, IDisposable
{
    private const float FallbackLifetime = 3f;

    private readonly IGameObjectPoolService _poolService;
    private readonly Dictionary<GameObject, float> _lifetimes = new Dictionary<GameObject, float>();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public VfxPlayer(IGameObjectPoolService poolService)
    {
        _poolService = poolService;
    }

    public void Play(GameObject prefab, Vector3 position, float scale = 1f)
    {
        if (prefab == null)
            return;

        if (!_lifetimes.TryGetValue(prefab, out float lifetime))
        {
            _poolService.CreatePool(prefab, 8, 64);
            lifetime = MeasureLifetime(prefab);
            _lifetimes[prefab] = lifetime;
        }

        var instance = _poolService.Get(prefab);
        if (instance == null)
            return;

        instance.transform.position = position;
        instance.transform.localScale = Vector3.one * scale;
        foreach (var effect in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            effect.Clear(false);
            effect.Play(false);
        }

        ReleaseAfterAsync(instance, lifetime).Forget();
    }

    private async UniTaskVoid ReleaseAfterAsync(GameObject instance, float lifetime)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _poolService.Release(instance);
    }

    // 반복 재생 파티클은 끝을 알 수 없어 기본 시간 뒤에 거둔다
    private static float MeasureLifetime(GameObject prefab)
    {
        float lifetime = 0f;
        foreach (var effect in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = effect.main;
            if (main.loop)
                return FallbackLifetime;

            lifetime = Mathf.Max(lifetime, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
        }

        return lifetime > 0f ? lifetime : FallbackLifetime;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}