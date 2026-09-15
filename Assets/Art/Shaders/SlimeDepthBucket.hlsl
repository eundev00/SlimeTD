#ifndef SLIME_DEPTH_BUCKET_INCLUDED
#define SLIME_DEPTH_BUCKET_INCLUDED

// _SlimeDepthBucket은 각 셰이더의 UnityPerMaterial CBUFFER에 선언한다 (SRP Batcher 요구사항).
float _SlimeBucketSpacing;

// 직교 투영에서는 clip z가 near~far 전체 범위에 선형 대응하므로
// 카메라 거리로 스케일하지 않고 월드 유닛을 NDC로 환산해 더한다.
float SlimeDepthBucketOffset()
{
    float ndcPerUnit = 2.0 / (_ProjectionParams.z - _ProjectionParams.y);

    // 카메라 쪽으로 당기면 전경 오브젝트를 뚫으므로 뒤로 민다.
#if defined(UNITY_REVERSED_Z)
    float direction = -1.0;
#else
    float direction = 1.0;
#endif

    return _SlimeDepthBucket * _SlimeBucketSpacing * ndcPerUnit * direction;
}

#endif
