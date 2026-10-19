Shader "Verdant/WaterClear2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (mask only)", 2D) = "white" {}

        _ShallowColor ("Shallow Color", Color) = (0.30, 0.72, 0.85, 0.55)
        _DeepColor    ("Deep Color",    Color) = (0.04, 0.18, 0.42, 0.92)
        _ToxicColor   ("Toxic Color",   Color) = (0.40, 0.20, 0.55, 0.92)
        _Pollution    ("Pollution 0..1", Range(0,1)) = 0.7

        _SurfaceX   ("Surface World X (ขวา = ตื้น)", Float) = 8
        _DepthRange ("Depth Range (units)", Float) = 16

        _Pixelate ("Pixelate (0=off, = PPU)", Float) = 0

        _CausticColor    ("Caustic Color", Color) = (0.6, 0.95, 1.0, 1)
        _CausticScale    ("Caustic Scale", Float) = 0.6
        _CausticSpeed    ("Caustic Speed", Float) = 0.25
        _CausticSharp    ("Caustic Sharpness", Range(1,12)) = 6
        _CausticStrength ("Caustic Strength", Range(0,2)) = 0.6

        _WaveScale     ("Wave Scale", Float) = 1.2
        _WaveSpeed     ("Wave Speed", Float) = 1.0
        _WaveStrength  ("Wave Distort", Range(0,1)) = 0.3
        _WaveHighlight ("Wave Highlight", Range(0,1)) = 0.15

        _FoamColor    ("Foam Color", Color) = (1,1,1,1)
        _FoamWidth    ("Foam Width (depth)", Range(0,0.4)) = 0.08
        _FoamStrength ("Foam Strength", Range(0,2)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 wpos:TEXCOORD1; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShallowColor, _DeepColor, _ToxicColor, _CausticColor, _FoamColor;
                float  _Pollution, _SurfaceX, _DepthRange, _Pixelate;
                float  _CausticScale, _CausticSpeed, _CausticSharp, _CausticStrength;
                float  _WaveScale, _WaveSpeed, _WaveStrength, _WaveHighlight;
                float  _FoamWidth, _FoamStrength;
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
                o.color = IN.color;
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                o.wpos = TransformObjectToWorld(IN.positionOS).xy;
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                float2 wp = _Pixelate > 0.001 ? floor(IN.wpos * _Pixelate) / _Pixelate : IN.wpos;
                float depth = saturate((_SurfaceX - IN.wpos.x) / max(0.001, _DepthRange));
                float3 col = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);
                float  a   = lerp(_ShallowColor.a,   _DeepColor.a,   depth);

                float t = _Time.y;

                float2 warp = float2(sin(wp.y * _WaveScale + t * _WaveSpeed),
                                     cos(wp.x * _WaveScale - t * _WaveSpeed)) * _WaveStrength;


                float2 cp = wp * _CausticScale + warp;
                float n = vnoise(cp + float2( t*_CausticSpeed,  t*_CausticSpeed*0.6))
                        + vnoise(cp*1.4 + float2(-t*_CausticSpeed*0.7, t*_CausticSpeed));
                float caustic = pow(saturate(1.0 - abs(n - 1.0)), _CausticSharp);
                caustic *= (1.0 - depth * 0.7);
                col += caustic * _CausticStrength * _CausticColor.rgb;

                float crest = sin((wp.x + wp.y * 0.35) * _WaveScale * 0.6 - t * _WaveSpeed * 1.5) * 0.5 + 0.5;
                col += pow(crest, 6.0) * _WaveHighlight * (1.0 - depth * 0.5) * _CausticColor.rgb;

                float wob = (vnoise(wp * 2.5 + t * _WaveSpeed) - 0.5) * _FoamWidth * 0.6;
                float foam = 1.0 - smoothstep(0.0, _FoamWidth, depth + wob);
                foam = saturate(foam) * _FoamStrength;
                col += foam * _FoamColor.rgb;
                a = saturate(a + foam * _FoamColor.a);

                col = lerp(col, _ToxicColor.rgb, saturate(_Pollution) * 0.85);

                col *= IN.color.rgb;
                a *= mask * IN.color.a;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
