#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float4 _MainTex_ST;

    // Normal map
    float4 _BumpMap_ST;
    half _BumpMapScale;

    // Lightmap
    half _UseLightMap;

    // _USE_SHADOWMAP
    half4 _ShadowColor;

    // _USE_SSS
    half4 _ColorScatter;
    half _BackDiffuse;

    // _USE_REFLECTION
    half4 _ReflectionColor;
    half _ReflectionMetallic;
    half _ReflectionIntensity;
    half _ReflectionFresnelPow;
    half _ReflectionRoughness;
CBUFFER_END
