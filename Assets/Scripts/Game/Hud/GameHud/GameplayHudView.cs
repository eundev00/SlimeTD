using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHudView : MonoBehaviour
{
    [NotNull][SerializeField] private Button _summonButton;
    [NotNull][SerializeField] private TMP_Text _jellyText;
    [NotNull][SerializeField] private TMP_Text _summonCostText;
    [SerializeField] private Button _testSummonButton;

    public event Action SummonButtonClicked;
    public event Action TestSummonButtonClicked;

    private void Start()
    {
        if (_summonButton == null)
        {
            Debug.Log("[GameplayHudView] _summonButton이 연결되지 않았습니다.", this);
            return;
        }
        _summonButton.onClick.AddListener(OnSummonButtonClicked);

        if (_testSummonButton != null)
            _testSummonButton.onClick.AddListener(OnTestSummonButtonClicked);
    }

    private void OnDestroy()
    {
        if (_summonButton != null)
        {
            _summonButton.onClick.RemoveListener(OnSummonButtonClicked);
        }

        if (_testSummonButton != null)
        {
            _testSummonButton.onClick.RemoveListener(OnTestSummonButtonClicked);
        }
    }

    private void OnSummonButtonClicked()
    {
        SummonButtonClicked?.Invoke();
    }

    private void OnTestSummonButtonClicked()
    {
        TestSummonButtonClicked?.Invoke();
    }

    public void SetSummonButtonInteractable(bool interactable)
    {
        if (_summonButton != null)
            _summonButton.interactable = interactable;
    }

    public void SetJelly(int jelly)
    {
        if (_jellyText != null)
            _jellyText.text = jelly.ToString();
    }

    public void SetSummonCost(int cost)
    {
        if (_summonCostText != null)
            _summonCostText.text = cost.ToString();
    }
}
