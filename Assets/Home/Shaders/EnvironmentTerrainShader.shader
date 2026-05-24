Shader "Spatial/Environment/EnvironmentTerrainShader"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)

        _MainTex ("Main Texture (Base Color)", 2D) = "white" {}
        _MainTexR ("Texture for CH R", 2D) = "white" {}
        _MainTexG ("Texture for CH G", 2D) = "white" {}
        _MainTexB ("Texture for CH B", 2D) = "white" {}

        _ChannelMap ("Channelmap for texture blending", 2D) = "white" {}

        [Toggle(_USE_SHADOWMAP)] _UseShadowmap("Use Shadow Map", int) = 1
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
    }
    SubShader 
    {
        Pass 
        {
            Tags { "RenderType"="Opaque" "LightMode"="UniversalForward" "RenderPipeline" = "UniversalPipeline"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_SHADOWMAP

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MainTexR);
            SAMPLER(sampler_MainTexR);
            TEXTURE2D(_MainTexG);
            SAMPLER(sampler_MainTexG);
            TEXTURE2D(_MainTexB);
            SAMPLER(sampler_MainTexB);
            TEXTURE2D(_ChannelMap);
            SAMPLER(sampler_ChannelMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTexR_ST;
            float4 _MainTexG_ST;
            float4 _MainTexB_ST;
            half4 _Color;
            half4 _ShadowColor;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                float2 uv : TEXCOORD0;  // Textures
                float2 uv2 : TEXCOORD1; // Lightmap
                float2 uv3 : TEXCOORD2; // Channelmap
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;  // Textures
                float2 uv2 : TEXCOORD1; // Lightmap
                float2 uv3 : TEXCOORD2; // Channelmap
                float4 uv4 : TEXCOORD3;
                float4 uv5 : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                #ifdef _USE_SHADOWMAP
                    float4 shadowCoord : TEXCOORD6;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = mad(v.uv2, unity_LightmapST.xy, unity_LightmapST.zw);
                o.uv3 = v.uv3;
                o.uv4 = float4(TRANSFORM_TEX(v.uv, _MainTexR), TRANSFORM_TEX(v.uv, _MainTexG));
                o.uv5 = float4(TRANSFORM_TEX(v.uv, _MainTexB), 0, 0);

                #ifdef _USE_SHADOWMAP
                    o.shadowCoord = TransformWorldToShadowCoord(TransformObjectToWorld(v.vertex.xyz));
                #endif

                o.fogFactor = ComputeFogFactor(o.vertex.z); // positionCS.z
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 mainTexR = SAMPLE_TEXTURE2D(_MainTexR, sampler_MainTexR, i.uv4.xy);
                half4 mainTexG = SAMPLE_TEXTURE2D(_MainTexG, sampler_MainTexG, i.uv4.zw);
                half4 mainTexB = SAMPLE_TEXTURE2D(_MainTexB, sampler_MainTexB, i.uv5.xy);
                half4 channel = SAMPLE_TEXTURE2D(_ChannelMap, sampler_ChannelMap, i.uv3);

                half3 color = lerp(lerp(lerp(mainTex.rgb, mainTexR.rgb, channel.r), mainTexG.rgb, channel.g), mainTexB.rgb, channel.b);

                color *= _Color.rgb;

                // Lightmap
                real4 encodedIlluminance = SAMPLE_TEXTURE2D_LIGHTMAP(unity_Lightmap, samplerunity_Lightmap, i.uv2).rgba;
                half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
                half3 lightmap = DecodeLightmap(encodedIlluminance, decodeInstructions);
                color *= lightmap;

                // Shadow
                #ifdef _USE_SHADOWMAP
                    // from MainLightRealtimeShadow in Shadows.hlsl
                    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
                    half4 shadowParams = GetMainLightShadowParams();
                    half shadow = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), i.shadowCoord, shadowSamplingData, shadowParams, false);

                    color.rgb = lerp(color.rgb * _ShadowColor.rgb, color.rgb, shadow);
                #endif

                // Fog
                color.rgb = MixFog(color.rgb, i.fogFactor);

                return half4(color, 1.);
            }
            ENDHLSL
        }

    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
