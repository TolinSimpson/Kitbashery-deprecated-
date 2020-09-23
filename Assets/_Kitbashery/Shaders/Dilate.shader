Shader "Kitbashery/Dilate" {

	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Size("Size", Float) = 0.001
	}

		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGINCLUDE

#include "UnityCG.cginc"

		sampler2D _MainTex;
		float _Size;

		float sobel(sampler2D tex, float2 uv)
		{
			float2 delta = float2(_Size, _Size);

			float4 hr = float4(0, 0, 0, 0);
			float4 vt = float4(0, 0, 0, 0);

			hr += tex2D(tex, (uv + float2(-1.0, -1.0) * delta)) *  1.0;
			hr += tex2D(tex, (uv + float2(1.0, -1.0) * delta)) * -1.0;
			hr += tex2D(tex, (uv + float2(-1.0,  0.0) * delta)) *  2.0;
			hr += tex2D(tex, (uv + float2(1.0,  0.0) * delta)) * -2.0;
			hr += tex2D(tex, (uv + float2(-1.0,  1.0) * delta)) *  1.0;
			hr += tex2D(tex, (uv + float2(1.0,  1.0) * delta)) * -1.0;

			vt += tex2D(tex, (uv + float2(-1.0, -1.0) * delta)) *  1.0;
			vt += tex2D(tex, (uv + float2(0.0, -1.0) * delta)) *  2.0;
			vt += tex2D(tex, (uv + float2(1.0, -1.0) * delta)) *  1.0;
			vt += tex2D(tex, (uv + float2(-1.0,  1.0) * delta)) * -1.0;
			vt += tex2D(tex, (uv + float2(0.0,  1.0) * delta)) * -2.0;
			vt += tex2D(tex, (uv + float2(1.0,  1.0) * delta)) * -1.0;

			return sqrt(hr * hr + vt * vt);
		}

	float4 frag(v2f_img IN) : COLOR
	{
		float s = sobel(_MainTex, IN.uv);
		return float4(tex2D(_MainTex, IN.uv).r + s, tex2D(_MainTex, IN.uv).g + s, tex2D(_MainTex, IN.uv).b + s, 1);
	}

		ENDCG

		Pass
	{
		CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
			ENDCG
	}

	}
		FallBack "Diffuse"
}