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
                float dist = length(ObjSpaceViewDir(v.vertex)); // distance from cam to vertex in atmoshell

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
                // if this dot is positive that means viewdir is in direction of world normal
                // which means inside sphere looking out, otherwise you are outside looking in
                float dott = dot(viewDir, worldNormal);
                /*if (dott > 0.0) {
                    dist = 0.0;
                }*/

                // somethings still a little boned here but getting closer
				// also make a custom shader later that alpha fades between the skybox and just the color of this atmo
				// so as you get lower the space skybox fades out (this will work for sure i read it somewhere lol)
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                //half nl = max(0, dot(worldNormal, viewDir));
                o.uv = float2(nl, 1.0 - dist);
                o.diff = _LightColor0;

                // add illumination from ambient or light probes
                // ShadeSH9 function from UnityCG.cginc evaluates it using world space normal
                //o.diff.rgb += ShadeSH9(half4(worldNormal, 1));

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_ScatterLookup, i.uv);
                //col *= i.diff;
                //col.a = max(i.diff, 0.01);
                col.a = saturate(i.uv.y);
                
                return col;
            }
            ENDCG
        }
    }
}
