Shader "DebtRunner/ShieldSpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0.32, 0.78, 1, 0.92)
        _OutlineSize ("Outline Size", Range(0.1, 6.0)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float _OutlineSize;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * max(0.1, _OutlineSize);

                half centerAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half edgeAlpha = 0.0h;
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texel.x, 0)).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texel.x, 0)).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, texel.y)).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, -texel.y)).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + texel).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - texel).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texel.x, -texel.y)).a);
                edgeAlpha = max(edgeAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texel.x, texel.y)).a);

                half outlineAlpha = saturate(edgeAlpha - centerAlpha);
                return half4(_OutlineColor.rgb, _OutlineColor.a * outlineAlpha);
            }
            ENDHLSL
        }
    }
}
