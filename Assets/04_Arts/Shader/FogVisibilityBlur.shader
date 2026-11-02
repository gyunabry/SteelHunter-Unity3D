Shader "Hidden/FogOfWar/VisibilityBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
        _HistoryTex ("History", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _HistoryTex;

        // 한 샘플 간격을 UV 단위로 전달합니다.
        float4 _StepUV;

        // 0: 이전 결과 유지, 1: 현재 결과 사용
        float _Response;

        float ReadMask(float2 uv)
        {
            // 텍스처 바깥은 시야 밖입니다.
            if (any(uv < 0.0) || any(uv > 1.0))
                return 0.0;

            return tex2D(_MainTex, uv).r;
        }

        float Gaussian9(float2 uv, float2 stepUV)
        {
            float value = ReadMask(uv) * 0.2270270270;

            value += (
                ReadMask(uv + stepUV) +
                ReadMask(uv - stepUV)
            ) * 0.1945945946;

            value += (
                ReadMask(uv + stepUV * 2.0) +
                ReadMask(uv - stepUV * 2.0)
            ) * 0.1216216216;

            value += (
                ReadMask(uv + stepUV * 3.0) +
                ReadMask(uv - stepUV * 3.0)
            ) * 0.0540540541;

            value += (
                ReadMask(uv + stepUV * 4.0) +
                ReadMask(uv - stepUV * 4.0)
            ) * 0.0162162162;

            return saturate(value);
        }

        float4 FragHorizontal(v2f_img input) : SV_Target
        {
            float visibility = Gaussian9(
                input.uv,
                float2(_StepUV.x, 0.0));

            return float4(visibility, visibility, visibility, 1.0);
        }

        float4 FragVertical(v2f_img input) : SV_Target
        {
            float visibility = Gaussian9(
                input.uv,
                float2(0.0, _StepUV.y));

            return float4(visibility, visibility, visibility, 1.0);
        }

        float4 FragTemporal(v2f_img input) : SV_Target
        {
            float current = tex2D(_MainTex, input.uv).r;
            float previous = tex2D(_HistoryTex, input.uv).r;

            float visibility = lerp(
                previous,
                current,
                saturate(_Response));

            return float4(visibility, visibility, visibility, 1.0);
        }

        ENDCG

        // Pass 0: 가로 블러
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragHorizontal
            ENDCG
        }

        // Pass 1: 세로 블러
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragVertical
            ENDCG
        }

        // Pass 2: 이전 프레임과 혼합
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragTemporal
            ENDCG
        }
    }
}