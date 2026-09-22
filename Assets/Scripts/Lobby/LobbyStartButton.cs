using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LobbyStartButton : MonoBehaviour
{
    [NotNull][SerializeField] private Button _startButton;
    [SerializeField] private Button _testStartButton;
    [SerializeField] private Button _game2StartButton;

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
            Debug.Log("[LobbyStartButton] _startButton이 연결되지 않았습니다.", this);
            return;
        }

        _startButton.onClick.AddListener(OnStartButtonClicked);

        if (_testStartButton != null)
            _testStartButton.onClick.AddListener(OnTestStartButtonClicked);

        if (_game2StartButton != null)
            _game2StartButton.onClick.AddListener(OnGame2StartButtonClicked);
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);

        if (_testStartButton != null)
            _testStartButton.onClick.RemoveListener(OnTestStartButtonClicked);

        if (_game2StartButton != null)
            _game2StartButton.onClick.RemoveListener(OnGame2StartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        LoadScene(SceneNames.Game);
    }

    private void OnTestStartButtonClicked()
    {
        LoadScene(SceneNames.GameTest);
    }

    private void OnGame2StartButtonClicked()
    {
        LoadScene(SceneNames.Game2);
    }

    private void LoadScene(string sceneName)
    {
        _startButton.interactable = false;

        if (_testStartButton != null)
            _testStartButton.interactable = false;

        if (_game2StartButton != null)
            _game2StartButton.interactable = false;

        // 취소 토큰을 붙이지 말 것: 전환 도중 이 오브젝트가 파괴되어 자기 전환을 취소한다
        _sceneLoader.TransitionAsync(SceneNames.Lobby, sceneName).Forget();
    }
}
