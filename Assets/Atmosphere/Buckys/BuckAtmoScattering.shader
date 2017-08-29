Shader "Custom/BuckAtmoScattering"
{
    Properties
    {
        _Color("TintColor", Color) = (1,1,1,1)
        _ScatterLookup("ScatterLookup", 2D) = "white" {}
    }
        SubShader
    {
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Blend One One
        Cull Off

        Pass
        {
            Tags { "LightMode" = "ForwardBase"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct v2f {
                float2 uv : TEXCOORD0; // .x is angle to sun (dot product), .y is dist to atmo (density kinda)
                fixed4 diff : COLOR0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            sampler2D _ScatterLookup;

            v2f vert(appdata_base v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = v.texcoord;
                float dist = saturate(length(ObjSpaceViewDir(v.vertex))/10.0); // distance from cam to vertex in atmoshell

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.uv = float2(0.0, dist);
                o.diff = nl * _LightColor0;

                // add illumination from ambient or light probes
                // ShadeSH9 function from UnityCG.cginc evaluates it using world space normal
                //o.diff.rgb += ShadeSH9(half4(worldNormal, 1));

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_ScatterLookup, i.uv);
                col *= i.diff;
                //col.a = max(i.diff, 0.01);
                col.a = saturate(i.uv.y);
                
                return col;
            }
            ENDCG
        }
    }
}
