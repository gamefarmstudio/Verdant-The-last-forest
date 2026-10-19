Shader "Verdant/Lava2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (mask only)", 2D) = "white" {}

        [HDR] _CrustColor ("Crust Color (หินดำ)",  Color) = (0.16, 0.05, 0.03, 1)
        [HDR] _HotColor   ("Hot Color (ส้ม)",      Color) = (1.0, 0.40, 0.08, 1)
        [HDR] _CoreColor  ("Core Color (เหลือง)",   Color) = (1.0, 0.72, 0.25, 1)
        [HDR] _ToxicColor ("Toxic Color (พิษ)",    Color) = (0.5, 0.7, 0.1, 1)
        _Pollution ("Pollution 0..1", Range(0,1)) = 0

        _Scale     ("Lava Scale", Float) = 0.5
        _FlowSpeed ("Flow Speed (xy)", Vector) = (0.04, 0.02, 0, 0)
        _WarpScale ("Warp Scale", Float) = 0.3
        _WarpStrength ("Warp Strength (churn)", Range(0,2)) = 0.5

        _Posterize ("Posterize (ก้อนสี)", Range(2,10)) = 4
        _Pixelate  ("Pixelate (0=off, = PPU)", Float) = 0

        _Pulse         ("Glow Pulse Speed", Float) = 2
        _PulseStrength ("Glow Pulse Strength", Range(0,1)) = 0.2

        _EdgeX     ("Front World X (ขับด้วย driver)", Float) = 100
        _EdgeSoft  ("Front Softness", Float) = 1.0
        _EdgeWobble("Front Wobble (หยัก)", Float) = 1.0
        _FrontGlow ("Front Glow (ขอบเรือง)", Range(0,3)) = 1.2
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
                float4 _CrustColor, _HotColor, _CoreColor, _ToxicColor;
                float  _Pollution, _Scale, _WarpScale, _WarpStrength, _Posterize, _Pixelate;
                float  _Pulse, _PulseStrength;
                float4 _FlowSpeed;
                float  _EdgeX, _EdgeSoft, _EdgeWobble, _FrontGlow;
            CBUFFER_END

            float hash21(float2 p){ p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float vnoise(float2 p){
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1)), d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }
            float fbm(float2 p){
                float s = 0.0, amp = 0.6;
                for (int k = 0; k < 3; k++){ s += vnoise(p) * amp; p *= 2.0; amp *= 0.5; }
                return s;
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
                float t = _Time.y;

                float2 wc = wp * _WarpScale + t * 0.12;
                float2 warp = (float2(vnoise(wc), vnoise(wc + 5.2)) - 0.5) * _WarpStrength;
                float2 p = wp * _Scale + _FlowSpeed.xy * t + warp;
                float n = fbm(p);

                float nq = floor(n * _Posterize) / _Posterize;

                float3 col = _CrustColor.rgb;
                col = lerp(col, _HotColor.rgb,  step(0.42, nq));
                col = lerp(col, _CoreColor.rgb, step(0.74, nq));

                float glow = 0.5 + 0.5 * sin(t * _Pulse + nq * 8.0);
                col += _HotColor.rgb * glow * _PulseStrength * step(0.6, nq);

                col = lerp(col, _ToxicColor.rgb, saturate(_Pollution) * 0.5);

                float edge = _EdgeX + (vnoise(wp * 0.4 + t * 0.08) - 0.5) * _EdgeWobble;
                float cover = smoothstep(edge + _EdgeSoft, edge - _EdgeSoft, IN.wpos.x);
                float front = smoothstep(_EdgeSoft, 0.0, abs(IN.wpos.x - edge));
                col += _CoreColor.rgb * front * _FrontGlow;

                col *= IN.color.rgb;
                float a = mask * IN.color.a * cover;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
