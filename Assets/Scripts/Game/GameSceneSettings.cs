// TODO: 로비에서 스테이지를 고르는 구조가 생기면 씬 설정 대신 그 선택을 쓴다
public class GameSceneSettings
{
    public string StageKey { get; }

    public GameSceneSettings(string stageKey)
    {
        StageKey = stageKey;
    }
}