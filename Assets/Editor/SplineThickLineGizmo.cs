using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public static class SplineThickLineGizmo
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawThick(SplineContainer container, GizmoType gizmoType)
    {
        var spline = container.Spline;
        if (spline == null || spline.Count < 2) return;

        const int resolution = 100;
        var points = new Vector3[resolution + 1];
        for (int i = 0; i <= resolution; i++)
        {
            spline.Evaluate(i / (float)resolution, out var pos, out _, out _);
            points[i] = container.transform.TransformPoint(pos);
        }

        Handles.color = Color.softRed;
        Handles.DrawAAPolyLine(20f, points);
    }
}
