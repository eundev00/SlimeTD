using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleView : MonoBehaviour
{
    [NotNull][SerializeField] private RectTransform _waveScrollSlot;
    [NotNull][SerializeField] private Button _startButton;

    public RectTransform WaveScrollSlot => _waveScrollSlot;

    public event Action StartClicked;

    private void Start()
    {
        if (_startButton == null)
        {
            Debug.Log("[BattleView] 시작 버튼이 연결되지 않았다", this);
            return;
        }

        _startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        StartClicked?.Invoke();
    }
}
