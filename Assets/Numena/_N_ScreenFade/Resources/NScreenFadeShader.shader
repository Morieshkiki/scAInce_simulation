Shader "Hidden/NScreenFadeShader"
{
    Properties
    {
        _Color ("_Color", COLOR) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent"  "Queue"="Overlay+99"}
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                return o;
            }

			fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
