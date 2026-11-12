Shader "Game/URP_Mobile_Diffuse_KeyVertexAlphabetPress_Fade" {
	Properties {
		_BaseMap ("Base Map", 2D) = "white" {}
		_BaseColor ("Base Color", Vector) = (1,1,1,1)
		_AlphabetTex ("Alphabet Atlas", 2D) = "white" {}
		_PressTex ("Press Texture", 2D) = "black" {}
		_PressDepth ("Press Depth", Float) = 0.18
		_PaintTex ("Paint Texture", 2D) = "black" {}
		_PaintPatternTex ("Paint Pattern Index Texture", 2D) = "black" {}
		_PaintPatternStrengthTex ("Paint Pattern Strength Texture", 2D) = "black" {}
		_InkPatternAtlas ("Ink Pattern Atlas", 2D) = "black" {}
		_InkPatternAtlasGrid ("Ink Pattern Atlas Grid (cols,rows)", Vector) = (1,1,0,0)
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Universal Render Pipeline/Lit"
}