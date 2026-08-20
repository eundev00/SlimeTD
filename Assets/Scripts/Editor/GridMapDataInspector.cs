using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridMapData))]
public class GridMapDataInspector : Editor
{
    private static readonly string[] PaintModeLabels = { "Placeable", "Blocked", "Path" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_centerPosition"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_width"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_height"));

        if (serializedObject.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("페인트 모드", EditorStyles.boldLabel);

        int selected = GUILayout.Toolbar((int)GridPaintMode.CurrentMode, PaintModeLabels);
        if (selected != (int)GridPaintMode.CurrentMode)
        {
            GridPaintMode.CurrentMode = (GridCellState)selected;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();

        var gridMapData = (GridMapData)target;

        if (GUILayout.Button("전체 Placeable로 초기화"))
        {
            FillAll(gridMapData, GridCellState.Placeable);
        }

        if (GUILayout.Button("전체 Blocked로 초기화"))
        {
            FillAll(gridMapData, GridCellState.Blocked);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("씬 뷰에서 좌클릭 또는 드래그로 칸을 칠한다.", MessageType.None);
    }

    private static void FillAll(GridMapData gridMapData, GridCellState state)
    {
        Undo.RecordObject(gridMapData, "그리드 전체 초기화");

        for (int y = 0; y < gridMapData.Height; y++)
        {
            for (int x = 0; x < gridMapData.Width; x++)
            {
                gridMapData.SetCellState(x, y, state);
            }
        }

        EditorUtility.SetDirty(gridMapData);
        SceneView.RepaintAll();
    }
}
