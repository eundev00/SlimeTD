using System.Collections.Generic;
using UnityEngine;

// 먼저 나온 슬라임을 먼저 그리고(sortingOrder 작을수록 먼저), 스텐실 값은 클수록 앞선다.
// 셰이더는 "내 값 >= 이미 쓰인 값"일 때만 그려서, 나중 슬라임이 실제로 더 앞에 있어도 먼저 나온 슬라임을 덮지 못한다
public class SlimeDrawOrderService : ISlimeDrawOrderService
{
    private const int BossStencilRef = 255;
    private const int BossSortingOrder = 0;
    // 스텐실이 8비트라 일반 슬라임은 동시에 254마리(1~254)까지 순서를 구분한다
    private const int MaxNormalRank = 253;

    private readonly List<SlimeDrawOrder> _normals = new List<SlimeDrawOrder>();
    private int _nextRank;

    public void Add(SlimeDrawOrder drawOrder, SlimeRenderGroup group)
    {
        if (drawOrder == null)
            return;

        if (group == SlimeRenderGroup.Boss)
        {
            drawOrder.Set(BossSortingOrder, BossStencilRef);
            return;
        }

        if (_nextRank > MaxNormalRank)
            Compact();

        _normals.Add(drawOrder);
        Apply(drawOrder, _nextRank);
        _nextRank++;
    }

    public void Remove(SlimeDrawOrder drawOrder)
    {
        _normals.Remove(drawOrder);
    }

    // 순번이 끝까지 차면 살아 있는 슬라임만 등장 순서대로 다시 매긴다
    private void Compact()
    {
        for (int i = 0; i < _normals.Count; i++)
            Apply(_normals[i], i);

        _nextRank = _normals.Count;
    }

    private static void Apply(SlimeDrawOrder drawOrder, int rank)
    {
        int clamped = Mathf.Min(rank, MaxNormalRank);
        drawOrder.Set(clamped + 1, MaxNormalRank + 1 - clamped);
    }
}
