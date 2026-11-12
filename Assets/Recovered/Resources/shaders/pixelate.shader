Shader "TransitionsPlus/Pixelate" {
	Properties {
		[HideInInspector] _T ("Progress", Range(0, 1)) = 0
		[HideInInspector] _MainTex ("Texture", 2D) = "white" {}
		[HideInInspector] _MaskTex ("Texture", 2D) = "white" {}
		[HideInInspector] _NoiseTex ("Noise", 2D) = "white" {}
		[HideInInspector] _FirstCameraTex ("First Camera Tex", 2D) = "white" {}
		[HideInInspector] _GradientTex ("Gradient Tex", 2D) = "white" {}
		[HideInInspector] _Color ("Color", Vector) = (0,0,0,1)
		[HideInInspector] _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.5
		[HideInInspector] _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.5
		[HideInInspector] _ToonIntensity ("Toon Intensity", Float) = 1
		[HideInInspector] _ToonDotIntensity ("Toon Dot Intensity", Float) = 1
		[HideInInspector] _AspectRatio ("Aspect Ratio", Float) = 1
		[HideInInspector] _Distortion ("Distortion", Float) = 1
		[HideInInspector] _ToonDotRadius ("Toon Dot Radius", Float) = 0
		[HideInInspector] _ToonDotCount ("Toon Dot Count", Float) = 0
		[HideInInspector] _Contrast ("Constrast", Float) = 1
		[HideInInspector] _CellDivisions ("Cell Divisions", Float) = 32
		[HideInInspector] _Spread ("Spread", Float) = 64
		[HideInInspector] _RotationMultiplier ("Rotation", Float) = 0
		[HideInInspector] _Rotation ("Rotation", Float) = 0
		[HideInInspector] _Splits ("Splits", Float) = 2
		[HideInInspector] _Seed ("Seed", Float) = 0
		[HideInInspector] _CentersCount ("Seed", Float) = 1
		[HideInInspector] _Center ("Center", Vector) = (0,0,0,1)
		[HideInInspector] _TimeMultiplier ("Time Multiplier", Float) = 1
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
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
}