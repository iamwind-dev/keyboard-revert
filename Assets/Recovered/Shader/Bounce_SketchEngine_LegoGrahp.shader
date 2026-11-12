Shader "Bounce/SketchEngine_LegoGrahp" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		[NoScaleOffset] _SketchPreviewBaseMap ("Preview Base Map", 2D) = "white" {}
		_SketchPreviewTilingOffset ("Preview Tiling Offset", Vector) = (1,1,0,0)
		[NoScaleOffset] _BaseMapArray ("Base Map Array", 2DArray) = "" {}
		[ToggleUI] _UseCombined ("Use Combined", Float) = 0
		[NoScaleOffset] [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Scale", Float) = 1
		[ToggleUI] _SketchPreviewUseVertexColor ("Preview Uses Vertex Color", Float) = 0
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
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

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}