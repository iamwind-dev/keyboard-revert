Shader "Recovery/MapSurface"
{
    Properties
    {
        _MainTex ("Recovered texture", 2D) = "white" {}
        _Color ("Recovered color", Color) = (1,1,1,1)
        _VertexColor ("Use recovered vertex colors", Float) = 0
        _RecoveryFade ("Gameplay fade", Float) = 1
        [HideInInspector] _KeyboardPress ("Pressed and released key", Vector) = (-1,0,-1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST, _Color;
            float _VertexColor;
            float _RecoveryFade;
            float4 _KeyboardPress;
            float _KeyboardPressClock;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float4 color:COLOR; };
            v2f vert(appdata v, uint vertexId : SV_VertexID)
            {
                v2f o;
                if (_KeyboardPress.x >= 0 || _KeyboardPress.z >= 0)
                {
                    float key = vertexId / 20u;
                    float depression = 0;
                    if (key == _KeyboardPress.x)
                        depression = smoothstep(0, .055, _KeyboardPressClock - _KeyboardPress.y);
                    else if (key == _KeyboardPress.z)
                        depression = 1 - smoothstep(0, .4, _KeyboardPressClock - _KeyboardPress.w);
                    v.vertex.y -= .12 * depression;
                }
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = lerp(float4(1,1,1,1), v.color, _VertexColor);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color * i.color;
                clip(c.a - .08);
                clip(_RecoveryFade - .008);
                float light = .65 + .35 * max(0, dot(normalize(i.normal), normalize(float3(.3,.85,-.4))));
                return fixed4(c.rgb * light, _RecoveryFade);
            }
            ENDCG
        }
    }
}
