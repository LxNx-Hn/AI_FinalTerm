Shader "Custom/UIEdgeFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 가장자리 페이드 폭 (0=없음, 0.3=30% 가장자리 페이드)
        _EdgeFadeX ("Edge Fade X", Range(0,0.5)) = 0.18
        _EdgeFadeY ("Edge Fade Y", Range(0,0.5)) = 0.22

        // 채도 감소 (1=원본, 0=흑백)
        _Saturation ("Saturation", Range(0,1)) = 0.80

        // 파란 회색 틴트 강도
        _BlueTint ("Blue-Gray Tint", Range(0,0.3)) = 0.08

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half2  mask     : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;
            float4    _MainTex_ST;
            float     _UIMaskSoftnessX;
            float     _UIMaskSoftnessY;
            float     _EdgeFadeX;
            float     _EdgeFadeY;
            float     _Saturation;
            float     _BlueTint;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;
                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.mask = (v.vertex.xy - _ClipRect.xy) * 2.0 / (_ClipRect.zw - _ClipRect.xy) - 1.0;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // 가장자리 페이드 (smoothstep으로 부드럽게)
                float2 uv = IN.texcoord;
                float fadeX = smoothstep(0.0, _EdgeFadeX, uv.x) * smoothstep(1.0, 1.0 - _EdgeFadeX, uv.x);
                float fadeY = smoothstep(0.0, _EdgeFadeY, uv.y) * smoothstep(1.0, 1.0 - _EdgeFadeY, uv.y);
                // 하단을 더 많이 페이드 (난간 방향)
                float fadeYBottom = smoothstep(0.0, _EdgeFadeY * 1.5, uv.y);
                float edgeFactor = fadeX * min(fadeY, fadeYBottom);
                color.a *= edgeFactor;

                // 채도 감소
                float luma = dot(color.rgb, float3(0.299, 0.587, 0.114));
                color.rgb = lerp(float3(luma, luma, luma), color.rgb, _Saturation);

                // 파란 회색 틴트 (하늘에 스며드는 느낌)
                float3 blueTintColor = float3(0.55, 0.62, 0.78);
                color.rgb = lerp(color.rgb, blueTintColor, _BlueTint);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask)) * 1e5);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
