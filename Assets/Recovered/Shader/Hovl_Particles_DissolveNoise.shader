Shader "Hovl/Particles/DissolveNoise" {
	Properties {
		_MainTex ("MainTex", 2D) = "white" {}
		_TextureNoise ("Texture Noise", 2D) = "white" {}
		_Dissolvenoise ("Dissolve noise", 2D) = "white" {}
		_NoisespeedXYEmissonZPowerW ("Noise speed XY / Emisson Z / Power W", Vector) = (0.5,0,2,1)
		_DissolvespeedXY ("Dissolve speed XY", Vector) = (0,0,0,0)
		_Maincolor ("Main color", Vector) = (0.7609469,0.8547776,0.9433962,1)
		_Noisecolor ("Noise color", Vector) = (0.2470588,0.3012382,0.3607843,1)
		_Dissolvecolor ("Dissolve color", Vector) = (1,1,1,1)
		[Toggle] _Usetexturecolor ("Use texture color", Float) = 0
		[Toggle] _Usetexturedissolve ("Use texture dissolve", Float) = 0
		_Opacity ("Opacity", Range(0, 1)) = 1
		[Toggle] _Usedepth ("Use depth?", Float) = 0
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
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
}