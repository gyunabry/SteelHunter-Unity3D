Shader "Custom/URP/FogOfWarComposite"
{
    Properties
    {
        _FogTex ("Current Visibility", 2D) = "black" {}

        _FogColor ("Hidden Area Multiplier", Color) = (0.08, 0.08, 0.08, 1)

        // xy: 맵 중심의 XZ, zw: 맵의 가로·세로 크기
        _WorldRect ("World Rectangle", Vector) = (0, 0, 100, 100)
        [Toggle] _FlipY ("Flip Visibility Y", Float) = 0

        _EdgeLow ("Edge Dark Threshold", Range(0, 1)) = 0.05
        _EdgeHigh ("Edge Bright Threshold", Range(0, 1)) = 0.95
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "FogOfWarComposite"

            Cull Off
            ZWrite Off
            ZTest Always

            // 현재 화면 색상에 출력 색상을 곱합니다.
            Blend DstColor Zero
            ColorMask RGB

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _WorldRect;
                half4 _FogColor;
                float _FlipY;
                float _EdgeLow;
                float _EdgeHigh;
            CBUFFER_END

            half ReadSoftVisibility(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0.0h;

                half visibility = SAMPLE_TEXTURE2D(
                    _FogTex,
                    sampler_FogTex,
                    uv).r;

                float low = min(_EdgeLow, 0.999);
                float high = max(_EdgeHigh, low + 0.001);

                return smoothstep(low, high, visibility);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV =
                    input.positionCS.xy / _ScaledScreenParams.xy;

                float rawDepth = SampleSceneDepth(screenUV);

                // 깊이가 없는 배경에는 안개를 적용하지 않습니다.
                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.000001)
                        return half4(1, 1, 1, 1);

                    float depth = rawDepth;
                #else
                    if (rawDepth >= 0.999999)
                        return half4(1, 1, 1, 1);

                    float depth =
                        lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(
                    screenUV, depth, UNITY_MATRIX_I_VP);

                float2 fogUV =
                    (worldPosition.xz - _WorldRect.xy) /
                    max(_WorldRect.zw, float2(0.001, 0.001)) + 0.5;

                if (_FlipY > 0.5)
                    fogUV.y = 1.0 - fogUV.y;

                half visibility = ReadSoftVisibility(fogUV);

                half3 multiplier = lerp(
                    _FogColor.rgb,
                    half3(1, 1, 1),
                    visibility);

                return half4(multiplier, 1);
            }

            ENDHLSL
        }
    }
}