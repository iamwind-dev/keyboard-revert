Shader "HunFX/SH_HunFX_common" {
	Properties {
		_TintColor ("Tint Color", Vector) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		[HDR] _color1 ("color1", Vector) = (1,1,1,1)
		[HDR] _color2 ("color2", Vector) = (0,0,0,1)
		[Toggle(_USEDISSOLVECOLOR_ON)] _UseDissolveColor ("UseDissolveColor", Float) = 0
		_DissolveColor_step ("DissolveColor_step", Vector) = (0,1,0,0)
		[Toggle(_USECOLOR_ON)] _UseColor ("UseColor", Float) = 1
		_ColorIntensity ("ColorIntensity", Float) = 1
		_MainTexture ("MainTexture", 2D) = "white" {}
		[Toggle] _MainTex_UV_invert ("MainTex_UV_invert", Float) = 0
		_MainTex_uv ("MainTex_uv", Vector) = (0,0,1,1)
		_MainTex_speed ("MainTex_speed", Vector) = (0,0,0,0)
		_DissolveTex ("DissolveTex", 2D) = "white" {}
		_DissolveTex_uv ("DissolveTex_uv", Vector) = (0,0,1,1)
		_Smoothstep ("Smoothstep", Vector) = (0,1,0,0)
		[Toggle] _DissTex_UV_invert ("DissTex_UV_invert", Float) = 0
		_DissolveTex_speed ("DissolveTex_speed", Vector) = (0,0,0,0)
		_VOTex ("VOTex", 2D) = "white" {}
		_VOTex_uv ("VOTex_uv", Vector) = (0,0,1,1)
		_VOTex_speed ("VOTex_speed", Vector) = (0,0,0,0)
		_VOintensity ("VOintensity", Float) = 0
		_DistTex ("DistTex", 2D) = "white" {}
		_DistTex_uv_speed ("DistTex_uv_speed", Vector) = (1,1,0,0)
		_Distort_intensity ("Distort_intensity", Range(0, 0.5)) = 0
		[Toggle(_USEFRESNEL_ON)] _UseFresnel ("UseFresnel", Float) = 0
		_Fresnel_pow ("Fresnel_pow", Float) = 5
		[Toggle] _MaskTex_UV_invert ("MaskTex_UV_invert", Float) = 0
		_Mask ("Mask", 2D) = "white" {}
		_MaskTex_tile ("MaskTex_tile", Vector) = (1,1,0,0)
		_MaskMult ("MaskMult", Float) = 1
		_Mask_udrl ("Mask_udrl", Vector) = (0,0,0,0)
		[Toggle(_USEPOSXSCROLL_ON)] _UsePosXScroll ("UsePosXScroll", Float) = 0
		_SubDissolveTex ("SubDissolveTex", 2D) = "white" {}
		[Toggle] _UseSubDissTex ("UseSubDissTex", Float) = 0
		_SubDissolveTex_multi ("SubDissolveTex_multi", Float) = 1
		_SubDissolveTex_add ("SubDissolveTex_add", Float) = 0
		[Toggle(_USEDEPTHFADE_ON)] _UseDepthFade ("UseDepthFade", Float) = 0
		_DepthFade_Distance ("DepthFade_Distance", Float) = 1
		[Toggle] _InvertFresnel ("InvertFresnel", Float) = 0
		[HideInInspector] _texcoord ("", 2D) = "white" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
	//CustomEditor "ASEMaterialInspector"
}