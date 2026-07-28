Shader "Custom/TriplanarFacade"
{
    // World-space triplanar facade texturing for the baked OSM buildings (their wall
    // meshes have no usable UVs). Lighting and the procedural window grid intentionally
    // match Custom/VertexColorLitURP so textured buildings blend with the scene style.
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _WorldScale("World Meters Per Tile", Float) = 3.0
        _Windows("Draw Window Grid", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _WorldScale;
                float _Windows;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pi.positionCS;
                o.positionWS = pi.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(pi.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 wp = i.positionWS;

                // Triplanar blend weights, sharpened so flat walls use a single projection
                float3 w = pow(abs(n), 8.0);
                w /= (w.x + w.y + w.z);

                float invScale = 1.0 / max(_WorldScale, 0.001);
                half4 cx = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.zy * invScale);
                half4 cy = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.xz * invScale);
                half4 cz = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.xy * invScale);
                float3 base = (cx * w.x + cy * w.y + cz * w.z).rgb * _BaseColor.rgb;
                float3 albedo = base;

                if (_Windows > 0.5 && abs(n.y) < 0.30)
                {
                    // ---- WALL: procedural window grid (world-space, no UVs) ----
                    // Same layout as Custom/VertexColorLitURP so the city style stays consistent.
                    float2 nh = float2(n.x, n.z);
                    nh /= max(length(nh), 1e-3);
                    float2 tang = float2(nh.y, -nh.x);
                    float u = dot(wp.xz, tang);
                    float v = wp.y;
                    float cw = 2.4;   // bay width (m)
                    float ch = 3.1;   // floor height (m)
                    float fu = frac(u / cw);
                    float fv = frac((v - 0.15) / ch);
                    float halfW = 0.34;
                    float halfH = 0.32;
                    float dx = abs(fu - 0.5);
                    float dy = abs(fv - 0.5);
                    float inWin   = (dx < halfW       && dy < halfH)       ? 1.0 : 0.0;
                    float inFrame = ((dx < halfW+0.06 && dy < halfH+0.06)  ? 1.0 : 0.0) - inWin;
                    float colId = floor(u / cw);
                    float rowId = floor((v - 0.15) / ch);
                    float rnd = frac(sin(colId*12.9898 + rowId*78.233) * 43758.5453);
                    float3 glass = float3(0.13,0.16,0.19) + rnd*0.05;
                    glass = lerp(glass, float3(0.55,0.55,0.52), step(0.86, rnd)*0.5); // some lit/curtain
                    float3 frameCol = base * 1.12;
                    float ground = smoothstep(0.0, 1.2, v); // plaster plinth at base
                    inWin *= ground;
                    albedo = lerp(base, glass, inWin);
                    albedo = lerp(albedo, frameCol, saturate(inFrame) * 0.7 * ground);
                }

                float4 shadowCoord = TransformWorldToShadowCoord(wp);
                Light ml = GetMainLight(shadowCoord);
                float ndl = saturate(dot(n, ml.direction));
                float3 col = albedo * ml.color * ndl * ml.shadowAttenuation;
                float3 sh = SampleSH(n);
                col += albedo * max(sh, float3(0.32, 0.32, 0.34));
                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; li++)
                {
                    Light al = GetAdditionalLight(li, wp);
                    float d = saturate(dot(n, al.direction));
                    col += albedo * al.color * d * al.distanceAttenuation * al.shadowAttenuation;
                }
                #endif
                col = MixFog(col, i.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
