using Cysharp.Threading.Tasks;
using Services.PoolService;
using UnityEngine;
using VContainer;

public class DamageTextSpawner : MonoBehaviour
{
    [SerializeField] private int _poolCapacity = 16;
    [SerializeField] private int _poolMaxSize = 64;

    private IGameObjectPoolService _poolService;
    private IResourceLoadService _resourceLoadService;
    private Canvas _canvas;
    private Camera _camera;
    private GameObject _damageTextPrefab;
    private GameObject _criticalDamageTextPrefab;

    [Inject]
    public void Construct(
        IGameObjectPoolService poolService,
        IResourceLoadService resourceLoadService)
    {
        _poolService = poolService;
        _resourceLoadService = resourceLoadService;
    }

    private async void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.Log("[DamageTextSpawner] Canvas를 찾을 수 없습니다.", this);
            return;
        }

        _camera = Camera.main;

        _damageTextPrefab = await _resourceLoadService.LoadAsync<GameObject>(PrefabKeys.DamageText);
        if (_damageTextPrefab == null)
        {
            Debug.Log("[DamageTextSpawner] DamageText 프리팹 로드 실패", this);
            return;
        }

        _poolService.CreatePool(_damageTextPrefab, _poolCapacity, _poolMaxSize);

        _criticalDamageTextPrefab = await _resourceLoadService.LoadAsync<GameObject>(PrefabKeys.CriticalDamageText);
        if (_criticalDamageTextPrefab == null)
        {
            Debug.Log("[DamageTextSpawner] CriticalDamageText 프리팹 로드 실패. 일반 데미지 텍스트로 대체합니다.", this);
            return;
        }

        _poolService.CreatePool(_criticalDamageTextPrefab, _poolCapacity, _poolMaxSize);
    }

    public void Show(int damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (_camera == null)
            _camera = Camera.main;

        var prefab = isCritical && _criticalDamageTextPrefab != null
            ? _criticalDamageTextPrefab
            : _damageTextPrefab;

        if (_canvas == null || _camera == null || prefab == null)
        {
            Debug.Log($"[DamageTextSpawner] null 체크 실패: canvas={_canvas != null}, camera={_camera != null}, prefab={prefab != null}");
            return;
        }

        var instance = _poolService.Get(prefab);
        if (instance == null)
        {
            Debug.Log("[DamageTextSpawner] 인스턴스 Get 실패");
            return;
        }

        instance.transform.SetParent(transform, false);

        var view = instance.GetComponent<DamageTextView>();
        if (view != null)
            view.Initialize(damage, worldPosition, _canvas, _camera);
    }
}
