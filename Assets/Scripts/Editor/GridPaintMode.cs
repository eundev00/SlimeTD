using UnityEditor;

public static class GridPaintMode
{
    private const string PaintModeKey = "SlimeTD.Grid.PaintMode";

    private const string PlaceableMenu = "Tools/SlimeTD/Grid Paint Mode/Placeable";
    private const string BlockedMenu = "Tools/SlimeTD/Grid Paint Mode/Blocked";
    private const string PathMenu = "Tools/SlimeTD/Grid Paint Mode/Path";

    public static GridCellState CurrentMode
    {
        get => (GridCellState)EditorPrefs.GetInt(PaintModeKey, (int)GridCellState.Placeable);
        set => EditorPrefs.SetInt(PaintModeKey, (int)value);
    }

    [MenuItem(PlaceableMenu)]
    private static void SetPlaceable()
    {
        SetMode(GridCellState.Placeable);
    }

    [MenuItem(PlaceableMenu, true)]
    private static bool ValidatePlaceable()
    {
        Menu.SetChecked(PlaceableMenu, CurrentMode == GridCellState.Placeable);
        return true;
    }

    [MenuItem(BlockedMenu)]
    private static void SetBlocked()
    {
        SetMode(GridCellState.Blocked);
    }

    [MenuItem(BlockedMenu, true)]
    private static bool ValidateBlocked()
    {
        Menu.SetChecked(BlockedMenu, CurrentMode == GridCellState.Blocked);
        return true;
    }

    [MenuItem(PathMenu)]
    private static void SetPath()
    {
        SetMode(GridCellState.Path);
    }

    [MenuItem(PathMenu, true)]
    private static bool ValidatePath()
    {
        Menu.SetChecked(PathMenu, CurrentMode == GridCellState.Path);
        return true;
    }

    private static void SetMode(GridCellState mode)
    {
        CurrentMode = mode;
        SceneView.RepaintAll();
    }
}
