Shader "Custom/WebcamChromaKey"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (0, 1, 0, 1)
        _Threshold ("Threshold", Range(0,1)) = 0.4
        _Smooth ("Smoothness", Range(0,1)) = 0.1

        _KeyColor2 ("Key Color 2", Color) = (0, 0, 0, 0)
        _Threshold2 ("Threshold 2", Range(0,1)) = 0.4
        _Smooth2 ("Smoothness 2", Range(0,1)) = 0.1
        _UseSecondKey ("Use Second Key", Float) = 0
        
        _Spill ("Spill Reduction (unused)", Range(0,1)) = 0.2
        _Mirror ("Mirror Horizontal", Float) = 0
        _VFlip ("Vertical Flip", Float) = 0
        _OpaqueToBlack ("Opaque to Black", Float) = 0
        _EdgeContrast ("Edge Contrast", Range(1,10)) = 1
        _NoiseFilter ("Noise Filter", Range(0,1)) = 1
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

            float4 _KeyColor2;
            float _Threshold2;
            float _Smooth2;
            float _UseSecondKey;

            float _Spill;
            float _Mirror;
            float _VFlip;
            float _OpaqueToBlack;
            float _EdgeContrast;
            float _NoiseFilter;

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

                // 셰이더 차원에서 좌우/상하 반전 처리
                if (_Mirror > 0.5) uv.x = 1.0 - uv.x;
                if (_VFlip > 0.5) uv.y = 1.0 - uv.y;

                o.uv = uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float keyLuma = dot(_KeyColor.rgb, float3(0.299, 0.587, 0.114));

                float2 t = _MainTex_TexelSize.xy;
                float2 uv = i.uv;

                // 3x3 평균으로 자잘한 노이즈를 먼저 완화
                float l00 = dot(tex2D(_MainTex, uv + float2(-t.x, -t.y)).rgb, float3(0.299, 0.587, 0.114));
                float l01 = dot(tex2D(_MainTex, uv + float2( 0.0,  -t.y)).rgb, float3(0.299, 0.587, 0.114));
                float l02 = dot(tex2D(_MainTex, uv + float2( t.x,  -t.y)).rgb, float3(0.299, 0.587, 0.114));
                float l10 = dot(tex2D(_MainTex, uv + float2(-t.x,  0.0)).rgb, float3(0.299, 0.587, 0.114));
                float l11 = dot(tex2D(_MainTex, uv).rgb,                          float3(0.299, 0.587, 0.114));
                float l12 = dot(tex2D(_MainTex, uv + float2( t.x,  0.0)).rgb, float3(0.299, 0.587, 0.114));
                float l20 = dot(tex2D(_MainTex, uv + float2(-t.x,  t.y)).rgb, float3(0.299, 0.587, 0.114));
                float l21 = dot(tex2D(_MainTex, uv + float2( 0.0,   t.y)).rgb, float3(0.299, 0.587, 0.114));
                float l22 = dot(tex2D(_MainTex, uv + float2( t.x,   t.y)).rgb, float3(0.299, 0.587, 0.114));

                float avgLuma = (l00 + l01 + l02 + l10 + l11 + l12 + l20 + l21 + l22) / 9.0;
                float filteredCenter = lerp(l11, avgLuma, _NoiseFilter);

                // 3x3 다수결(majority vote): 흰/투명 점 노이즈, 검정 점 노이즈를 모두 줄임
                float whiteCount = 0.0;
                whiteCount += step(keyLuma, l00);
                whiteCount += step(keyLuma, l01);
                whiteCount += step(keyLuma, l02);
                whiteCount += step(keyLuma, l10);
                whiteCount += step(keyLuma, filteredCenter);
                whiteCount += step(keyLuma, l12);
                whiteCount += step(keyLuma, l20);
                whiteCount += step(keyLuma, l21);
                whiteCount += step(keyLuma, l22);

                float isWhiteTransparent = step(4.5, whiteCount);

                float3 rgb = float3(0.0, 0.0, 0.0);
                float alpha = 1.0 - isWhiteTransparent;

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
