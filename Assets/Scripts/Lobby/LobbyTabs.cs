using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTabs : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private bool _locked;
        [SerializeField] private GameObject _normalState;
        [SerializeField] private GameObject _focusState;

        public Button Button => _button;
        public GameObject Panel => _panel;
        public Image Background => _background;
        public TMP_Text Label => _label;
        public bool Locked => _locked;
        public GameObject NormalState => _normalState;
        public GameObject FocusState => _focusState;
    }

    [SerializeField] private Tab[] _tabs;
    [SerializeField] private int _defaultIndex;
    [SerializeField] private Color _selectedColor = new Color(0.18f, 0.49f, 0.43f);
    [SerializeField] private Color _normalColor = new Color(0.87f, 0.89f, 0.86f);
    [SerializeField] private Color _lockedColor = new Color(0.82f, 0.84f, 0.81f);

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

        // 에셋 탭은 Normal/Focus 오브젝트 전환으로 선택 상태를 그린다. 색 갱신은 건너뛴다.
        if (tab.NormalState != null || tab.FocusState != null)
        {
            if (tab.NormalState != null)
                tab.NormalState.SetActive(!selected);

            if (tab.FocusState != null)
                tab.FocusState.SetActive(selected);

            return;
        }

        if (tab.Background != null)
            tab.Background.color = selected ? _selectedColor : (tab.Locked ? _lockedColor : _normalColor);

        if (tab.Label != null)
        {
            tab.Label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            tab.Label.color = selected ? Color.white : (tab.Locked ? new Color(0.62f, 0.66f, 0.62f) : new Color(0.37f, 0.4f, 0.37f));
        }
    }
}