using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageWaveGroup
{
    [SerializeField] private SlimeDataBase _slime;
    [SerializeField] private int _count = 1;
    [SerializeField] private float _delay;

    public SlimeDataBase Slime => _slime;
    public int Count => _count;
    public float Delay => _delay;
}

[Serializable]
public class StageWave
{
    [SerializeField] private List<StageWaveGroup> _groups = new();

    public IReadOnlyList<StageWaveGroup> Groups => _groups;
}

// TODO: 서버 연결 전까지 쓰는 임시 데이터. 원본은 SlimeTD-Data의 Stages.csv와 Waves/<codeName>.csv다
[CreateAssetMenu(fileName = "Stage_00", menuName = "SlimeTD/Stage Data", order = 23)]
public class StageData : ScriptableObject
{
    [SerializeField] private int _stageId;
    [SerializeField] private string _codeName;
    [SerializeField] private GameMode _mode;
    [SerializeField] private Difficulty _difficulty;
    [SerializeField] private string _displayName;
    [SerializeField] private bool _enabled = true;

    [Header("플레이어")]
    [SerializeField] private int _startingLife = 10;
    [SerializeField] private int _startingJelly;

    [Header("진행")]
    [SerializeField] private float _spawnInterval = 0.8f;
    [SerializeField] private float _interWaveDelay = 2f;
    [Tooltip("일반 슬라임에만 적용. 보스는 기본 HP 고정")]
    [SerializeField] private float _normalHpGrowth = 0.06f;

    [Header("맵")]
    [SerializeField] private string _mapKey;

    [Header("웨이브 (1웨이브부터)")]
    [SerializeField] private List<StageWave> _waves = new();

    public int StageId => _stageId;
    public string CodeName => _codeName;
    public string MapKey => _mapKey;
    public GameMode Mode => _mode;
    public Difficulty Difficulty => _difficulty;
    public string DisplayName => _displayName;
    public bool Enabled => _enabled;
    public int StartingLife => _startingLife;
    public int StartingJelly => _startingJelly;
    public float SpawnInterval => _spawnInterval;
    public float InterWaveDelay => _interWaveDelay;
    public float NormalHpGrowth => _normalHpGrowth;
    public int FinalWave => _waves == null ? 0 : _waves.Count;

    public StageWave GetWave(int waveNumber)
    {
        int index = waveNumber - 1;
        if (_waves == null || index < 0 || index >= _waves.Count)
            return null;

        return _waves[index];
    }
}
