using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GridSceneViewDrawer
{
    private static readonly Color PlaceableColor = new Color(0f, 1f, 0f, 0.15f);
    private static readonly Color BlockedColor = new Color(1f, 0f, 0f, 0.15f);
    private static readonly Color PathColor = new Color(1f, 1f, 0f, 0.15f);
    private static readonly Color OutlineColor = new Color(1f, 1f, 1f, 0.4f);
    private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.4f);

    private static readonly Vector3[] CellVertices = new Vector3[4];

    private static GridMapReference _cachedReference;
    private static bool _isPainting;
    private static int _lastPaintedX = int.MinValue;
    private static int _lastPaintedY = int.MinValue;

    static GridSceneViewDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged += InvalidateCache;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        PrefabStage.prefabStageOpened += OnPrefabStageChanged;
        PrefabStage.prefabStageClosing += OnPrefabStageChanged;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        GridMapData gridMapData = ResolveGridMapData();
        if (gridMapData == null)
        {
            return;
        }

        if (Event.current.type == EventType.Repaint)
        {
            DrawCells(gridMapData);
            DrawGridLines(gridMapData);
        }

        DrawCenterHandle(gridMapData);
        HandlePaintInput(gridMapData);
    }

    #region 조회

    private static GridMapData ResolveGridMapData()
    {
        if (_cachedReference == null || !IsActive(_cachedReference))
        {
            _cachedReference = FindReference();
        }

        return _cachedReference == null ? null : _cachedReference.GridMapData;
    }

    private static GridMapReference FindReference()
    {
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            GameObject root = prefabStage.prefabContentsRoot;
            return root == null
                ? null
                : FirstActive(root.GetComponentsInChildren<GridMapReference>(true));
        }

        return FirstActive(Object.FindObjectsByType<GridMapReference>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    private static GridMapReference FirstActive(GridMapReference[] candidates)
    {
        foreach (GridMapReference candidate in candidates)
        {
            if (IsActive(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsActive(GridMapReference reference)
    {
        return reference != null && reference.enabled && reference.gameObject.activeInHierarchy;
    }

    private static void InvalidateCache()
    {
        _cachedReference = null;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        InvalidateCache();
    }

    private static void OnSceneClosed(Scene scene)
    {
        InvalidateCache();
    }

    private static void OnPrefabStageChanged(PrefabStage stage)
    {
        InvalidateCache();
    }

    #endregion

    #region 그리기

    private static void DrawCells(GridMapData gridMapData)
    {
        float cellSize = gridMapData.CellSize;
        Vector3 origin = gridMapData.Origin;

        for (int y = 0; y < gridMapData.Height; y++)
        {
            for (int x = 0; x < gridMapData.Width; x++)
            {
                Vector3 corner = origin + new Vector3(x * cellSize, 0f, y * cellSize);
                CellVertices[0] = corner;
                CellVertices[1] = corner + new Vector3(cellSize, 0f, 0f);
                CellVertices[2] = corner + new Vector3(cellSize, 0f, cellSize);
                CellVertices[3] = corner + new Vector3(0f, 0f, cellSize);

                Handles.DrawSolidRectangleWithOutline(
                    CellVertices,
                    GetCellColor(gridMapData.GetCellState(x, y)),
                    OutlineColor);
            }
        }
    }

    private static void DrawGridLines(GridMapData gridMapData)
    {
        float cellSize = gridMapData.CellSize;
        Vector3 origin = gridMapData.Origin;
        float totalWidth = gridMapData.Width * cellSize;
        float totalHeight = gridMapData.Height * cellSize;

        Color previousColor = Handles.color;
        Handles.color = LineColor;

        for (int x = 0; x <= gridMapData.Width; x++)
        {
            Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
            Handles.DrawLine(start, start + new Vector3(0f, 0f, totalHeight));
        }

        for (int y = 0; y <= gridMapData.Height; y++)
        {
            Vector3 start = origin + new Vector3(0f, 0f, y * cellSize);
            Handles.DrawLine(start, start + new Vector3(totalWidth, 0f, 0f));
        }

        Handles.color = previousColor;
    }

    private static void DrawCenterHandle(GridMapData gridMapData)
    {
        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.PositionHandle(gridMapData.CenterPosition, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(gridMapData, "Move Grid Center");
        gridMapData.SetCenterPosition(moved);
        EditorUtility.SetDirty(gridMapData);
        SceneView.RepaintAll();
    }

    private static Color GetCellColor(GridCellState state)
    {
        switch (state)
        {
            case GridCellState.Blocked:
                return BlockedColor;
            case GridCellState.Path:
                return PathColor;
            default:
                return PlaceableColor;
        }
    }

    #endregion

    #region 입력

    private static void HandlePaintInput(GridMapData gridMapData)
    {
        Event current = Event.current;

        if (current.button != 0 || current.alt)
        {
            return;
        }

        switch (current.type)
        {
            case EventType.MouseDown:
                if (GUIUtility.hotControl != 0)
                {
                    return;
                }

                _isPainting = true;
                _lastPaintedX = int.MinValue;
                _lastPaintedY = int.MinValue;

                if (TryPaint(gridMapData, current.mousePosition))
                {
                    current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (!_isPainting)
                {
                    return;
                }

                TryPaint(gridMapData, current.mousePosition);
                current.Use();
                break;

            case EventType.MouseUp:
                if (!_isPainting)
                {
                    return;
                }

                _isPainting = false;
                _lastPaintedX = int.MinValue;
                _lastPaintedY = int.MinValue;
                current.Use();
                break;
        }
    }

    private static bool TryPaint(GridMapData gridMapData, Vector2 mousePosition)
    {
        if (!TryGetCoordinate(gridMapData, mousePosition, out int x, out int y))
        {
            return false;
        }

        if (x == _lastPaintedX && y == _lastPaintedY)
        {
            return true;
        }

        _lastPaintedX = x;
        _lastPaintedY = y;

        GridCellState mode = GridPaintMode.CurrentMode;
        if (gridMapData.GetCellState(x, y) == mode)
        {
            return true;
        }

        Undo.RecordObject(gridMapData, "Paint Grid Cell");
        gridMapData.SetCellState(x, y, mode);
        EditorUtility.SetDirty(gridMapData);
        SceneView.RepaintAll();
        return true;
    }

    private static bool TryGetCoordinate(GridMapData gridMapData, Vector2 mousePosition, out int x, out int y)
    {
        x = -1;
        y = -1;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        var plane = new Plane(Vector3.up, gridMapData.Origin);

        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        (x, y) = GridUtility.WorldToGrid(ray.GetPoint(distance), gridMapData);
        return gridMapData.IsValidCoordinate(x, y);
    }

    #endregion
}
