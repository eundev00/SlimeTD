using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTabs : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _panel;
        [SerializeField] private bool _locked;
        [SerializeField] private GameObject _normalState;
        [SerializeField] private GameObject _focusState;

        public Button Button => _button;
        public GameObject Panel => _panel;
        public bool Locked => _locked;
        public GameObject NormalState => _normalState;
        public GameObject FocusState => _focusState;
    }

    [SerializeField] private Tab[] _tabs;
    [SerializeField] private int _defaultIndex;

    private int _currentIndex = -1;

    private void Start()
    {
        if (_tabs == null || _tabs.Length == 0)
        {
            Debug.Log("[LobbyTabs] 탭이 설정되지 않았습니다.", this);
            return;
        }

        for (int i = 0; i < _tabs.Length; i++)
        {
            var tab = _tabs[i];
            if (tab.Button == null)
                continue;

            int index = i;
            tab.Button.onClick.AddListener(() => Select(index));
        }

        Select(_defaultIndex);
    }

    private void OnDestroy()
    {
        if (_tabs == null)
            return;

        for (int i = 0; i < _tabs.Length; i++)
        {
            if (_tabs[i].Button != null)
                _tabs[i].Button.onClick.RemoveAllListeners();
        }
    }

    // 잠긴 탭은 선택할 수 없다.
    private void Select(int index)
    {
        if (index < 0 || index >= _tabs.Length || index == _currentIndex)
            return;

        if (_tabs[index].Locked)
            return;

        _currentIndex = index;

        for (int i = 0; i < _tabs.Length; i++)
            Apply(_tabs[i], i == index);
    }

    private void Apply(Tab tab, bool selected)
    {
        if (tab.Panel != null)
            tab.Panel.SetActive(selected);

        if (tab.NormalState != null)
            tab.NormalState.SetActive(!selected);

        if (tab.FocusState != null)
            tab.FocusState.SetActive(selected);
    }
}
