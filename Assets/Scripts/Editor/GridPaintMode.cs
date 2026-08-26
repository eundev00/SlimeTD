using UnityEditor;

public static class GridPaintMode
{
    private const string PaintModeKey = "SlimeTD.Grid.PaintMode";

    public static GridCellState CurrentMode
    {
        get => (GridCellState)EditorPrefs.GetInt(PaintModeKey, (int)GridCellState.Placeable);
        set => EditorPrefs.SetInt(PaintModeKey, (int)value);
    }
}
