Shader "Custom/WindWakerWater" {
	Properties {
		_WaterCol ("Water Color", Vector) = (0.04,0.38,0.88,1)
		_Water2Col ("Water2 Color", Vector) = (0.04,0.35,0.78,1)
		_FoamCol ("Foam Color", Vector) = (0.8125,0.9609,0.9648,1)
		_DistortionSpeed ("Distortion Speed", Range(0, 10)) = 2
		_Tile ("Tile", Vector) = (5,5,0,0)
		_Height ("Wave Height", Range(0, 10)) = 2
		_WaveSize ("Wave Size", Vector) = (2,2,0,0)
		_WaveSpeed ("Wave Speed", Range(0, 10)) = 1
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
}