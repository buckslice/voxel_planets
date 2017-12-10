
Shader "Hidden/RaymarchGeneric"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", COLOR) = (1,1,1,1)
    }
        SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        //Blend One One   // omg turn this on it looks so cool and glitchy
        Pass
    {
        CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
        // Compile one version of the shader with performance debugging
        // This way we can simply set a shader keyword to test perf
    #pragma shader_feature DEBUG_PERFORMANCE
        // You may need to use an even later shader model target, depending on how many instructions you have
        // or if you need variable-length for loops.
    #pragma target 3.0

    #include "UnityCG.cginc"
    #include "Assets/Shaders/Includes/Noise.cginc"
    #include "Assets/Shaders/Includes/Shapes.cginc"

    uniform sampler2D _CameraDepthTexture;
    // These are are set by our script (see RaymarchGeneric.cs)
    uniform sampler2D _MainTex;
    uniform float4 _MainTex_TexelSize;

    uniform float4 _Color;

    uniform float4x4 _CameraInvViewMatrix;
    uniform float4x4 _FrustumCornersES;
    uniform float4 _CameraWS;

    uniform float3 _LightDir;
    //uniform float4x4 _MatTorus_InvModel;
    uniform sampler2D _ColorRamp_Material;
    uniform sampler2D _ColorRamp_PerfMap;

    uniform float _DrawDistance;

    static const int MAX_STEPS = 100;

    struct appdata {
        // Remember, the z value here contains the index of _FrustumCornersES to use
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 ray : TEXCOORD1;
    };

    v2f vert(appdata v) {
        v2f o;

        // Index passed via custom blit function in RaymarchGeneric.cs
        half index = v.vertex.z;
        v.vertex.z = 0.1;

        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv.xy;

    #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            o.uv.y = 1 - o.uv.y;
    #endif

        // Get the eyespace view ray (normalized)
        o.ray = _FrustumCornersES[(int)index].xyz;
        // Dividing by z "normalizes" it in the z axis
        // Therefore multiplying the ray by some number i gives the viewspace position
        // of the point on the ray with [viewspace z]=i
        o.ray /= abs(o.ray.z);

        // Transform the ray from eyespace to worldspace
        o.ray = mul(_CameraInvViewMatrix, o.ray);

        return o;
    }

    // This is the distance field function.  The distance field represents the closest distance to the surface
    // of any object we put in the scene.  If the given point (point p) is inside of an object, we return a
    // negative answer.
    // return.x: result of distance field
    // return.y: material data for closest object (not doing this anymore but could later)
    //float map(float3 pos) {
    //    float s = sdSphere(pos, float3(0, 0, 0), 0.5);
    //    float t = sdTorus(pos, float3(0, 0, 0), float2(.4, .2));
    //    return opSub(s, t);
    //}

    float2 map(float3 pos) {
        //float3 rpos = opRep(pos, float3(1.1, 10, 1.1));
        float3 rpos = opRep(pos, float3(1.1, 10+sin(_Time.y+pos.x*0.1), 1.1));
        //float3 rpos = float3(opRep1(pos.x, 1.1), pos.y+sin(_Time.y + pos.x), opRep1(pos.z, 1.1));
        float2 s = float2(sdSphere(rpos, float3(0, 0, 0), 0.5), 0.5);
        float t = sdTorus(rpos, float3(0, 0, 0), float2(.4, .2));
        return float2(t, s.y);
    }

    float3 calcNormal(in float3 pos) {
        const float2 eps = float2(0.001, 0.0);
        // The idea here is to find the "gradient" of the distance field at pos
        // Remember, the distance field is not boolean - even if you are inside an object
        // the number is negative, so this calculation still works.
        // Essentially you are approximating the derivative of the distance field at this point.
        float3 nor = float3(
            map(pos + eps.xyy).x - map(pos - eps.xyy).x,
            map(pos + eps.yxy).x - map(pos - eps.yxy).x,
            map(pos + eps.yyx).x - map(pos - eps.yyx).x);
        return normalize(nor);
    }

    // Raymarch along given ray
    // ro: ray origin
    // rd: ray direction
    // s: unity depth buffer
    fixed4 raymarch(float3 ro, float3 rd, float s) {
        float t = 0; // current distance traveled along ray
        for (int i = 0; i < MAX_STEPS; ++i) {
            // If we run past the depth buffer, or if we exceed the max draw distance,
            // stop and return nothing (transparent pixel).
            // this way raymarched objects and traditional meshes can coexist.
            if (t >= s || t > _DrawDistance) {
                return fixed4(0, 0, 0, 0);
            }

            float3 p = ro + rd * t; // World space position of sample
            float2 d = map(p);		// Sample of distance field (see map())
                                    
            if (d.x < 0.001) {      // If the sample <= 0, we have hit something (see map()).
                float3 n = calcNormal(p);
                float light = max(dot(-_LightDir.xyz, n),0.0); // unclamped looks cool kinda (shadows go negative color)
                // using tex2Dlod since tex2D needs to calculate derivatives to figure out mips but this manually defines them
                //fixed3 col = tex2Dlod(_ColorRamp_Material, float4(d.y, 0, 0, 0)).xyz * light;
                fixed3 col = fixed3(.95, .65, .4) * light;  // omg donuts yes
                //fog = t / (_DrawDistance*0.5);
                //fog = saturate(1.0 - fog*fog);
                float fog = t / _DrawDistance;
                fog = 1.0-saturate(fog*2.);
                
                return fixed4(col, fog);

                //return fixed4(_Color.rgb * light, 1.0);
            }

            // If the sample > 0, we haven't hit anything yet so we should march forward
            // We step forward by distance d, because d is the minimum distance possible to intersect
            // an object (see map()).
            t += d.x;
        }
        return fixed4(0,0,0,0);
    }

    // Modified raymarch loop that displays a heatmap of ray sample counts
    // Useful for performance testing and analysis
    // ro: ray origin
    // rd: ray direction
    // s: unity depth buffer
    fixed4 raymarch_perftest(float3 ro, float3 rd, float s) {
        float t = 0; // current distance traveled along ray
        for (int i = 0; i < MAX_STEPS; ++i) {
            float3 p = ro + rd * t; // World space position of sample
            float2 d = map(p);      // Sample of distance field (see map())

            // If the sample <= 0, we have hit something (see map()).
            // If t > drawdist, we can safely bail because we have reached the max draw distance
            if (d.x < 0.001 || t > _DrawDistance) {
                // Simply return the number of steps taken, mapped to a color ramp.
                float perf = (float)i / MAX_STEPS;
                return fixed4(tex2Dlod(_ColorRamp_PerfMap, float4(perf,0,0,0)).xyz, 1);
            }

            t += d.x;
        }
        // By this point the loop guard (i < maxstep) is false.  Therefore
        // we have reached maxstep steps.
        return fixed4(tex2D(_ColorRamp_PerfMap, float2(1, 0)).xyz, 1);
    }

    fixed4 frag(v2f i) : SV_Target
    {
        // ray direction
        float3 rd = normalize(i.ray.xyz);
        // ray origin (camera position)
        float3 ro = _CameraWS;

        float2 duv = i.uv;
    #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            duv.y = 1 - duv.y;
    #endif

        // Convert from depth buffer (eye space) to true distance from camera
        // This is done by multiplying the eyespace depth by the length of the "z-normalized"
        // ray (see vert()).  Think of similar triangles: the view-space z-distance between a point
        // and the camera is proportional to the absolute distance.
        float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, duv).r);
        depth *= length(i.ray);

        fixed3 col = tex2D(_MainTex,i.uv);

    #if DEBUG_PERFORMANCE
        fixed4 add = raymarch_perftest(ro, rd, depth);
    #else
        fixed4 add = raymarch(ro, rd, depth);
    #endif

        // Returns final color using alpha blending
        return fixed4(col*(1.0 - add.w) + add.xyz * add.w,1.0);
    }
        ENDCG
    }
    }
}