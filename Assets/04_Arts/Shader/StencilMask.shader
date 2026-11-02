// 캐릭터의 부채꼴 시야 메시
// 메시가 덮는 화면 영역에 스텐실 값을 기록
Shader "Custom/StencilMask"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-100"
        }

        Pass
        {
            Name "StencilMask"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            // 화면 색상은 변경하지 않습니다.
            ColorMask 0

            // 시야 메시가 다른 물체를 깊이로 가리지 않게 합니다.
            ZWrite Off

            // 이미 기록된 깊이와 관계없이 마스크를 기록합니다.
            ZTest Always

            // 평면 메시를 양쪽에서 사용할 수 있게 합니다.
            Cull Off

            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask 1

                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 0);
            }

            ENDHLSL
        }
    }
}