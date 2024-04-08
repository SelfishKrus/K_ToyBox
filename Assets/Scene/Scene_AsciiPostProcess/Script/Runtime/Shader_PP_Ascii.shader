Shader "KrusShader/PostProcess/Ascii"
{   
    Properties
    {   
        _Contrast ("Contrast", Float) = 2
        _AsciiTex ("Ascii Texture", 2D) = "white" {}
        _Density ("Density", Float) = 1

        _DownSample ("Downsample", Float) = 1

        [Space(20)]
        _TestFac ("Test Factor", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off 
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
            float _Contrast;
            float4 _AsciiTex_ST;
            float4 _AsciiTex_TexelSize;
            float _DownSample;
            float _Density;
            float4 _TestFac;
            CBUFFER_END

            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_AsciiTex);   SAMPLER(sampler_AsciiTex);

            half Desaturate(half3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }

            float clamp(float val, float minVal, float maxVal)
            {
                return max(minVal, min(maxVal, val));
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Downsample & pixelize
                float screenRatio = _ScreenParams.x / _ScreenParams.y;
                float charCount = _AsciiTex_TexelSize.z / _AsciiTex_TexelSize.w;

                float3 blitCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.texcoord);
                float floorBlitGray = floor(Desaturate(blitCol) * (charCount-1)) / (charCount-1);
                float stepGray = 1 / (charCount -1);
                float stepU = 1 / charCount;
                float index = floorBlitGray / stepGray;

                float2 charUV = (input.texcoord+float2(-0.5f, 0.0f)) * _ScreenParams.xy / (_DownSample); // wrap around
                //charUV += float2(-0.5f, 0.0f);
                charUV.x /= charCount;
                charUV = frac(charUV);
                charUV.x = charUV.x % stepU + index * stepU;
                half4 asciiCol = SAMPLE_TEXTURE2D(_AsciiTex, sampler_AsciiTex, charUV);

                half3 col = asciiCol;

                //col = asciiCol.a;
                //col.g = (charUV.x + charUV.y) * 0.1f;
                //col.r = floorBlitGray;
                //col.b = 0;

                return half4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}