using Cysharp.Threading.Tasks;
using SlimeTD.Protocol;

// 전투 종료 응답이 정상이면 결과 팝업, 아니면 에러 팝업을 열고 확인하면 로비로 간다
public static class BattleFinishPopups
{
    private const string ErrorTitle = "알림";

    public static async UniTask OpenAsync(ResultCode result, IPopupService popupService, ISceneLoader sceneLoader)
    {
        if (result == ResultCode.Ok)
        {
            await popupService.OpenAsync(PrefabKeys.GameResultPopup);
            return;
        }

        var popup = await popupService.OpenAsync<MessagePopup>(PrefabKeys.MessagePopup);
        if (popup == null)
            return;

        popup.Setup(ErrorTitle, GetErrorMessage(result));
        popup.Confirmed += () => sceneLoader.TransitionAsync(popup.gameObject.scene.name, SceneNames.Lobby).Forget();
    }

    // 에너지는 시작할 때 이미 써서 돌려주지 않는다. 문구에서 반환을 약속하지 않는다
    private static string GetErrorMessage(ResultCode result)
    {
        switch (result)
        {
            case ResultCode.BattleMismatch:
                return "전투 결과를 확인할 수 없어\n보상이 지급되지 않았습니다";
            case ResultCode.NotFound:
                return "전투 정보가 만료되었습니다";
            default:
                return "서버 오류가 발생했습니다\n잠시 후 다시 시도해주세요";
        }
    }
}
