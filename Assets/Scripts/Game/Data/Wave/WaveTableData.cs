using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AutoWaveSetting
{
    [SerializeField] private int _baseCount = 12;
    [SerializeField] private float _countGrowth = 0.06f;
    [SerializeField] private int _maxCount = 40;
    [SerializeField] private SlimeData[] _slimes;

    public int BaseCount => _baseCount;
    public float CountGrowth => _countGrowth;
    public int MaxCount => _maxCount;
    public SlimeData[] Slimes => _slimes;
}

[Serializable]
public class BossSpawnEntry
{
    [SerializeField] private int _waveNumber = 1;
    [SerializeField] private BossSlimeData _bossData;
    [SerializeField] private float _extraStartDelay = 3f;

    public int WaveNumber => _waveNumber;
    public BossSlimeData BossData => _bossData;
    public float ExtraStartDelay => _extraStartDelay;
}

[CreateAssetMenu(fileName = "WaveTable", menuName = "SlimeTD/Wave Table", order = 23)]
public class WaveTableData : ScriptableObject
{
    [Header("공통")]
    [SerializeField] private float _spawnInterval = 0.8f;
    [SerializeField] private float _interWaveDelay = 3f;
    [SerializeField] private int _finalWave = 50;

    [Header("성장")]
    [SerializeField] private float _hpGrowth = 0.04f;

    [Header("보상")]
    [SerializeField] private float _goldRatio = 0.2f;
    [SerializeField] private int _waveClearGoldBase = 10;
    [SerializeField] private int _waveClearGoldStep = 1;

    [Header("웨이브")]
    [SerializeField] private FixedWaveData[] _fixedWaves;
    [SerializeField] private AutoWaveSetting _autoWaveSetting = new();
    [SerializeField] private List<BossSpawnEntry> _bossEntries = new();

    public float SpawnInterval => _spawnInterval;
    public float InterWaveDelay => _interWaveDelay;
    public int FinalWave => _finalWave;
    public float HpGrowth => _hpGrowth;
    public float GoldRatio => _goldRatio;
    public int WaveClearGoldBase => _waveClearGoldBase;
    public int WaveClearGoldStep => _waveClearGoldStep;
    public FixedWaveData[] FixedWaves => _fixedWaves;
    public AutoWaveSetting AutoWaveSetting => _autoWaveSetting;
    public IReadOnlyList<BossSpawnEntry> BossEntries => _bossEntries;

    public int MaxWave => _finalWave;

    public int WaveClearGold(int waveNumber) => _waveClearGoldBase + _waveClearGoldStep * waveNumber;

    private void OnValidate()
    {
        if (_bossEntries == null)
            return;

        for (int i = 0; i < _bossEntries.Count; i++)
        {
            var entry = _bossEntries[i];
            if (entry == null)
                continue;

            if (entry.BossData == null)
                Debug.Log($"[WaveTableData] bossEntries[{i}]에 보스 데이터가 없습니다.", this);

            if (entry.WaveNumber < 1 || entry.WaveNumber > _finalWave)
                Debug.Log($"[WaveTableData] bossEntries[{i}] 웨이브 {entry.WaveNumber}가 1~{_finalWave} 범위를 벗어납니다.", this);

            for (int j = i + 1; j < _bossEntries.Count; j++)
            {
                if (_bossEntries[j] != null && _bossEntries[j].WaveNumber == entry.WaveNumber)
                    Debug.Log($"[WaveTableData] 웨이브 {entry.WaveNumber}에 보스 엔트리가 중복됩니다.", this);
            }
        }
    }
}
