using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlayerDataMenu
{
    private const string MenuRoot = "SlimeTD/Data/";
    private const string Title = "플레이어 데이터";

    [MenuItem(MenuRoot + "Reset Player Data")]
    public static void Reset()
    {
        string path = LocalPlayerDataStore.FilePath;

        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog(Title, "저장된 플레이어 데이터가 없습니다.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(Title, $"저장된 플레이어 데이터를 삭제합니다.\n다음 실행은 새 계정으로 시작합니다.\n\n{path}", "삭제", "취소"))
            return;

        File.Delete(path);
        Debug.Log($"[PlayerDataMenu] 플레이어 데이터를 삭제했습니다: {path}");
    }

    // 플레이 중에는 서비스가 값을 들고 있다가 다시 저장하므로 삭제를 막는다
    [MenuItem(MenuRoot + "Reset Player Data", true)]
    private static bool ValidateReset()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem(MenuRoot + "Show Player Data Folder")]
    public static void ShowFolder()
    {
        string path = LocalPlayerDataStore.FilePath;
        string target = File.Exists(path) ? path : Application.persistentDataPath;
        EditorUtility.RevealInFinder(target);
    }
}
