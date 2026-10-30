using UnityEngine;
using System.Collections.Generic;

// SlimeFlatKit 외곽선의 Smooth Normals 옵션은 UV 채널 2(uv3)에서 노멀을 읽는다. FBX 메시에는 이 채널이 없어서 런타임에 굽는다.
public class OutlineSmoothNormals : MonoBehaviour
{
    private const int SmoothNormalUVChannel = 2;

    private static readonly Dictionary<Mesh, Mesh> _cache = new Dictionary<Mesh, Mesh>();
    private static readonly HashSet<Mesh> _bakedMeshes = new HashSet<Mesh>();

    private void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        Mesh original = null;

        if (meshFilter != null)
            original = meshFilter.sharedMesh;
        else if (skinnedMeshRenderer != null)
            original = skinnedMeshRenderer.sharedMesh;

        if (original == null || _bakedMeshes.Contains(original))
            return;

        if (!_cache.TryGetValue(original, out Mesh baked))
        {
            baked = Instantiate(original);
            BakeSmoothNormals(baked);
            _cache[original] = baked;
            _bakedMeshes.Add(baked);
        }

        if (meshFilter != null)
            meshFilter.sharedMesh = baked;
        else
            skinnedMeshRenderer.sharedMesh = baked;
    }

    private static void BakeSmoothNormals(Mesh mesh)
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

        var smoothNormals = new Vector3[vertices.Length];

        foreach (var group in groups.Values)
        {
            Vector3 sum = Vector3.zero;
            foreach (int i in group)
                sum += normals[i];

            Vector3 avg = sum.normalized;
            foreach (int i in group)
                smoothNormals[i] = avg;
        }

        mesh.SetUVs(SmoothNormalUVChannel, smoothNormals);
    }
}
