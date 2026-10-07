using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveZone
{
    [SerializeField] private SlimeData _slime;
    [SerializeField] private int _count = 10;
    [SerializeField] private BossSlimeData _boss;
    [SerializeField] private float _bossExtraStartDelay = 1f;

    public SlimeData Slime => _slime;
    public int Count => _count;
    public BossSlimeData Boss => _boss;
    public float BossExtraStartDelay => _bossExtraStartDelay;
}

[CreateAssetMenu(fileName = "WaveTable", menuName = "SlimeTD/Wave Table", order = 23)]
public class WaveTableData : ScriptableObject
{
    public const int WavesPerZone = 10;

    [Header("공통")]
    [SerializeField] private float _spawnInterval = 0.8f;
    [SerializeField] private float _interWaveDelay = 3f;

    [Header("성장")]
    [Tooltip("일반 슬라임에만 적용. 보스는 _baseHealth 고정")]
    [SerializeField] private float _normalHpGrowth = 0.06f;

    [Header("구간")]
    [SerializeField] private List<WaveZone> _zones = new();

    public float SpawnInterval => _spawnInterval;
    public float InterWaveDelay => _interWaveDelay;
    public float NormalHpGrowth => _normalHpGrowth;
    public IReadOnlyList<WaveZone> Zones => _zones;

    public int FinalWave => _zones == null ? 0 : _zones.Count * WavesPerZone;
    public int MaxWave => FinalWave;

    public static int ZoneIndexOf(int waveNumber) => (waveNumber - 1) / WavesPerZone;
    public static bool IsBossWave(int waveNumber) => waveNumber % WavesPerZone == 0;

    public WaveZone GetZone(int zoneIndex)
    {
        if (_zones == null || zoneIndex < 0 || zoneIndex >= _zones.Count)
            return null;

        return _zones[zoneIndex];
    }

    private void OnValidate()
    {
        if (_zones == null)
            return;

        for (int i = 0; i < _zones.Count; i++)
        {
            var zone = _zones[i];
            if (zone == null)
                continue;

            if (zone.Slime == null)
                Debug.Log($"[WaveTableData] zones[{i}]에 슬라임 데이터가 없습니다.", this);

            if (zone.Boss == null)
                Debug.Log($"[WaveTableData] zones[{i}]에 보스 데이터가 없습니다.", this);

            if (zone.Count < 1)
                Debug.Log($"[WaveTableData] zones[{i}] 마릿수가 1 미만입니다.", this);
        }
    }
}
