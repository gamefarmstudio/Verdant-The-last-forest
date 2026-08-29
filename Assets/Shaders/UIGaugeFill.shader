Shader "Verdant/UIGaugeFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FlowSpeed ("Flow Speed", Float) = 0.6
        _FlowScale ("Flow Scale", Float) = 5
        _FlowBright ("Flow Brightness", Range(0,1)) = 0.25
        _Glow ("Glow (emissive)", Range(0,2)) = 0.3
        _PulseSpeed ("Pulse Speed", Float) = 3
        _PulseAmt ("Pulse Amount", Range(0,0.5)) = 0.1
        _BubbleAmount ("Bubble Amount", Range(0,1)) = 0.4
        _BubbleSpeed ("Bubble Speed", Float) = 0.7
        _BubbleScale ("Bubble Scale", Float) = 8

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex:POSITION; float4 color:COLOR; float2 texcoord:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; float4 worldPosition:TEXCOORD1; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _FlowSpeed, _FlowScale, _FlowBright, _Glow, _PulseSpeed, _PulseAmt;
            float _BubbleAmount, _BubbleSpeed, _BubbleScale;

            float hash21(float2 p){ p = frac(p * float2(123.34, 345.45)); p += dot(p, p + 34.345); return frac(p.x * p.y); }
            float vnoise(float2 p){
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1)), d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 tex = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float t = _Time.y;
                float2 uv = IN.texcoord;

                float3 rgb = tex.rgb;

                // ของเหลวไหล (noise เลื่อนขึ้น)
                float flow = vnoise(float2(uv.x * _FlowScale, uv.y * _FlowScale - t * _FlowSpeed));
                rgb += flow * _FlowBright;

                // ฟองผุดขึ้น
                float2 bp = uv * _BubbleScale;
                bp.y -= t * _BubbleSpeed;
                float2 cell = floor(bp), f = frac(bp);
                float d = distance(f, float2(hash21(cell + 3.1), 0.5));
                float bub = smoothstep(0.25, 0.05, d) * step(hash21(cell), _BubbleAmount);
                rgb += bub;

                // เรืองแสง + เต้น
                rgb *= 1.0 + sin(t * _PulseSpeed) * _PulseAmt;
                rgb += tex.rgb * _Glow;

                half4 col = half4(rgb, tex.a);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
