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

                // compute chroma (ignore luminance). chroma = (R - Y, B - Y)
                float keyY = dot(key, float3(0.299, 0.587, 0.114));
                float2 keyCh = float2(key.r - keyY, key.b - keyY);

                float2 s00 = tex2D(_MainTex, uv + float2(-t.x, -t.y)).rgb.xy;
                float3 c00r = tex2D(_MainTex, uv + float2(-t.x, -t.y)).rgb;
                float c00y = dot(c00r, float3(0.299, 0.587, 0.114));
                float d00 = length(float2(c00r.r - c00y, c00r.b - c00y) - keyCh);

                float3 c01r = tex2D(_MainTex, uv + float2( 0.0,  -t.y)).rgb;
                float c01y = dot(c01r, float3(0.299, 0.587, 0.114));
                float d01 = length(float2(c01r.r - c01y, c01r.b - c01y) - keyCh);

                float3 c02r = tex2D(_MainTex, uv + float2( t.x,  -t.y)).rgb;
                float c02y = dot(c02r, float3(0.299, 0.587, 0.114));
                float d02 = length(float2(c02r.r - c02y, c02r.b - c02y) - keyCh);

                float3 c10r = tex2D(_MainTex, uv + float2(-t.x,  0.0)).rgb;
                float c10y = dot(c10r, float3(0.299, 0.587, 0.114));
                float d10 = length(float2(c10r.r - c10y, c10r.b - c10y) - keyCh);

                float3 c11r = tex2D(_MainTex, uv).rgb;
                float c11y = dot(c11r, float3(0.299, 0.587, 0.114));
                float d11 = length(float2(c11r.r - c11y, c11r.b - c11y) - keyCh);

                float3 c12r = tex2D(_MainTex, uv + float2( t.x,  0.0)).rgb;
                float c12y = dot(c12r, float3(0.299, 0.587, 0.114));
                float d12 = length(float2(c12r.r - c12y, c12r.b - c12y) - keyCh);

                float3 c20r = tex2D(_MainTex, uv + float2(-t.x,  t.y)).rgb;
                float c20y = dot(c20r, float3(0.299, 0.587, 0.114));
                float d20 = length(float2(c20r.r - c20y, c20r.b - c20y) - keyCh);

                float3 c21r = tex2D(_MainTex, uv + float2( 0.0,   t.y)).rgb;
                float c21y = dot(c21r, float3(0.299, 0.587, 0.114));
                float d21 = length(float2(c21r.r - c21y, c21r.b - c21y) - keyCh);

                float3 c22r = tex2D(_MainTex, uv + float2( t.x,   t.y)).rgb;
                float c22y = dot(c22r, float3(0.299, 0.587, 0.114));
                float d22 = length(float2(c22r.r - c22y, c22r.b - c22y) - keyCh);

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
