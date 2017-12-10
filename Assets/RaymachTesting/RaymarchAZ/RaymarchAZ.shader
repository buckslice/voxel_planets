// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

//https://www.alanzucconi.com/2016/07/01/surface-shading/
//https://github.com/smkplus/UnityRayMarching/tree/master/Assets/Shaders
//http://www.habrador.com/tutorials/shaders/1-volume-rendering-death-star/
//http://flafla2.github.io/2016/10/01/raymarching.html

Shader "Unlit/Raymarch"
{
    Properties
    {
        _Color("Color", COLOR) = (1,1,1,1)
    }
        SubShader
    {
        Tags{ "RenderType" = "Opaque" }
        //Tags {"RenderType" = "Transparent"}
        //Cull Off
        ZWrite Off
        //ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
    {
        CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    #include "UnityCG.cginc"
    #include "Lighting.cginc"
    #include "Assets/Shaders/Includes/Noise.cginc"
    #include "Assets/Shaders/Includes/Shapes.cginc"

        //struct appdata
        //{
        //	float4 vertex : POSITION;
        //	float2 uv : TEXCOORD0;
        //};

        //sampler2D _MainTex;
        //float4 _MainTex_ST;
        float4 _Color;

    struct v2f {
        float4 pos : SV_POSITION; // clip space
        float3 wpos : TEXCOORD1; // world pos
    };


    float Density(float3 pos) {
        //return distance(pos, float3(0, 0, 0)) - 0.5;
        float s = Sphere(pos, float3(0, 0, 0), 0.5);
        float t = Torus(pos, float3(0, 0, 0), float2(.4, .2));
        return Subtraction(s, t);
    }

    float3 calcNormal(float3 p) {
        const fixed2 eps = fixed2(0.00, 0.02);  // swizzles are fast
        return normalize(float3(
            Density(p + eps.yxx) - Density(p - eps.yxx),
            Density(p + eps.xyx) - Density(p - eps.xyx),
            Density(p + eps.xxy) - Density(p - eps.xxy)
            ));
    }

    fixed4 lighting(fixed3 pos) {
        fixed3 lightDir = -normalize(_WorldSpaceLightPos0.xyz);// somethings weird going on here
        fixed3 lightCol = _LightColor0.rgb;
        float3 norm = calcNormal(pos);
        fixed3 NdotL = max(dot(norm, lightDir), 0.0);
        fixed4 c;
        c.rgb = _Color * lightCol * NdotL;
        c.a = 1;
        return c;
    }

    static const int STEPS = 64; // max number of steps
    static const float MIN_DIST = 0.001;
    static const float MAX_DIST = 1000.0;
    fixed4 raymarch(float3 pos, float3 dir) {
        for (int i = 0; i < STEPS; ++i) {
            float dist = Density(pos);
            if (dist < MIN_DIST) {  // very close to surface so get color here
                                    //return fixed4(1, 0, 0, 1);
                                    //return i / (float)10;
                return lighting(pos);
            }
            if (dist > MAX_DIST) {  // this ray is too far so break early
                break;
            }
            // move ray along its path by the distance
            pos += dist * dir;
        }
        //return fixed4(1, 1, 1, 1);
        return fixed4(1, 1, 1, 0);
    }


    v2f vert(appdata_full v) {
        v2f o;
        //o.uv = TRANSFORM_TEX(v.uv, _MainTex);

        o.pos = UnityObjectToClipPos(v.vertex);
        o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;


        //o.pos = v.vertex;
        //o.pos.xy *= 2.0;
        //o.wpos = mul(unity_ObjectToWorld, o.pos).xyz;

        return o;
    }
    fixed4 frag(v2f i) : SV_Target
    {
        float3 viewDir = normalize(i.wpos - _WorldSpaceCameraPos.xyz);
        return raymarch(i.wpos, viewDir);
    }

        ENDCG
    }
    }
}
