using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleView : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _waveScrollSlot;
    [NotNull][SerializeField] private Button _startButton;
    [NotNull][SerializeField] private Button _devTestButton;
    [NotNull][SerializeField] private Button _hunterCardGachaButton;
    [NotNull][SerializeField] private GameObject _hunterCardGachaNotice;

    public RectTransform WaveScrollSlot => _waveScrollSlot;

    public event Action StartClicked;
    public event Action DevTestClicked;
    public event Action HunterCardGachaClicked;

    public void SetHunterCardGachaNotice(bool visible)
    {
        if (_hunterCardGachaNotice != null)
            _hunterCardGachaNotice.SetActive(visible);
    }

    public void SetStartInteractable(bool interactable)
    {
        _startButton.interactable = interactable;
        if (_devTestButton != null)
            _devTestButton.interactable = interactable;
    }

    private void Start()
    {
        if (_startButton == null)
        {
            Debug.Log("[BattleView] 시작 버튼이 연결되지 않았다", this);
            return;
        }

        _startButton.onClick.AddListener(OnStartButtonClicked);

        if (_hunterCardGachaButton == null)
            Debug.Log("[BattleView] 헌터 카드 뽑기 버튼이 연결되지 않았다", this);
        else
            _hunterCardGachaButton.onClick.AddListener(OnHunterCardGachaButtonClicked);

        if (_devTestButton == null)
        {
            Debug.Log("[BattleView] 데브 테스트 버튼이 연결되지 않았다", this);
            return;
        }

        _devTestButton.onClick.AddListener(OnDevTestButtonClicked);
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
        if (_devTestButton != null)
            _devTestButton.onClick.RemoveListener(OnDevTestButtonClicked);
        if (_hunterCardGachaButton != null)
            _hunterCardGachaButton.onClick.RemoveListener(OnHunterCardGachaButtonClicked);
    }

    private void OnHunterCardGachaButtonClicked()
    {
        HunterCardGachaClicked?.Invoke();
    }

    private void OnStartButtonClicked()
    {
        StartClicked?.Invoke();
    }

    private void OnDevTestButtonClicked()
    {
        DevTestClicked?.Invoke();
    }
}
