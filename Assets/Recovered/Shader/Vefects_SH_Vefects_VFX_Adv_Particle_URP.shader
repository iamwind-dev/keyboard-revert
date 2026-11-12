Shader "Vefects/SH_Vefects_VFX_Adv_Particle_URP" {
	Properties {
		[HideInInspector] _EmissionColor ("Emission Color", Vector) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff ("Alpha Cutoff ", Range(0, 1)) = 0.5
		_Noise_01_Texture ("Noise_01_Texture", 2D) = "white" {}
		_ColorGradientMask ("Color Gradient Mask", 2D) = "white" {}
		_Mask_Texture ("Mask_Texture", 2D) = "white" {}
		_ColorBottom ("Color Bottom", Vector) = (1,1,1,0)
		_ColorTop ("Color Top", Vector) = (1,1,1,0)
		_NoiseDistortion_Texture ("NoiseDistortion_Texture", 2D) = "white" {}
		_Noise_01_Scale ("Noise_01_Scale", Vector) = (1,1,0,0)
		_GradientColor_Scale ("GradientColor_Scale", Vector) = (1,1,0,0)
		_NoiseDistortion_Scale ("NoiseDistortion_Scale", Vector) = (1,1,0,0)
		_Noise_01_Speed ("Noise_01_Speed", Vector) = (0,0,0,0)
		_Mask_Scale ("Mask_Scale", Vector) = (1,1,0,0)
		_Mask_Offset ("Mask_Offset", Vector) = (0,0,0,0)
		_NoiseDistortion_Speed ("NoiseDistortion_Speed", Vector) = (0,0,0,0)
		_GradientColor_Speed ("GradientColor_Speed", Vector) = (0,0,0,0)
		_UseColor ("UseColor", Range(0, 1)) = 0
		_Mask_Multiply ("Mask_Multiply", Float) = 1
		_Noises_Multiply ("Noises_Multiply", Float) = 1
		_Mask_Power ("Mask_Power", Float) = 1
		_Noises_Power ("Noises_Power", Float) = 1
		_DistortionAmount ("Distortion Amount", Float) = 0
		_DistortionMaskIntensity ("Distortion Mask Intensity", Float) = 1
		_WindSpeed ("Wind Speed", Float) = 1
		_Erosion_Intensity ("Erosion_Intensity", Float) = 0
		_Erosion_Sub ("Erosion_Sub", Float) = 0
		[Space(33)] [Header(Emission)] [Space(13)] _EmissiveIntensity ("Emissive Intensity", Float) = 1
		[Space(33)] [Header(Depth Fade)] [Space(13)] _DepthFade ("Depth Fade", Float) = 0
		[Space(33)] [Header(Cam Offset)] [Space(13)] _CameraOffset ("Camera Offset", Float) = 0
		[Space(33)] [Header(AR)] [Space(13)] _Cull ("Cull", Float) = 2
		_Src ("Src", Float) = 1
		_Dst ("Dst", Float) = 1
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
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
}