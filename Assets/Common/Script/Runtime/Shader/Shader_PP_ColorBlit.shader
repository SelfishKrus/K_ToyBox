Shader "KrusShader/PostProcess/ColorBlit"
{   
    Properties
    {
        _Intensity("Intensity", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off 
        Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            CBUFFER_END

            SAMPLER(sampler_BlitTexture);

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 color = 1.0f;
                color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.texcoord);
                color.g *= _Intensity;

                return half4(color, 1.0f);
            }
            ENDHLSL
        }
    }
}