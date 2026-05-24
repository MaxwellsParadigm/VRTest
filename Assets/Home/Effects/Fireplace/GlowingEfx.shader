Shader "Spatial/Environment/GlowingEfx"
{
    Properties
    {
        _Color("Color", color) = (1,1,1,1)
        _GlowSpread("Glow Spread", float) = 0
        _GlowStrength("Glow Strength", float) = 1
        _Refraction("Refraction", float) = 1
    }
    SubShader
    {
        Tags { "PerformanceChecks"="False" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        Cull Back
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers xboxone ps4 n3ds wiiu
            #pragma multi_compile_fwdbase nodirlightmap nodynlightmap novertexlight noambient exclude_path:deferred exclude_path:prepass
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float glow : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _GlowSpread;
            float _GlowStrength;
            half _Refraction;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.vertex.xyz += v.normal.xyz * sin(_Time.w * 9.218) * 0.001;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                half3 localViewDir = normalize(TransformWorldToObject(GetCurrentViewPosition()) - v.vertex.xyz);

                float3 p = v.vertex.xyz - localViewDir * _Refraction;
                float glow = p.x*p.x + p.y*p.y + p.z*p.z;
                glow = _GlowSpread - glow;
                glow *= _GlowStrength;
                glow = saturate(glow);
                o.glow = smoothstep(.5, 1., glow);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return half4(_Color.rgb, _Color.a * i.glow);
            }
            ENDHLSL
        }
    }
}
