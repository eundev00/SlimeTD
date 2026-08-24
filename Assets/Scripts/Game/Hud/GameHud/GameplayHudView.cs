using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class GameplayHudView : MonoBehaviour
{
    [SerializeField] private Button _summonButton;

    private ITowerSpawner _towerSpawner;

    [Inject]
    public void Construct(ITowerSpawner towerSpawner)
    {
        _towerSpawner = towerSpawner;
    }

    private void Start()
    {
        if (_summonButton == null)
        {
            Debug.LogError("[GameplayHudView] _summonButton이 연결되지 않았습니다.", this);
            return;
        }

        _summonButton.onClick.AddListener(OnSummonButtonClicked);
    }

    private void OnDestroy()
    {
        if (_summonButton != null)
            _summonButton.onClick.RemoveListener(OnSummonButtonClicked);
    }

    private void OnSummonButtonClicked()
    {
        if (_towerSpawner == null)
        {
            Debug.LogError("[GameplayHudView] ITowerSpawner가 주입되지 않았습니다.", this);
            return;
        }

        _towerSpawner.TrySpawnRandom();
    }
}
