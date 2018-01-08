
//http://flafla2.github.io/2016/10/01/raymarching.html

Shader "Hidden/RaymarchOctree"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
		_Color("Color", COLOR) = (1,1,1,1)
		_Volume("Volume", 3D) = "" {}
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
	uniform sampler3D _Volume;
    uniform float _VoxelSize = 1.0;	// currently unused, idea was to be able to specify worldspace size of voxels (now just things just combo of chunksize and texsize)
	uniform float _ChunkSize = 64.0;	// world space size of chunk
	static const float _TexSize = 64.0; // resolution of texture on each side (prob will change to 32 or 16 maybe?)
	// basically seems like you want everything to match texture dims so far otherwise stuff just looks weird
	// texs are gonna be like 16^3 or 32^3 all of em so nbd. still cant just upscale by *2 tho
	// something to do with that fract(p) nonsense down below (maybe just rethink my own version of what its trying to do)

    uniform float _DrawDistance;

    static const int MAX_STEPS = 100;
    static const float PRECISION = 0.0001;

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

    float2 map(float3 p) {
		//p.x = (int)p.x;
		//p.y = (int)p.y;
		//p.z = (int)p.z;
		// need to somehow get accurate readings even tho texture isnt a perfect field
		// biinear sampling is good but i want those blocky boys (also prob cheaper? doesnt seem like it though...
		// maybe thing where if u overstep field you step backward next time at half rez
		float d = tex3Dlod(_Volume, float4(p.x,p.y,p.z,0) / _ChunkSize).r;

		//p.x = (int)(p.x/_VoxelSize)*_VoxelSize;
		//p.y = (int)(p.y/_VoxelSize)*_VoxelSize;
		//p.z = (int)(p.z/_VoxelSize)*_VoxelSize;
		//float d = tex3Dlod(_Volume, float4(p.x,p.y,p.z,0) / _ChunkSize).r;

		//float d = sdSphere(p, float3(0,32,32), 32.0);

		return float2(d, 0.67);
    }

    float3 calcNormal(in float3 pos) {
        // some iq witchcraft seems to do the same thing
        //float2 e = float2(1.0, -1.0)*0.5773*0.0005;
        //return normalize(
            //e.xyy*map(pos + e.xyy).x +
            //e.yyx*map(pos + e.yyx).x +
            //e.yxy*map(pos + e.yxy).x +
            //e.xxx*map(pos + e.xxx).x
        //);

		// right now it should be whatever the current voxel size is i think
		// because you want to sample world space neighbor voxel cube boy
		// used to be 0.001 for normal raymarching (cant have any err tho)
        const float2 eps = float2(_ChunkSize/_TexSize, 0.0);

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
	
	// calculates intersection between a ray and a box
	// http://www.siggraph.org/education/materials/HyperGraph/raytrace/rtinter3.htm
	bool IntersectBox(float3 ray_o, float3 ray_d, float3 boxMin, float3 boxMax, out float tNear, out float tFar)
	{
		// compute intersection of ray with all six bbox planes
		float3 invR = 1.0 / ray_d;
		float3 tBot = invR * (boxMin.xyz - ray_o);
		float3 tTop = invR * (boxMax.xyz - ray_o);
		// re-order intersections to find smallest and largest on each axis
		float3 tMin = min (tTop, tBot);
		float3 tMax = max (tTop, tBot);
		// find the largest tMin and the smallest tMax
		float2 t0 = max (tMin.xx, tMin.yz);
		float largest_tMin = max (t0.x, t0.y);
		t0 = min (tMax.xx, tMax.yz);
		float smallest_tMax = min (t0.x, t0.y);
		// check for hit
		bool hit = (largest_tMin <= smallest_tMax);
		tNear = largest_tMin;
		tFar = smallest_tMax;
		return hit;
	}

    // cast a ray and return distance and material of the surface
    // ro: ray origin
    // rd: ray direction
    // d: unity depth buffer
    float2 castRay(float3 ro, float3 rd, float depth) {
        float t = 0.0;  // length of ray
        float m = -1.0;

		float tmin,tmax;
		bool intersects = IntersectBox(ro, rd, float3(0,0,0), float3(1,1,1)*_ChunkSize, tmin, tmax);
		if(!intersects){
			#if DEBUG_PERFORMANCE
				return float2(_DrawDistance, 0.0);
			#else
				return float2(_DrawDistance, m);
			#endif
		}
		t += tmin;
		t = max(t, 0.0); // disable if you want to see shadow boy lol
		//t = 10.0;	// just trying a linear step t, need to disable intersects above, and tmax check below
        for (int i = 0; i < MAX_STEPS; ++i) {
            // if past the depth buffer, or exceeded the max draw distance, stop and return a transparent material
            // this way raymarched objects and traditional meshes can coexist (with depth)
            if (t >= depth || t > _DrawDistance || t >= tmax) {
				#if DEBUG_PERFORMANCE
					return float2(t, (float)i / MAX_STEPS);
				#else
					return float2(t, -1.0);
				#endif
            }
			float3 p = ro + rd * t;
            float2 r = map(p);

			// if close enough, return current distance and material
			if (r.x <= PRECISION * t + 0.1) { // add a little too (dont really know what im doing just eyeballing raycast to match with mc mesh a little better)
                #if DEBUG_PERFORMANCE
					return float2(t, (float)i / MAX_STEPS);
				#else
					return float2(t, r.y);
				#endif
            }

			// weird clamping shit for point filtering (looks kinda good actually on point noise)
			float fm = _ChunkSize / _TexSize;			
			float3 deltas = (step(0,rd)*fm - fmod(p,fm)) / rd;
			float minc = min(min(deltas.x, deltas.y),deltas.z);
			const float ERR = 0.002;
			t += max(minc + r.x*min(t*ERR,1.0), 0.001); // scaled version of above line so less accurate far away

            //t += r.x;
            m = r.y;    // remember material of closest
        }
        // these two options below only get called if i hits the max
        // basically both options have artifacts so swag ur pick
		#if DEBUG_PERFORMANCE
			return float2(t, 1.0);
		#else
			//return float2(t, m);      // returns whatever last mat was
			return float2(t, -1.0); // returns blank if ray runs out
		#endif
    }

    fixed4 render(float3 ro, float3 rd, float d) {
        fixed4 col = fixed4(0, 0, 0, 0);
        float2 r = castRay(ro, rd, d);
        float t = r.x;
        float m = r.y;
        if (m > -0.5) { // if not should just be transparent
            float3 p = ro + rd*t;
            float3 n = calcNormal(p);
            float light = clamp(dot(-_LightDir.xyz, n), 0.0, 1.0); // unclamped looks cool kinda (shadows go negative color)
                                                                   // using tex2Dlod since tex2D needs to calculate derivatives to figure out mips but this manually defines them
                                                                   // oh actually advantage of using castray function is dont get those warnings cuz not in for loop anymore
            col.xyz = tex2Dlod(_ColorRamp_Material, float4(m, 0, 0, 0)).xyz;
			//float3 fp = floor(p*5.);
			//col.xyz = frac(fp / _VoxelSize);

            col.xyz *= light;

            col.w = 1.0;
        }
        return fixed4(clamp(col, 0.0, 1.0));
    }

    fixed4 renderPerf(float3 ro, float3 rd, float d) {
        float2 r = castRay(ro, rd, d);
        float t = r.x;
        float m = r.y;
        return fixed4(tex2Dlod(_ColorRamp_PerfMap, float4(m, 0, 0, 0)).xyz, 1);
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
        // ray (see vert()). Think of similar triangles: the view-space z-distance between a point
        // and the camera is proportional to the absolute distance.
        float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, duv).r);
        depth *= length(i.ray);

        fixed3 col = tex2D(_MainTex,i.uv);

    #if DEBUG_PERFORMANCE
        fixed4 add = renderPerf(ro, rd, depth);
    #else
        fixed4 add = render(ro, rd, depth);
    #endif

        // Returns final color using alpha blending
        return fixed4(col*(1.0 - add.w) + add.xyz * add.w, 1.0);
    }
        ENDCG
    }
    }
}