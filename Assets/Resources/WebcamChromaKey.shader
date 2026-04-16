Shader "Custom/WebcamChromaKey"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TargetMaskTex ("Target Mask Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (0, 1, 0, 1)
        _Threshold ("Threshold", Range(0,1)) = 0.48
        _Smooth ("Smoothness", Range(0,1)) = 0.05
        _Spill ("Spill Reduction (unused)", Range(0,1)) = 0.2
        _Mirror ("Mirror Horizontal", Float) = 0
        _VFlip ("Vertical Flip", Float) = 0
        _OpaqueToBlack ("Opaque to Black", Float) = 0
        _EdgeContrast ("Edge Contrast", Range(1,10)) = 1.6
        _NoiseFilter ("Noise Filter", Range(0,1)) = 0.2
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.62
        _MidValueFilter ("Mid Value Filter", Range(0,0.49)) = 0.15
        _LeftClip ("Left Clip", Range(0,1)) = 0
        _RightClip ("Right Clip", Range(0,1)) = 0
        _TopClip ("Top Clip", Range(0,1)) = 0
        _BottomClip ("Bottom Clip", Range(0,1)) = 0
        _TargetMaskEnabled ("Target Mask Enabled", Float) = 0
        _TargetMaskMinAlpha ("Target Mask Min Alpha", Range(0,1)) = 0.01
        _TargetRectMinMax ("Target Rect MinMax", Vector) = (0,0,1,1)
        _TargetUvRect ("Target UV Rect", Vector) = (0,0,1,1)
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
            sampler2D _TargetMaskTex;

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
            float _TargetMaskEnabled;
            float _TargetMaskMinAlpha;
            float4 _TargetRectMinMax;
            float4 _TargetUvRect;
            float4x4 _TargetWorldToLocal;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD1;
            };

            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float ComputeKeyDistance(float3 c, float3 keyHsv)
            {
                float3 hsv = RGBToHSV(c);

                float hueDist = abs(hsv.x - keyHsv.x);
                hueDist = min(hueDist, 1.0 - hueDist) * 2.0;
                float satDist = abs(hsv.y - keyHsv.y);
                float valDist = abs(hsv.z - keyHsv.z);

                float keySat = keyHsv.y;
                float keyVal = keyHsv.z;
                float wHue = 0.7 + (0.2 * keySat);
                float wSat = 0.35;
                float wVal = lerp(0.45, 0.12, keySat);
                wVal += saturate((0.2 - keyVal) / 0.2) * 0.12;

                float dist = sqrt((hueDist * hueDist * wHue * wHue) + (satDist * satDist * wSat * wSat) + (valDist * valDist * wVal * wVal));
                float norm = sqrt((wHue * wHue) + (wSat * wSat) + (wVal * wVal));
                return dist / max(norm, 1e-5);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);

                if (_Mirror > 0.5) uv.x = 1.0 - uv.x;
                if (_VFlip > 0.5) uv.y = 1.0 - uv.y;

                o.uv = uv;
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;
                float2 uv = i.uv;
                float3 key = _KeyColor.rgb;

                float3 keyHsv = RGBToHSV(key);

                float3 c00r = tex2D(_MainTex, uv + float2(-t.x, -t.y)).rgb;
                float d00 = ComputeKeyDistance(c00r, keyHsv);

                float3 c01r = tex2D(_MainTex, uv + float2( 0.0,  -t.y)).rgb;
                float d01 = ComputeKeyDistance(c01r, keyHsv);

                float3 c02r = tex2D(_MainTex, uv + float2( t.x,  -t.y)).rgb;
                float d02 = ComputeKeyDistance(c02r, keyHsv);

                float3 c10r = tex2D(_MainTex, uv + float2(-t.x,  0.0)).rgb;
                float d10 = ComputeKeyDistance(c10r, keyHsv);

                float3 c11r = tex2D(_MainTex, uv).rgb;
                float d11 = ComputeKeyDistance(c11r, keyHsv);

                float3 c12r = tex2D(_MainTex, uv + float2( t.x,  0.0)).rgb;
                float d12 = ComputeKeyDistance(c12r, keyHsv);

                float3 c20r = tex2D(_MainTex, uv + float2(-t.x,  t.y)).rgb;
                float d20 = ComputeKeyDistance(c20r, keyHsv);

                float3 c21r = tex2D(_MainTex, uv + float2( 0.0,   t.y)).rgb;
                float d21 = ComputeKeyDistance(c21r, keyHsv);

                float3 c22r = tex2D(_MainTex, uv + float2( t.x,   t.y)).rgb;
                float d22 = ComputeKeyDistance(c22r, keyHsv);

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

                // Clip overlay by target mask area (driven from AcCheck).
                if (_TargetMaskEnabled > 0.5)
                {
                    float4 worldPos = mul(unity_ObjectToWorld, float4(i.localPos, 1.0));
                    float4 targetLocalPos4 = mul(_TargetWorldToLocal, worldPos);
                    float2 targetLocalPos = targetLocalPos4.xy;

                    float inRect =
                        step(_TargetRectMinMax.x, targetLocalPos.x) *
                        step(targetLocalPos.x, _TargetRectMinMax.z) *
                        step(_TargetRectMinMax.y, targetLocalPos.y) *
                        step(targetLocalPos.y, _TargetRectMinMax.w);

                    if (inRect < 0.5)
                    {
                        alpha = 0.0;
                    }
                    else
                    {
                        float2 rectSize = _TargetRectMinMax.zw - _TargetRectMinMax.xy;
                        float2 targetNormalized = (targetLocalPos - _TargetRectMinMax.xy) / max(rectSize, float2(1e-5, 1e-5));
                        targetNormalized = saturate(targetNormalized);

                        float2 targetUv;
                        targetUv.x = lerp(_TargetUvRect.x, _TargetUvRect.z, targetNormalized.x);
                        targetUv.y = lerp(_TargetUvRect.y, _TargetUvRect.w, targetNormalized.y);

                        float targetAlpha = tex2D(_TargetMaskTex, targetUv).a;
                        if (targetAlpha < _TargetMaskMinAlpha)
                        {
                            alpha = 0.0;
                        }
                    }
                }

                float3 c = tex2D(_MainTex, uv).rgb;
                float3 rgb = (_OpaqueToBlack > 0.5) ? float3(0.0, 0.0, 0.0) : c;

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
