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

            float4 _KeyColor;
            float _Threshold;
            float _Smooth;
            float _Spill;
            float _Mirror;
            float _VFlip;
            float _OpaqueToBlack;
            float _EdgeContrast;

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
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 c = col.rgb;
                float3 k = _KeyColor.rgb;

                // 단순 컬러키: RGB 거리로 특정 색을 투명 처리
                // dist가 작으면 키 색에 가깝다 → 투명
                float dist = distance(c, k);

                // 마스크: 0(키 색 영역, 투명) ~ 1(전경 유지)
                float mask = saturate((dist - _Threshold) / max(_Smooth, 1e-5));
                float alpha = mask;

                // 경계 선명도(콘트라스트) 조절: 1은 기본, 값이 클수록 경계가 또렷해짐
                alpha = saturate((alpha - 0.5) * _EdgeContrast + 0.5);

                // 스필 감소는 사용하지 않음(단순 컬러키 요구사항)
                float3 rgb = c;
                if (_OpaqueToBlack > 0.5)
                {
                    // 불투명(전경) 영역을 검은색으로 출력, 알파는 mask 유지
                    rgb = float3(0.0, 0.0, 0.0);
                }

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
