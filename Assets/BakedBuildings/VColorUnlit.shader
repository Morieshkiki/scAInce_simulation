Shader "Custom/VColorUnlit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionHCS:SV_POSITION; float4 color:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A IN){ V o; UNITY_SETUP_INSTANCE_ID(IN); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionHCS=GetVertexPositionInputs(IN.positionOS.xyz).positionCS; o.color=IN.color; return o; }
            half4 frag(V IN):SV_Target{ UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN); return half4(IN.color.rgb,1.0); }
            ENDHLSL
        }
    }
}
