using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SlimeDrawOrder : IDisposable
{
    private static readonly int StencilRefId = Shader.PropertyToID("_SlimeStencilRef");
    private static readonly int StencilCompId = Shader.PropertyToID("_SlimeStencilComp");
    private static readonly int StencilPassId = Shader.PropertyToID("_SlimeStencilPass");

    private readonly Renderer[] _renderers;
    private readonly Material[] _materials;

    // sortingOrder가 그리는 순서를 정하는 건 Transparent 큐뿐이라 슬라임을 그 큐로 옮긴다 (ZWrite는 그대로 켜서 자기 몸 가림은 깊이로 한다).
    // 스텐실 값이 슬라임마다 달라야 해서 머티리얼을 인스턴스로 쓴다 (렌더 상태는 PropertyBlock으로 못 바꾼다)
    public SlimeDrawOrder(GameObject root)
    {
        var renderers = new List<Renderer>();
        var materials = new List<Material>();

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer)
                continue;

            renderers.Add(renderer);
            foreach (var material in renderer.materials)
            {
                int queueOffset = material.renderQueue >= (int)RenderQueue.AlphaTest ? 1 : 0;
                material.renderQueue = (int)RenderQueue.Transparent + queueOffset;
                material.SetFloat(StencilCompId, (float)CompareFunction.GreaterEqual);
                material.SetFloat(StencilPassId, (float)StencilOp.Replace);
                materials.Add(material);
            }
        }

        _renderers = renderers.ToArray();
        _materials = materials.ToArray();
    }

    public void Set(int sortingOrder, int stencilRef)
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].sortingOrder = sortingOrder;

        for (int i = 0; i < _materials.Length; i++)
            _materials[i].SetFloat(StencilRefId, stencilRef);
    }

    public void Dispose()
    {
        for (int i = 0; i < _materials.Length; i++)
            UnityEngine.Object.Destroy(_materials[i]);
    }
}
