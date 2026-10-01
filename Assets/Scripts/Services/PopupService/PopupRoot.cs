using UnityEngine;

namespace Services.PopupService
{
    public class PopupRoot : MonoBehaviour
    {
        [NotNull][SerializeField] private RectTransform _container;
        [NotNull][SerializeField] private GameObject _dim;

        public RectTransform Container => _container;

        private void Awake()
        {
            HideDim();
        }

        private void Start()
        {
            if (_container == null)
            {
                Debug.Log("[PopupRoot] _container가 연결되지 않았습니다.", this);
                return;
            }

            if (_dim == null)
            {
                Debug.Log("[PopupRoot] _dim이 연결되지 않았습니다.", this);
                return;
            }

            if (_dim.transform.parent != _container)
                Debug.Log("[PopupRoot] _dim은 _container의 자식이어야 팝업 바로 아래에 배치됩니다.", this);
        }

        public void ShowDimBelow(Transform popup)
        {
            if (_dim == null || popup == null)
                return;

            _dim.SetActive(true);

            // 딤이 팝업보다 앞 순서에 있으면 팝업 인덱스가 밀리므로 맨 뒤로 뺀 다음 인덱스를 읽는다
            _dim.transform.SetAsLastSibling();
            _dim.transform.SetSiblingIndex(popup.GetSiblingIndex());
        }

        public void HideDim()
        {
            if (_dim != null)
                _dim.SetActive(false);
        }
    }
}
