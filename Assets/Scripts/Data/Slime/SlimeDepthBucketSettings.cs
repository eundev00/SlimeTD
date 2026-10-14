using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SlimeDepthBucketSettings", menuName = "SlimeTD/Slime Depth Bucket Settings", order = 41)]
public class SlimeDepthBucketSettings : ScriptableObject
{
    [SerializeField] private bool _enabled = true;
    [SerializeField][Range(0f, 5f)] private float _bucketSpacing = 0.3f;
    [SerializeField][Range(2, 64)] private int _bucketCount = 16;

    public event Action Changed;

    public bool Enabled => _enabled;
    public float BucketSpacing => _bucketSpacing;
    public int BucketCount => _bucketCount;

    public float EffectiveSpacing => _enabled ? _bucketSpacing : 0f;

    private void OnValidate()
    {
        Changed?.Invoke();
    }
}
