using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VContainer;

public class TowerTestSpawner : MonoBehaviour
{
    private const float ReferenceWidth = 1080f;
    private const float PanelHeight = 440f;

    [SerializeField] private Transform _spawnRoot;
    [SerializeField] private Transform[] _spawnPoints;

    private IObjectResolver _resolver;
    private IGameplayService _gameplayService;
    private ITowerLevelService _levelService;

    private readonly List<TowerData> _towers = new List<TowerData>();
    private readonly List<TowerSkillData> _skillBuffer = new List<TowerSkillData>();
    private GameObject[] _spawned;
    private Transform _holder;
    private int _towerIndex;
    private int _level = TowerLevelService.MinLevel;
    private int _nextPoint;
    private GUIStyle _labelStyle;
    private GUIStyle _buttonStyle;

    [Inject]
    public void Construct(IObjectResolver resolver, IGameplayService gameplayService, ITowerLevelService levelService)
    {
        _resolver = resolver;
        _gameplayService = gameplayService;
        _levelService = levelService;
    }

    private void Start()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.Log("[TowerTestSpawner] _spawnPoints가 비어 있습니다.", this);
            return;
        }

        _spawned = new GameObject[_spawnPoints.Length];
    }

    public void Initialize(TowerCatalog catalog)
    {
        _towers.Clear();

        if (catalog == null || catalog.Towers == null)
        {
            Debug.Log("[TowerTestSpawner] TowerCatalog가 없습니다.", this);
            return;
        }

        foreach (var tower in catalog.Towers)
        {
            if (tower != null && tower.Prefab != null)
                _towers.Add(tower);
        }
    }

    private void OnDestroy()
    {
        if (_holder != null)
            Destroy(_holder.gameObject);
    }

    private void Spawn()
    {
        if (_towers.Count == 0 || _spawned == null || _resolver == null)
            return;

        var data = _towers[_towerIndex];
        int index = _nextPoint;
        _nextPoint = (_nextPoint + 1) % _spawnPoints.Length;

        if (_spawned[index] != null)
            Destroy(_spawned[index]);

        // 타워는 Initialize 시점의 레벨로 스킬을 켠다.
        _levelService.SetLevel(data.Id, _level);
        _spawned[index] = TowerFactory.Create(_resolver, data, GetHolder(), _spawnRoot, _spawnPoints[index].position);
    }

    private void Clear()
    {
        if (_spawned == null)
            return;

        for (int i = 0; i < _spawned.Length; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i]);

            _spawned[i] = null;
        }

        _nextPoint = 0;
    }

    private Transform GetHolder()
    {
        if (_holder != null)
            return _holder;

        var holderObject = new GameObject("[TowerTestHolder]");
        holderObject.SetActive(false);
        _holder = holderObject.transform;
        return _holder;
    }

    private void OnGUI()
    {
        if (_towers.Count == 0)
            return;

        EnsureStyles();

        float scale = Screen.width / ReferenceWidth;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        float height = Screen.height / scale;
        var panel = new Rect(0f, height - PanelHeight, ReferenceWidth, PanelHeight);
        GUI.Box(panel, GUIContent.none);

        var data = _towers[_towerIndex];
        float y = panel.y + 20f;

        if (GUI.Button(new Rect(20f, y, 120f, 90f), "◀", _buttonStyle))
            _towerIndex = (_towerIndex - 1 + _towers.Count) % _towers.Count;
        GUI.Label(new Rect(150f, y, 420f, 90f), $"{data.TowerName} ({data.Id})", _labelStyle);
        if (GUI.Button(new Rect(580f, y, 120f, 90f), "▶", _buttonStyle))
            _towerIndex = (_towerIndex + 1) % _towers.Count;

        if (GUI.Button(new Rect(740f, y, 320f, 90f), "스폰", _buttonStyle))
            Spawn();

        y += 110f;

        if (GUI.Button(new Rect(20f, y, 120f, 90f), "-", _buttonStyle))
            _level = Mathf.Max(TowerLevelService.MinLevel, _level - 1);
        GUI.Label(new Rect(150f, y, 420f, 90f), $"Lv.{_level}", _labelStyle);
        if (GUI.Button(new Rect(580f, y, 120f, 90f), "+", _buttonStyle))
            _level = Mathf.Min(TowerLevelService.MaxLevel, _level + 1);

        if (GUI.Button(new Rect(740f, y, 320f, 90f), "전체 삭제", _buttonStyle))
            Clear();

        y += 110f;
        GUI.Label(new Rect(20f, y, 1040f, 110f), DescribeSkills(data.Id), _labelStyle);

        y += 110f;
        GUI.Label(new Rect(20f, y, 1040f, 60f), $"젤리: {_gameplayService.Info.Jelly.Value}", _labelStyle);
    }

    private string DescribeSkills(int towerId)
    {
        var catalog = _gameplayService.SkillCatalog;
        if (catalog == null)
            return "스킬 카탈로그 없음";

        catalog.GetActiveSkills(towerId, _level, _skillBuffer);
        if (_skillBuffer.Count == 0)
            return "활성 스킬 없음";

        var builder = new StringBuilder("스킬: ");
        for (int i = 0; i < _skillBuffer.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");

            builder.Append(_skillBuffer[i].DisplayName);
        }

        return builder.ToString();
    }

    private void EnsureStyles()
    {
        if (_labelStyle != null)
            return;

        _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 40 };
    }
}
