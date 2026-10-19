Shader "Verdant/GodRays2D"
{
    Properties
    {
        _RayColor  ("Ray Color", Color) = (0.55, 0.8, 1.0, 1)
        _Intensity ("Intensity", Range(0,3)) = 1.0
        _Scale     ("Ray Density", Float) = 6
        _Tilt      ("Tilt (เอียง)", Float) = 0.5
        _Sharpness ("Sharpness", Range(1,8)) = 3
        _Falloff   ("Vertical Falloff", Range(0.2,4)) = 1.4
        _Speed     ("Drift Speed", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend One One  

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _RayColor;
                float  _Intensity;
                float  _Scale;
                float  _Tilt;
                float  _Sharpness;
                float  _Falloff;
                float  _Speed;
            CBUFFER_END

            float hash21(float2 p){ p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float vnoise(float2 p){
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1)), d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS);
                o.uv = IN.uv;
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float axis = IN.uv.x * _Scale + IN.uv.y * _Tilt + _Time.y * _Speed;

                float n = vnoise(float2(axis, IN.uv.y * 1.5)) * 0.65
                        + vnoise(float2(axis * 2.1, IN.uv.y * 2.7)) * 0.35;
                n = pow(saturate(n), _Sharpness);            // คมเป็นลำ

                float vFade = pow(saturate(IN.uv.y), _Falloff);
                float edge = smoothstep(0.0, 0.15, IN.uv.x) * smoothstep(1.0, 0.85, IN.uv.x);

                float intensity = n * vFade * edge * _Intensity;
                return half4(_RayColor.rgb * intensity, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
