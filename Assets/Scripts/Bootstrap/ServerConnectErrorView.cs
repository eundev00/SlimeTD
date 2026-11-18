using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ServerConnectErrorView : MonoBehaviour
{
    [NotNull][SerializeField] private GameObject _root;
    [NotNull][SerializeField] private Button _retryButton;

    private UniTaskCompletionSource _retry;

    private void Start()
    {
        if (_root == null || _retryButton == null)
        {
            Debug.Log("[ServerConnectErrorView] 참조가 연결되지 않았습니다.", this);
            return;
        }

        _retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnDestroy()
    {
        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    public UniTask WaitRetryAsync()
    {
        _retry = new UniTaskCompletionSource();
        _root.SetActive(true);
        return _retry.Task;
    }

    private void OnRetryClicked()
    {
        _root.SetActive(false);
        _retry?.TrySetResult();
    }
}