Shader "KrusShader/PostProcess/Ascii"
{   
    Properties
    {   
        _Contrast ("Contrast", Float) = 2
        _AsciiTex ("Ascii Texture", 2D) = "white" {}
        _Density ("Density", Float) = 1

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


                float charCount = _AsciiTex_TexelSize.z / _AsciiTex_TexelSize.w;
                float screenRatio = _ScaledScreenParams.x / _ScaledScreenParams.y;

                half3 blitCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.texcoord);
                half blitGray = floor(Desaturate(blitCol) * charCount) / charCount;

                _Density = floor(_Density);
                float tileW = _TestFac.x;
                float tileH = _TestFac.y;

                int charIndex = round(blitGray * (charCount-1));
                float2 charCoord =float2(((_ScaledScreenParams.x * input.texcoord.x) % tileW + (tileW-1)*charIndex)/ ((tileW - 1)* charCount), 
                    saturate(((int)(_ScaledScreenParams.y * input.texcoord.y) % tileH) / (tileH-1)));
                half3 asciiCol = SAMPLE_TEXTURE2D(_AsciiTex, sampler_PointRepeat, (charCoord));

                half3 col = asciiCol;
                return half4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}