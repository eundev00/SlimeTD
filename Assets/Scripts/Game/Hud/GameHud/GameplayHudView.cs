using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHudView : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _goldText;
    [NotNull][SerializeField] private TMP_Text _summonCostText;
    [SerializeField] private Button _testSummonButton;

    public event Action TestSummonButtonClicked;

    private void Start()
    {
        if (_testSummonButton != null)
            _testSummonButton.onClick.AddListener(OnTestSummonButtonClicked);
    }

    private void OnDestroy()
    {
        if (_testSummonButton != null)
        {
            _testSummonButton.onClick.RemoveListener(OnTestSummonButtonClicked);
        }
    }

    private void OnTestSummonButtonClicked()
    {
        TestSummonButtonClicked?.Invoke();
    }

    public void SetGold(int gold)
    {
        if (_goldText != null)
            _goldText.text = gold.ToString();
    }

    public void SetSummonCost(int cost)
    {
        if (_summonCostText != null)
            _summonCostText.text = cost.ToString();
    }
}
