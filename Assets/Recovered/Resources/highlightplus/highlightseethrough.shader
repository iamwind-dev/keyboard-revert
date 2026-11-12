Shader "HighlightPlus/Geometry/SeeThrough" {
	Properties {
		_MainTex ("Texture", any) = "white" {}
		_SeeThrough ("See Through", Range(0, 1)) = 0.8
		_SeeThroughTintColor ("See Through Tint Color", Vector) = (1,0,0,0.8)
		_SeeThroughNoise ("Noise", Float) = 1
		_Color ("Color", Vector) = (1,1,1,1)
		_CutOff ("CutOff", Float) = 0.5
		_SeeThroughStencilRef ("Stencil Ref", Float) = 2
		_SeeThroughStencilComp ("Stencil Comp", Float) = 5
		_SeeThroughStencilPassOp ("Stencil Pass Operation", Float) = 0
		_SeeThroughDepthOffset ("Depth Offset", Float) = 0
		_SeeThroughMaxDepth ("Max Depth", Float) = 0
		_SeeThroughTexture ("Mask Texture", 2D) = "white" {}
		_SeeThroughTextureScale ("Mask Texture Scale", Float) = 1
		_Cull ("Cull Mode", Float) = 2
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