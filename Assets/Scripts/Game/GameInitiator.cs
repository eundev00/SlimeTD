using VContainer.Unity;

public class GameInitiator : IStartable
{
    private readonly IGameplayService _gameplayService;

    public GameInitiator(IGameplayService gameplayService)
    {
        _gameplayService = gameplayService;
    }

    public void Start()
    {
        // TODO: 게임 시작 초기화
    }
}
