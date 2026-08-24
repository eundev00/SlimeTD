using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LobbyStartButton : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _testStartButton;

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

        if (_testStartButton != null)
            _testStartButton.onClick.AddListener(OnTestStartButtonClicked);
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartButtonClicked);

        if (_testStartButton != null)
            _testStartButton.onClick.RemoveListener(OnTestStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        LoadScene(SceneNames.Game);
    }

    private void OnTestStartButtonClicked()
    {
        LoadScene(SceneNames.GameTest);
    }

    private void LoadScene(string sceneName)
    {
        _startButton.interactable = false;

        if (_testStartButton != null)
            _testStartButton.interactable = false;

        // 취소 토큰을 붙이지 말 것: 전환 도중 이 오브젝트가 파괴되어 자기 전환을 취소한다
        _sceneLoader.TransitionAsync(SceneNames.Lobby, sceneName).Forget();
    }
}
