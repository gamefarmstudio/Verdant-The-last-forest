Shader "Verdant/LogoGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Logo / Mask", 2D) = "white" {}

        [HDR] _ColorTop    ("Gradient Top (เขียว)", Color) = (0.52, 0.94, 0.60, 1)
        [HDR] _ColorBottom ("Gradient Bottom (เทา)", Color) = (0.22, 0.88, 0.75, 1)
        _OutlineColor      ("Outline Color", Color) = (0.04, 0.13, 0.09, 1)

        _FlowSpeed ("Gradient Flow Speed", Float) = 0.6
        _WaveFreq  ("Gradient Wave Freq", Float) = 4
        _WaveAmp   ("Gradient Wave Amp", Range(0,0.5)) = 0.08

        [HDR] _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineSpeed    ("Shine Speed", Float) = 0.35
        _ShineWidth    ("Shine Width", Range(0.01,0.5)) = 0.12
        _ShineStrength ("Shine Strength", Range(0,3)) = 1.0

        _Glow      ("Fill Glow (emissive)", Range(0,3)) = 0.5
        _PulseSpeed("Glow Pulse Speed", Float) = 2
        _PulseAmt  ("Glow Pulse Amount", Range(0,0.6)) = 0.12

        [HDR] _GlowColor ("Outer Glow Color", Color) = (0.30, 1.0, 0.55, 1)
        _OuterGlow   ("Outer Glow Strength", Range(0,3)) = 1.4
        _GlowRadius  ("Outer Glow Radius (texels)", Range(0,20)) = 7
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
            struct Varyings   { float4 positionHCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ColorTop, _ColorBottom, _OutlineColor, _ShineColor, _GlowColor;
                float  _FlowSpeed, _WaveFreq, _WaveAmp;
                float  _ShineSpeed, _ShineWidth, _ShineStrength;
                float  _Glow, _PulseSpeed, _PulseAmt;
                float  _OuterGlow, _GlowRadius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS);
                o.color = IN.color;
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float lum = max(tex.r, max(tex.g, tex.b));
                float2 uv = IN.uv;
                float t = _Time.y;

                float gy = saturate(uv.y + sin(uv.x * _WaveFreq + t * _FlowSpeed) * _WaveAmp
                                        + sin(t * _FlowSpeed) * 0.06);
                float3 grad = lerp(_ColorBottom.rgb, _ColorTop.rgb, gy);

                float sweep = frac((uv.x - uv.y) * 0.5 - t * _ShineSpeed);
                float band = smoothstep(_ShineWidth, 0.0, abs(sweep - 0.5));
                grad += band * _ShineStrength * _ShineColor.rgb;

                float fill = smoothstep(0.35, 0.80, lum);
                float3 letterRGB = lerp(_OutlineColor.rgb, grad, fill);
                letterRGB *= 1.0 + sin(t * _PulseSpeed) * _PulseAmt;
                letterRGB += grad * _Glow * fill;
                float letterA = tex.a;

                float ga = 0.0;
                float2 tsz = _MainTex_TexelSize.xy * _GlowRadius;
                const float2 dirs[8] = {
                    float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
                    float2(0.7,0.7), float2(-0.7,0.7), float2(0.7,-0.7), float2(-0.7,-0.7)
                };
                [unroll] for (int i = 0; i < 8; i++)
                    ga = max(ga, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + dirs[i]*tsz).a);
                ga *= (0.8 + 0.2 * sin(t * _PulseSpeed));           // เต้นเบาๆ
                float glowA = saturate(ga * _OuterGlow) * (1.0 - letterA);

                float3 outRGB = lerp(_GlowColor.rgb, letterRGB, letterA);
                float  outA   = max(letterA, glowA);
                return half4(outRGB * IN.color.rgb, outA * IN.color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
