Shader "Piloto Studio/UberFX" {
	Properties {
		[HideInInspector] _EmissionColor ("Emission Color", Vector) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff ("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Enum(UnityEngine.Rendering.BlendMode)] _SourceBlendRGB ("Blend Mode", Float) = 10
		[Enum(UnityEngine.Rendering.CullMode)] _Culling ("Culling", Float) = 0
		_MainTex ("Main Texture", 2D) = "white" {}
		_MainTextureChannel ("Main Texture Channel", Vector) = (1,1,1,0)
		_MainAlphaChannel ("Main Alpha Channel", Vector) = (0,0,0,1)
		_MainTexturePanning ("Main Texture Panning ", Vector) = (0,0,0,0)
		_Desaturate ("Desaturate? ", Range(0, 1)) = 0
		[Toggle(_USESOFTALPHA_ON)] _UseSoftAlpha ("Use Soft Particles?", Float) = 0
		_SoftFadeFactor ("Soft Fade Factor", Range(0.1, 1)) = 0.1
		[Toggle(_USEALPHAOVERRIDE_ON)] _UseAlphaOverride ("Use Alpha Override", Float) = 0
		_AlphaOverride ("Alpha Override", 2D) = "white" {}
		_AlphaOverrideChannel ("Alpha Override Channel", Vector) = (0,0,0,1)
		_AlphaOverridePanning ("Alpha Override Panning", Vector) = (0,0,0,0)
		_DetailNoise ("Detail Noise", 2D) = "white" {}
		_DetailNoisePanning ("Detail Noise Panning", Vector) = (0,0,0,0)
		_DetailDistortionChannel ("Detail Distortion Channel", Vector) = (0,0,0,0)
		_DistortionIntensity ("Distortion Intensity", Range(0, 3)) = 2
		_DetailMultiplyChannel ("Detail Multiply Channel", Vector) = (0,0,0,0)
		_MultiplyNoiseDesaturation ("Multiply Noise Desaturation", Range(0, 1)) = 1
		_DetailAdditiveChannel ("Detail Additive Channel", Vector) = (0,0,0,0)
		_DetailDisolveChannel ("Detail Disolve Channel", Vector) = (1,0,0,0)
		_DetailVertexOffsetChannel ("Detail Vertex Offset Channel", Vector) = (1,0,0,0)
		[Toggle(_USERAMP_ON)] _UseRamp ("Use Color Ramping?", Float) = 0
		[HDR] _WhiteColor ("Highs", Vector) = (1,0.8950032,0,0)
		_MiddlePointPos ("Middle Point Position", Range(-1, 0.99)) = 0.5
		[HDR] _MidColor ("Middles", Vector) = (1,0.4447915,0,0)
		_MiddlePointPos1 ("Middle Point Position 2", Range(-1, 0.99)) = 0.5
		[HDR] _LastColor ("Lows", Vector) = (1,0,0,0)
		[Toggle(_USEUVOFFSET_ON)] _UseUVOffset ("Use UV Offset", Float) = 0
		[Toggle(_FRESNEL_ON)] _Fresnel ("Fresnel", Float) = 0
		_FresnelPower ("Fresnel Power", Float) = 1
		_FresnelScale ("Fresnel Scale", Float) = 1
		[HDR] _FresnelColor ("Fresnel Color", Vector) = (1,1,1,1)
		[Toggle(_DISABLEEROSION_ON)] _DisableErosion ("Disable Erosion", Float) = 0
		[Toggle(_USEPIXELATION_ON)] _UsePixelation ("Use Pixelation", Float) = 0
		_Resolution ("Resolution", Vector) = (64,64,0,0)
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
		[ToggleOff] [HideInInspector] _ReceiveShadows ("Receive Shadows", Float) = 1
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
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
}