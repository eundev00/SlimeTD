#ifndef ETFX_POWERBOX_COMMON_INCLUDED
#define ETFX_POWERBOX_COMMON_INCLUDED

TEXTURE2D(_Icon); SAMPLER(sampler_Icon);
TEXTURE2D(_Background); SAMPLER(sampler_Background);

CBUFFER_START(UnityPerMaterial)
	float4 _Background_ST;
	half4 _IconTint;
	half _IconContrast;
	half _IconBrightness;
	half4 _BackgroundTint;
	half _BackgroundContrast;
	half _BackgroundBrightness;
	float _IconScale;
	float4 _IconOffset;
	half _Smoothness;
	half _Metallic;
CBUFFER_END

// 원본 Amplify 그래프와 같은 계산: 배경(대비, 틴트, 버텍스 컬러) 위에 아이콘을 알파로 합성한다.
half3 PowerboxColor(float2 uv, half4 vertexColor)
{
	float2 backgroundUV = uv * _Background_ST.xy + _Background_ST.zw;
	half4 background = SAMPLE_TEXTURE2D(_Background, sampler_Background, backgroundUV);
	half4 backgroundColor = _BackgroundBrightness * (pow(max(background, 0), _BackgroundContrast) * _BackgroundTint) * vertexColor;

	float2 iconUV = uv * _IconScale + _IconOffset.xy;
	half4 icon = SAMPLE_TEXTURE2D(_Icon, sampler_Icon, iconUV);
	half4 iconColor = pow(max(icon + _IconTint, 0), _IconContrast) * _IconBrightness;

	return lerp(backgroundColor, iconColor, icon.a).rgb;
}

#endif
