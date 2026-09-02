Shader "SlimeTD/Checkerboard"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.85, 0.85, 0.85, 1)
        _ColorB ("Color B", Color) = (0.35, 0.35, 0.35, 1)
        _CellSize ("Cell Size", Float) = 1
        _Offset ("Offset (XZ)", Vector) = (0, 0, 0, 0)
        _LineColor ("Line Color", Color) = (0, 0, 0, 1)
        _LineWidth ("Line Width", Range(0, 0.5)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 gridPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _LineColor;
                float4 _Offset;
                float _CellSize;
                float _LineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // Quad 는 XY 평면이라 로컬 xy 를 쓴다. 스케일을 곱해 칸을 월드 크기로 고정하고, 절반을 더해 좌하단 모서리를 0으로 만든다
                float2 scale = float2(
                    length(GetObjectToWorldMatrix()._m00_m10_m20),
                    length(GetObjectToWorldMatrix()._m01_m11_m21));

                output.gridPos = (input.positionOS.xy + 0.5) * scale;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float cellSize = max(_CellSize, 0.0001);
                float2 scaled = (input.gridPos - _Offset.xy) / cellSize;
                float2 cellIndex = floor(scaled);

                // fmod 는 음수 좌표에서 음수를 반환해 원점 반대편 체커가 어긋난다
                float checker = frac((cellIndex.x + cellIndex.y) * 0.5) * 2.0;
                half4 color = lerp(_ColorA, _ColorB, checker);

                // fwidth 로 픽셀당 셀 변화량을 구해야 원거리에서 선이 뭉개지지 않는다
                float2 cellUV = frac(scaled);
                float2 edge = min(cellUV, 1.0 - cellUV);
                float2 aa = fwidth(scaled) * 0.5;
                float gridLine = 1.0 - min(
                    smoothstep(_LineWidth * 0.5 - aa.x, _LineWidth * 0.5 + aa.x, edge.x),
                    smoothstep(_LineWidth * 0.5 - aa.y, _LineWidth * 0.5 + aa.y, edge.y));

                return lerp(color, _LineColor, gridLine * _LineColor.a * step(0.0001, _LineWidth));
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
