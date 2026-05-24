Shader "Spatial/Environment/Water"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaterColor("Water Color", Color) = (1,1,1,1)
        _WaterColorAdd("Water Color Additional", Color) = (0,0,0,0)
        _FresnelPow("Fresnel Strength", float) = 1

        [Header(Waves)][Space(5)]
        _BumpMap ("Normal Map", 2D) = "" {}
        _NormalIntensityNear("Normal Intensity (Near)", Range(0,4)) = 1
        _NormalIntensityFar("Normal Intensity (Far)", Range(0,4)) = 1
        _WaveSpeed("Wave Speed", float) = 1

        [Header(Anisotropic)][Space(5)]
        _AnisoRatio("Anisotropic distortion ratio", float) = 1

        [Header(Flow)][Space(5)]
        [KeywordEnum(Off, UV)] _FlowMode("Flow mode", float) = 0
        _FlowSpeed("Flow Speed", float) = 1
        _FlowDirection("Flow Direction(uv)", Vector) = (0,1,0,0)

        [Header(Opaque Water)][Space(5)]
        [Toggle(_OPAQUE_WATER)] _OpaqueWater ("Water opaque?", int) = 0
        _OpaqueWaterColor("Opaque Water Color", Color) = (1,1,1,1)

        [Header(Blending)][Space(5)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend mode Source", Int) = 5 //SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend mode Destination", Int) = 10 // OneMinusSrcAlpha
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _FLOWMODE_OFF _FLOWMODE_UV

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float3 worldPos : TEXCOORD0;
                // these three vectors will hold a 3x3 rotation matrix
                // that transforms from tangent to world space
                half3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
                half3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
                half3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z
                // texture coordinate for the normal map
                float2 uvNormal : TEXCOORD4;
                float2 uv : TEXCOORD5;
                float4 vertex : SV_POSITION;
                float distance : TEXCOORD9;
                float fogCoord : TEXCOORD8;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _WaterColor;
                half4 _WaterColorAdd;
                half _FresnelPow;

                int _OpaqueWater;
                half4 _OpaqueWaterColor;

                float4 _BumpMap_ST;
                float _NormalIntensityNear;
                float _NormalIntensityFar;

                float _WaveSpeed;

                float _FlowSpeed;
                float2 _FlowDirection;
                
                // Fake anisotropic reflection
                float _AnisoRatio;
            CBUFFER_END

            // vertex shader now also needs a per-vertex tangent vector.
            // in Unity tangents are 4D vectors, with the .w component used to
            // indicate direction of the bitangent vector.
            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
                o.worldPos = vertexInput.positionWS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normal.xyz, v.tangent);
                half3 wNormal = normalInputs.normalWS;
                half3 wTangent = normalInputs.tangentWS;
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(wNormal, wTangent) * tangentSign;

                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                o.uvNormal = TRANSFORM_TEX(v.uv, _BumpMap);

                o.uv = v.uv;

                o.distance = o.vertex.z;
                o.fogCoord = ComputeFogFactor(o.vertex.z);
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                half3 worldViewDir = GetWorldSpaceNormalizeViewDir(i.worldPos);

                // Sample the normal map, and decode from the Unity encoding
                float t = _Time.x * _WaveSpeed;
                float th = t*0.5;
                #if defined(_FLOWMODE_UV)
                    i.uvNormal += _FlowDirection * _Time.x * _FlowSpeed;
                #endif

                half3 tnormal1 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uvNormal + float2(sin(t), th)));
                half3 tnormal2 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uvNormal + float2(-th, -sin(th))));
                half3 tnormal = normalize(tnormal1 + tnormal2);

                // Reduce strength of normals as viewing angle becomes more horizontal to reduce aliasing.
                half normalIntensityFalloff = dot(worldViewDir, half3(0, 1, 0));
                half normalIntensity = lerp(_NormalIntensityFar, _NormalIntensityNear, normalIntensityFalloff * normalIntensityFalloff);
                tnormal = lerp(half3(0,0,1), tnormal, normalIntensity);

                // Transform normal from tangent to world space
                half3 worldNormal;
                worldNormal.x = dot(i.tspace0, tnormal);
                worldNormal.y = dot(i.tspace1, tnormal);
                worldNormal.z = dot(i.tspace2, tnormal);

                // World Reflection
                half3 worldRefl = reflect(-worldViewDir, worldNormal);
                // adjust by distance - assuming the world is not flat.
                worldRefl.y *= _AnisoRatio + (i.distance / 1000.0);

                // Fresnel
                half nv = dot(worldNormal, worldViewDir);
                nv = 1-saturate(nv);
                nv = pow(nv, _FresnelPow);

                // Sample Sky
                half4 skyData = SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, worldRefl);
                half3 skyColor = DecodeHDREnvironment (skyData, unity_SpecCube0_HDR);
                if(_OpaqueWater > 0){
                    skyColor = lerp(_OpaqueWaterColor.rgb, skyColor, nv*nv);
                }

                // Water Color
                half4 waterTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 col = _WaterColor;
                col.rgb *= skyColor;
                col.rgb *= waterTex.rgb;

                // Alpha
                col.a = saturate(col.a + nv) * waterTex.a;

                // Add color
                col += _WaterColorAdd;

                col.rgb = MixFog(col.rgb, i.fogCoord);
                return col;
            }
            ENDHLSL
        }
    }
}
