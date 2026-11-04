// 원본은 빌트인 서피스 셰이더(Amplify)다. URP에서 같은 속성과 계산으로 다시 작성했다.
Shader "Archanor VFX/Epic Toon FX/ETFX_PowerboxUnlit"
{
	Properties
	{
		_Icon("Icon", 2D) = "white" {}
		_Background("Background", 2D) = "white" {}
		_IconTint("Icon Tint", Color) = (0,0,0,0)
		_IconContrast("Icon Contrast", Range( 0.01 , 5)) = 1
		_IconBrightness("Icon Brightness", Range( 0 , 3)) = 1
		_BackgroundTint("Background Tint", Color) = (1,0,0.05511808,0)
		_BackgroundContrast("Background Contrast", Range( 0 , 5)) = 1
		_BackgroundBrightness("Background Brightness", Range( 0 , 3)) = 1
		_IconScale("Icon Scale", Float) = 2
		_IconOffset("Icon Offset", Vector) = (0,-1,0,0)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
		Cull Back

		Pass
		{
			Name "Unlit"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "ETFX_PowerboxCommon.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				half fogFactor : TEXCOORD1;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				output.color = input.color;
				output.fogFactor = ComputeFogFactor(output.positionCS.z);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half3 color = PowerboxColor(input.uv, input.color);
				color = MixFog(color, input.fogFactor);
				return half4(color, 1);
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }
			ZWrite On
			ColorMask R
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float4 vert(float4 positionOS : POSITION) : SV_POSITION
			{
				return TransformObjectToHClip(positionOS.xyz);
			}

			half frag() : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}
	}
}
