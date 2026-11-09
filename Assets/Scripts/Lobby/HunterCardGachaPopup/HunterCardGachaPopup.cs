using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[PopupPresenter(typeof(HunterCardGachaPresenter))]
public class HunterCardGachaPopup : PopupBase
{
    [NotNull][SerializeField] private HunterCardGachaPackView[] _packs;
    [NotNull][SerializeField] private Button _closeButton;
    [NotNull][SerializeField] private Button _dimButton;

    public IReadOnlyList<HunterCardGachaPackView> Packs => _packs;

    public event Action<int> PackClicked;
    public event Action CloseClicked;

    private void Start()
    {
        foreach (var pack in _packs)
        {
            if (pack != null)
                pack.Clicked += OnPackClicked;
        }

        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseClicked);
        if (_dimButton != null)
            _dimButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        foreach (var pack in _packs)
        {
            if (pack != null)
                pack.Clicked -= OnPackClicked;
        }

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        if (_dimButton != null)
            _dimButton.onClick.RemoveListener(OnCloseClicked);
    }

    private void OnPackClicked(int packId)
    {
        PackClicked?.Invoke(packId);
    }

    private void OnCloseClicked()
    {
        CloseClicked?.Invoke();
    }
}
