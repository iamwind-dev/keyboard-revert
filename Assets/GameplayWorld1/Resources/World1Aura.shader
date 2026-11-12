Shader "Recovery/World1Aura"
{
    Properties
    {
        _MainTex ("Texture / opacity", 2D) = "white" {}
        _Flow ("Aura flow", 2D) = "gray" {}
        _Mask ("Aura mask", 2D) = "gray" {}
        _Color_Texture ("Channel cut shape", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DistortionSpeedXYPowerZ ("Distortion speed and power", Vector) = (0,0,0,0)
        _Speed_Color_XY_Main_ZW ("Channel cut texture speeds", Vector) = (0,0,0,0)
        _Use_Color_texture ("Use channel cut shape", Float) = 0
        _Emission ("Emission", Float) = 1
        _AuraMode ("0 particle, 1 aura rim, 2 channel cut", Float) = 0
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
            sampler2D _MainTex, _Flow, _Mask, _Color_Texture;
            float4 _MainTex_ST, _Flow_ST, _Mask_ST, _Color_Texture_ST;
            float4 _DistortionSpeedXYPowerZ, _Speed_Color_XY_Main_ZW;
            float _AuraMode, _Use_Color_texture, _Emission;
            fixed4 _Color;

            struct Vertex
            {
                float4 position : POSITION;
                float4 uv : TEXCOORD0;
                float4 custom1 : TEXCOORD1;
                float4 custom2 : TEXCOORD2;
                fixed4 color : COLOR;
            };
            struct Varying
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 cutData : TEXCOORD1;
                fixed4 color : COLOR;
            };
            Varying vert(Vertex input)
            {
                Varying output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv.xy;
                output.cutData = float3(input.custom1.x, input.custom1.z, input.custom2.x);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(Varying input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                if (_AuraMode == 1)
                {
                    // The shipped texture contains one side of a luminance profile.
                    // Mirror it across the billboard and animate its rim with the source flow and mask.
                    float y = saturate((uv.y - .045) / .91);
                    float radius = max(.035, pow(saturate(sin(UNITY_PI * y)), .57) * (1.05 - y * .13));
                    float side = abs(uv.x * 2 - 1);
                    float2 flowUV = TRANSFORM_TEX(uv, _Flow) + _DistortionSpeedXYPowerZ.xy * time;
                    float2 maskUV = TRANSFORM_TEX(uv, _Mask) + float2(0, -time * .48);
                    float flow = tex2D(_Flow, flowUV).r - .5;
                    float mask = tex2D(_Mask, maskUV).r;
                    float edge = side / radius + (flow + (mask - .5) * .65) * _DistortionSpeedXYPowerZ.z;
                    float sample = tex2D(_MainTex, float2(saturate(edge), frac(y * 1.7 + time * .22))).r;
                    float rim = smoothstep(.52, .84, sample) * (1 - smoothstep(.98, 1.02, edge));
                    float interior = smoothstep(.14, .4, sample) * (1 - rim) * .075;
                    float cap = smoothstep(0, .018, y) * (1 - smoothstep(.982, 1, y));
                    float3 color = saturate(input.color.rgb * lerp(1, _Emission, .30));
                    return fixed4(color, (rim + interior) * input.color.a * cap);
                }
                if (_AuraMode == 2)
                {
                    float4 textureSample = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex) + time * _Speed_Color_XY_Main_ZW.zw);
                    float4 shape = tex2D(_Color_Texture, TRANSFORM_TEX(uv, _Color_Texture) + time * _Speed_Color_XY_Main_ZW.xy);
                    float threshold = .51 + saturate(input.cutData.x) * .33;
                    float noiseCut = smoothstep(threshold, threshold + .16, textureSample.r);
                    float2 margin = smoothstep(0, .16, uv) * (1 - smoothstep(.84, 1, uv));
                    float cut;
                    if (_Use_Color_texture > .5)
                    {
                        float profile = shape.r * shape.a;
                        float contour = smoothstep(.51, .63, profile) * (1 - smoothstep(.76, .91, profile));
                        cut = input.cutData.z > 4
                            ? contour * noiseCut
                            : smoothstep(.66, .92, profile) * noiseCut;
                    }
                    else
                    {
                        float channel = textureSample.r - .62 * textureSample.g;
                        cut = smoothstep(.28 + input.cutData.x * .32, .55 + input.cutData.x * .32, channel) * textureSample.a;
                    }
                    float darkest = 1 - smoothstep(.14, .24, max(input.color.r, max(input.color.g, input.color.b)));
                    float3 color = saturate(input.color.rgb * _Emission);
                    color = lerp(color, float3(.73, .75, .79), darkest * .68);
                    return fixed4(color, cut * input.color.a * margin.x * margin.y);
                }
                fixed4 source = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex));
                return fixed4(source.rgb * input.color.rgb * _Emission, source.a * input.color.a);
            }
            ENDCG
        }
    }
}
