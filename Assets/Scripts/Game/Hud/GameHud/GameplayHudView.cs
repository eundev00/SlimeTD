using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayHudView : MonoBehaviour
{
    [NotNull][SerializeField] private Button _summonButton;
    [NotNull][SerializeField] private TMP_Text _goldText;
    [NotNull][SerializeField] private TMP_Text _summonCostText;

    public event Action SummonButtonClicked;

    private void Start()
    {
        if (_summonButton == null)
        {
            Debug.Log("[GameplayHudView] _summonButton이 연결되지 않았습니다.", this);
            return;
        }
        _summonButton.onClick.AddListener(OnSummonButtonClicked);
    }

    private void OnDestroy()
    {
        if (_summonButton != null)
        {
            _summonButton.onClick.RemoveListener(OnSummonButtonClicked);
        }
    }

    private void OnSummonButtonClicked()
    {
        SummonButtonClicked?.Invoke();
    }

    public void SetSummonButtonInteractable(bool interactable)
    {
        if (_summonButton != null)
            _summonButton.interactable = interactable;
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
