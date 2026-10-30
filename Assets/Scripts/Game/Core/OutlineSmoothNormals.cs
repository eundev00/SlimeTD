using UnityEngine;
using System.Collections.Generic;

// SlimeFlatKit 외곽선의 Smooth Normals 옵션은 UV 채널 2(uv3)에서 노멀을 읽는다. FBX 메시에는 이 채널이 없어서 런타임에 굽는다.
public class OutlineSmoothNormals : MonoBehaviour
{
    private const int SmoothNormalUVChannel = 2;
    private const string TangentNormalsKeyword = "_OUTLINE_TANGENT_NORMALS";

    private static readonly Dictionary<(Mesh, bool), Mesh> _cache = new Dictionary<(Mesh, bool), Mesh>();
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

        bool tangentSpace = UsesTangentSpace(GetComponent<Renderer>());

        if (!_cache.TryGetValue((original, tangentSpace), out Mesh baked))
        {
            baked = Instantiate(original);
            BakeSmoothNormals(baked, tangentSpace);
            _cache[(original, tangentSpace)] = baked;
            _bakedMeshes.Add(baked);
        }

        if (meshFilter != null)
            meshFilter.sharedMesh = baked;
        else
            skinnedMeshRenderer.sharedMesh = baked;
    }

    private static bool UsesTangentSpace(Renderer renderer)
    {
        if (renderer == null)
            return false;

        foreach (var material in renderer.sharedMaterials)
        {
            if (material != null && material.IsKeywordEnabled(TangentNormalsKeyword))
                return true;
        }

        return false;
    }

    private static void BakeSmoothNormals(Mesh mesh, bool tangentSpace)
    {
        if (tangentSpace && (mesh.tangents == null || mesh.tangents.Length == 0))
            mesh.RecalculateTangents();

        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var tangents = tangentSpace ? mesh.tangents : null;

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
                smoothNormals[i] = tangentSpace ? ToTangentSpace(avg, normals[i], tangents[i]) : avg;
        }

        mesh.SetUVs(SmoothNormalUVChannel, smoothNormals);
    }

    // 셰이더(SlimeFlatKit Outline 패스)의 복원식과 같은 직교화를 써야 방향이 일치한다.
    private static Vector3 ToTangentSpace(Vector3 smooth, Vector3 normal, Vector4 tangent)
    {
        Vector3 n = normal.normalized;
        Vector3 t = ((Vector3)tangent - n * Vector3.Dot(n, tangent)).normalized;
        Vector3 b = Vector3.Cross(n, t) * (tangent.w < 0f ? -1f : 1f);
        return new Vector3(Vector3.Dot(smooth, t), Vector3.Dot(smooth, b), Vector3.Dot(smooth, n));
    }
}
