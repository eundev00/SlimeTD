using System;
using UnityEngine;

public class SlimeDepthBucketBinder : IDisposable
{
    private static readonly int BucketSpacingId = Shader.PropertyToID("_SlimeBucketSpacing");

    private SlimeDepthBucketSettings _settings;

    public void Bind(SlimeDepthBucketSettings settings)
    {
        Dispose();

        _settings = settings;
        if (_settings == null)
        {
            Debug.Log("[SlimeDepthBucketBinder] 설정이 없어 깊이 오프셋을 비활성화합니다.");
            Shader.SetGlobalFloat(BucketSpacingId, 0f);
            return;
        }

        _settings.Changed += Apply;
        Apply();
    }

    public void Dispose()
    {
        if (_settings == null)
            return;

        _settings.Changed -= Apply;
        _settings = null;
    }

    private void Apply()
    {
        if (_settings == null)
            return;

        Shader.SetGlobalFloat(BucketSpacingId, _settings.EffectiveSpacing);
    }
}
