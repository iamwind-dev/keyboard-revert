Shader "Vefects/SH_Vefects_VFX_URP_Trail_New" {
	Properties {
		[HideInInspector] _EmissionColor ("Emission Color", Vector) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff ("Alpha Cutoff ", Range(0, 1)) = 0.5
		_EmissiveIntensity ("Emissive Intensity", Float) = 1
		_AlphaAffectsOpacity ("Alpha Affects Opacity", Float) = 1
		_OverallSpeed ("Overall Speed", Float) = 1
		[Space(33)] [Header(Color)] [Space(13)] _Color ("Color", 2D) = "white" {}
		_ColorUVScale ("Color UV Scale", Vector) = (1,1,0,0)
		_ColorPanSpeed ("Color Pan Speed", Vector) = (0,0,0,0)
		_Color01 ("Color 01", Vector) = (1,1,1,0)
		_Color02 ("Color 02", Vector) = (1,1,1,0)
		_ColorSmoothstep ("Color Smoothstep", Float) = 0
		_ColorSmoothstepSmoothness ("Color Smoothstep Smoothness", Float) = 1
		[Space(33)] [Header(Distortion)] [Space(13)] _Distortion ("Distortion", 2D) = "white" {}
		_DistortionUVScale ("Distortion UV Scale", Vector) = (1,1,0,0)
		_DistortionPanSpeed ("Distortion Pan Speed", Vector) = (0,0,0,0)
		_DistortionAmount ("Distortion Amount", Float) = 0.1
		[Space(33)] [Header(Erosion)] [Space(13)] _Erosion ("Erosion", 2D) = "white" {}
		_ErosionUVScale ("Erosion UV Scale", Vector) = (1,1,0,0)
		_ErosionPanSpeed ("Erosion Pan Speed", Vector) = (0,0,0,0)
		_ErosionSmoothstep ("Erosion Smoothstep", Float) = 0
		_ErosionSmoothstepSmoothness ("Erosion Smoothstep Smoothness", Float) = 1
		[Space(33)] [Header(Mask)] [Space(13)] _Mask ("Mask", 2D) = "white" {}
		_MaskUVScale ("Mask UV Scale", Vector) = (1,1,0,0)
		_MaskPanSpeed ("Mask Pan Speed", Vector) = (0,0,0,0)
		_MaskDistortionIntensity ("Mask Distortion Intensity", Float) = 1
		_MaskSmoothstep ("Mask Smoothstep", Float) = 0
		_MaskSmoothstepSmoothness ("Mask Smoothstep Smoothness", Float) = 1
		[Space(33)] [Header(AR)] [Space(13)] _Cull ("Cull", Float) = 2
		_Src ("Src", Float) = 5
		_Dst ("Dst", Float) = 10
		_ZWrite ("ZWrite", Float) = 0
		_ZTest ("ZTest", Float) = 2
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
		[ToggleOff] [HideInInspector] _ReceiveShadows ("Receive Shadows", Float) = 1
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
	Fallback "Hidden/InternalErrorShader"
	//CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
}