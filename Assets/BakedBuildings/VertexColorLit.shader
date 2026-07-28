Shader "Custom/VertexColorLitURP"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float4 color:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float3 base = IN.color.rgb * _Tint.rgb;
                float3 n = normalize(IN.normalWS);
                float3 wp = IN.positionWS;
                float3 albedo = base;

                if (abs(n.y) < 0.30)
                {
                    // ---- WALL: procedural window grid (world-space, no UVs) ----
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
                else if (n.y < 0.90)
                {
                    // ---- PITCHED ROOF: faint tile courses ----
                    float course = frac(wp.y / 0.34);
                    albedo = base * (1.0 - 0.10 * smoothstep(0.93, 0.98, course));
                }
                // else FLAT ROOF: leave base color

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
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
