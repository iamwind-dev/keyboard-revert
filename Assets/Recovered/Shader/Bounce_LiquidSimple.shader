Shader "Bounce/LiquidSimple" {
	Properties {
		[ToggleUI] _UseTexture ("Use Texture (ignore Base Color)", Float) = 0
		_BaseColor ("Base Color (ink)", Vector) = (0.2,0.8,0.7,1)
		_BaseMap ("Base Texture", 2D) = "white" {}
		[Header(Fill driven by script (LOCAL Y))] _FillAmount ("Fill Amount local Y", Float) = 0
		_WobbleX ("Wobble X", Float) = 0
		_WobbleZ ("Wobble Z", Float) = 0
		_Frequency ("Wave Frequency", Float) = 8
		_Amplitude ("Wave Amplitude", Range(0, 0.5)) = 0.05
		[Header(Surface and Edge Foam)] _TopLighten ("Top Surface Lighten", Range(0, 1)) = 0.15
		_EdgeWidth ("Edge Foam Width", Range(0, 0.5)) = 0.04
		_EdgeStrength ("Edge Foam Strength", Range(0, 1)) = 0.6
		[Header(Liquid Look)] _Glossiness ("Glossiness", Range(0, 1)) = 0.85
		_SpecStrength ("Specular Strength", Range(0, 2)) = 0.6
		[Header(Fresnel Rim)] _RimStrength ("Rim Strength", Range(0, 1)) = 0.5
		_RimPower ("Rim Power", Range(0.5, 12)) = 4
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