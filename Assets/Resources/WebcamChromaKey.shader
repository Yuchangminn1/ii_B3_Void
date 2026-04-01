Shader "Custom/WebcamChromaKey"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (0, 1, 0, 1)
        _Threshold ("Threshold", Range(0,1)) = 0.4
        _Smooth ("Smoothness", Range(0,1)) = 0.1
        _Spill ("Spill Reduction (unused)", Range(0,1)) = 0.2
        _Mirror ("Mirror Horizontal", Float) = 0
        _VFlip ("Vertical Flip", Float) = 0
        _OpaqueToBlack ("Opaque to Black", Float) = 0
        _EdgeContrast ("Edge Contrast", Range(1,10)) = 1
        _NoiseFilter ("Noise Filter", Range(0,1)) = 1
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _MidValueFilter ("Mid Value Filter", Range(0,0.49)) = 0.15
        _LeftClip ("Left Clip", Range(0,1)) = 0
        _RightClip ("Right Clip", Range(0,1)) = 0
        _TopClip ("Top Clip", Range(0,1)) = 0
        _BottomClip ("Bottom Clip", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _KeyColor;
            float _Threshold;
            float _Smooth;
            float _Spill;
            float _Mirror;
            float _VFlip;
            float _OpaqueToBlack;
            float _EdgeContrast;
            float _NoiseFilter;
            float _AlphaCutoff;
            float _MidValueFilter;
            float _LeftClip;
            float _RightClip;
            float _TopClip;
            float _BottomClip;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);

                if (_Mirror > 0.5) uv.x = 1.0 - uv.x;
                if (_VFlip > 0.5) uv.y = 1.0 - uv.y;

                o.uv = uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;
                float2 uv = i.uv;
                float3 key = _KeyColor.rgb;

                float d00 = distance(tex2D(_MainTex, uv + float2(-t.x, -t.y)).rgb, key);
                float d01 = distance(tex2D(_MainTex, uv + float2( 0.0,  -t.y)).rgb, key);
                float d02 = distance(tex2D(_MainTex, uv + float2( t.x,  -t.y)).rgb, key);
                float d10 = distance(tex2D(_MainTex, uv + float2(-t.x,  0.0)).rgb, key);
                float d11 = distance(tex2D(_MainTex, uv).rgb,                          key);
                float d12 = distance(tex2D(_MainTex, uv + float2( t.x,  0.0)).rgb, key);
                float d20 = distance(tex2D(_MainTex, uv + float2(-t.x,  t.y)).rgb, key);
                float d21 = distance(tex2D(_MainTex, uv + float2( 0.0,   t.y)).rgb, key);
                float d22 = distance(tex2D(_MainTex, uv + float2( t.x,   t.y)).rgb, key);

                float avgDist = (d00 + d01 + d02 + d10 + d11 + d12 + d20 + d21 + d22) / 9.0;
                float filteredDist = lerp(d11, avgDist, _NoiseFilter);

                float alpha = saturate((filteredDist - _Threshold) / max(_Smooth, 1e-5));
                alpha = saturate((alpha - 0.5) * _EdgeContrast + 0.5);

                float midMin = 0.5 - _MidValueFilter;
                float midMax = 0.5 + _MidValueFilter;
                float isMid = step(midMin, alpha) * step(alpha, midMax);
                alpha = lerp(alpha, 0.0, isMid);
                alpha = step(_AlphaCutoff, alpha);
                alpha *= step(_LeftClip, uv.x);
                alpha *= step(uv.x, 1.0 - _RightClip);
                alpha *= step(_BottomClip, uv.y);
                alpha *= step(uv.y, 1.0 - _TopClip);

                float3 c = tex2D(_MainTex, uv).rgb;
                float3 rgb = (_OpaqueToBlack > 0.5) ? float3(0.0, 0.0, 0.0) : c;

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
