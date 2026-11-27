using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHudView : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _goldText;
    [NotNull][SerializeField] private TMP_Text _jellyText;
    [NotNull][SerializeField] private TMP_Text _summonCostText;
    [SerializeField] private Color _affordableCostColor = Color.white;
    [SerializeField] private Color _unaffordableCostColor = new Color(1f, 0.42f, 0.42f);
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

    public void SetTestSummonVisible(bool visible)
    {
        if (_testSummonButton != null)
            _testSummonButton.gameObject.SetActive(visible);
    }

    public void SetGold(int gold)
    {
        if (_goldText != null)
            _goldText.text = gold.ToString();
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

    public void SetSummonAffordable(bool affordable)
    {
        if (_summonCostText != null)
            _summonCostText.color = affordable ? _affordableCostColor : _unaffordableCostColor;
    }
}
