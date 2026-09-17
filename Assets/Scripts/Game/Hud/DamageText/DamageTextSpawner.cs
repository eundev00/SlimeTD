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
    }

    public void Show(int damage, Vector3 worldPosition)
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_canvas == null || _camera == null || _damageTextPrefab == null)
        {
            Debug.Log($"[DamageTextSpawner] null 체크 실패: canvas={_canvas != null}, camera={_camera != null}, prefab={_damageTextPrefab != null}");
            return;
        }

        var instance = _poolService.Get(_damageTextPrefab);
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
