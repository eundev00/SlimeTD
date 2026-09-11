using UnityEngine;
using System.Collections.Generic;

public class SmoothNormalsToVertexColor : MonoBehaviour
{
    static Dictionary<Mesh, Mesh> cache = new Dictionary<Mesh, Mesh>();
    static HashSet<Mesh> bakedMeshes = new HashSet<Mesh>();

    void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        Mesh original = null;

        if (meshFilter != null)
        {
            original = meshFilter.sharedMesh;
        }
        else if (skinnedMeshRenderer != null)
        {
            original = skinnedMeshRenderer.sharedMesh;
        }

        if (original == null) return;

        if (bakedMeshes.Contains(original)) return;

        if (!cache.TryGetValue(original, out Mesh baked))
        {
            baked = Instantiate(original);
            BakeSmoothNormals(baked);
            cache[original] = baked;
            bakedMeshes.Add(baked);
        }

        if (meshFilter != null)
        {
            meshFilter.sharedMesh = baked;
        }
        else if (skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.sharedMesh = baked;
        }
    }

    void BakeSmoothNormals(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var normals = mesh.normals;

        var groups = new Dictionary<Vector3Int, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            var key = new Vector3Int(
                Mathf.RoundToInt(pos.x * 10000f),
                Mathf.RoundToInt(pos.y * 10000f),
                Mathf.RoundToInt(pos.z * 10000f));

            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<int>();
                groups[key] = list;
            }
            list.Add(i);
        }

        var colors = new Color[vertices.Length];

        foreach (var group in groups.Values)
        {
            Vector3 sum = Vector3.zero;
            foreach (int i in group) sum += normals[i];
            Vector3 avg = (sum / group.Count).normalized;

            Color encoded = new Color(avg.x * 0.5f + 0.5f, avg.y * 0.5f + 0.5f, avg.z * 0.5f + 0.5f, 1f);
            foreach (int i in group) colors[i] = encoded;
        }

        mesh.colors = colors;
    }
}