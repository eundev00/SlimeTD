using MessagePipe;
using UniRx;
using UnityEngine;

// 게임플레이 수치 로직 담당(MVP의 Model). 상태는 GameplayInfo가 들고, 변경은 여기서만 한다.
// 기존 LifeService를 대체 — 라이프 차감/게임오버 로직은 그대로 유지하고 골드 처리를 통합했다.
public class GameplayService : IGameplayService
{
    private readonly GameplayInfo _info;
    private readonly IPublisher<GameOverEvent> _gameOverPublisher;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private bool _gameOverPublished;

    public GameplayInfo Info => _info;

    public GameplayService(
        GameConfig config,
        ISubscriber<SlimeReachedEndEvent> reachedEndSubscriber,
        ISubscriber<SlimeKilledEvent> killedSubscriber,
        IPublisher<GameOverEvent> gameOverPublisher)
    {
        _info = new GameplayInfo(config.StartingLife, config.StartingGold);
        _gameOverPublisher = gameOverPublisher;

        reachedEndSubscriber.Subscribe(OnSlimeReachedEnd).AddTo(_disposables);
        killedSubscriber.Subscribe(OnSlimeKilled).AddTo(_disposables);
    }

    private void OnSlimeReachedEnd(SlimeReachedEndEvent e)
    {
        if (_gameOverPublished)
            return;

        _info.Life.Value = Mathf.Max(0, _info.Life.Value - e.LifeCost);
        // TODO: UI 붙으면 제거 — 임시 확인용 로그
        Debug.Log($"[GameplayService] 라이프 {_info.Life.Value}");

        if (_info.Life.Value <= 0)
        {
            _gameOverPublished = true;
            _gameOverPublisher.Publish(new GameOverEvent());
            Debug.Log("[GameplayService] 게임오버");
        }
    }

    private void OnSlimeKilled(SlimeKilledEvent e)
    {
        // 하한 0 클램프 — 지금은 증가만 하지만 이후 골드 소모가 생겨도 음수로 못 내려가게 방어한다.
        _info.Gold.Value = Mathf.Max(0, _info.Gold.Value + e.GoldReward);
        // TODO: UI 붙으면 제거 — 임시 확인용 로그
        Debug.Log($"[GameplayService] 골드 {_info.Gold.Value}");
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _info.Dispose();
    }
}
