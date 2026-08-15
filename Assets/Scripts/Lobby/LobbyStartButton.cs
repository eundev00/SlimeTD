using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// TODO: 로비 UI가 커지면 Service/View/Presenter 3계층으로 승격
public class LobbyStartButton : MonoBehaviour
{
    [SerializeField] private Button _startButton;

    private ISceneLoader _sceneLoader;

    [Inject]
    public void Construct(ISceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    private void Start()
    {
        if (_startButton == null)
        {
            Debug.LogError("[LobbyStartButton] _startButton이 연결되지 않았습니다.", this);
            return;
        }

        _startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        _startButton.interactable = false;

        // 취소 토큰을 붙이지 말 것: 전환 도중 이 오브젝트가 파괴되어 자기 전환을 취소한다
        _sceneLoader.TransitionAsync(SceneNames.Lobby, SceneNames.Game).Forget();
    }
}
