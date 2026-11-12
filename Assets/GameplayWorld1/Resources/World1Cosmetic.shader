Shader "Recovery/World1Cosmetic"
{
    Properties
    {
        _MainTex ("Effect texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _DestinationBlend ("Destination blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha [_DestinationBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct Vertex { float4 position : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Varying { float4 position : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            Varying vert(Vertex input)
            {
                Varying output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }
            fixed4 frag(Varying input) : SV_Target { return tex2D(_MainTex, input.uv) * input.color; }
            ENDCG
        }
    }
}
