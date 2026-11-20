using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHudView : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _jellyText;
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
