Shader "UI/RadialReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress  ("Progress", Range(0,1)) = 0
        _Center    ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect    ("Aspect", Float) = 1
        _MaxRadius ("MaxRadius", Float) = 1
        _Softness  ("Softness", Float) = 0.05

        // UGUI Stencil
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 col:COLOR; };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;
            float _Progress, _Aspect, _MaxRadius, _Softness; float2 _Center;

            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.col=v.color*_Color; return o; }

            fixed4 frag(v2f i):SV_Target
            {
                float2 d = i.uv - _Center;
                d.y *= _Aspect;
                float dist = length(d);

                float radius = _Progress * _MaxRadius;
                // edge0 < edge1 로 계산(0일 때도 정상 동작)
                float e0 = max(0, radius - _Softness);
                float e1 = radius;
                float edge = smoothstep(e0, e1, dist);
                float mask = 1.0 - edge;

                fixed4 col = tex2D(_MainTex, i.uv) * i.col;
                col.a *= mask;
                #ifdef UNITY_UI_ALPHACLIP
                  clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
