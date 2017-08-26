#ifndef WFS_DIFFUSE_INCLUDED
#define WFS_DIFFUSE_INCLUDED


// WFS_TWOSIDED is a Cutout option
#if defined(WFS_TWOSIDED) && !defined(WFS_PASS_META)
	#define _ALPHATEST_ON
#endif



#include "AutoLight.cginc"
#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"
#include "UnityStandardUtils.cginc"




struct v2f{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 posWorld : TEXCOORD1;
    float3 dist : TEXCOORD2;
    half3 ambient : TEXCOORD4;
    #ifdef _NORMALMAP
        float3 tSpace[3] : TEXCOORD5;
    #else
    half3 worldNormal : TEXCOORD5;
    #endif
    SHADOW_COORDS(8)
    UNITY_FOG_COORDS(9)
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#include "WFSCG.cginc"

// sampler2D _MainTex;
// #ifdef _NORMALMAP
//     sampler2D _BumpMap;
// #endif
// fixed4 _Color;
// float4 _MainTex_ST;

v2f vert(appdata_tan v){
    UNITY_SETUP_INSTANCE_ID(v);
    v2f o;
    UNITY_INITIALIZE_OUTPUT(v2f,o);
    UNITY_TRANSFER_INSTANCE_ID(v,o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.pos = UnityObjectToClipPos(v.vertex);
    o.posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    half3 worldNormal = UnityObjectToWorldNormal(v.normal);

    o.ambient = 0;
    #ifdef VERTEXLIGHT_ON
	    o.ambient += Shade4PointLights (
	      unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
	      unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
	      unity_4LightAtten0, o.posWorld , worldNormal);
    #endif
    o.ambient += max(half3(0,0,0), ShadeSH9 (half4(worldNormal, 1.0)));

    #ifdef _NORMALMAP
        float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
        float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
        float3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
        o.tSpace[0] = worldTangent;
        o.tSpace[1] = worldBinormal;
        o.tSpace[2] = worldNormal;
    #else
        o.worldNormal = worldNormal;
    #endif

    TRANSFER_SHADOW(o)
    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}

fixed4 frag (v2f i) : SV_Target{
    UNITY_SETUP_INSTANCE_ID(i);

    fixed4 col = tex2D(_MainTex, i.uv) * _Color;


	fixed4 surfCol = col;
	half mask;
	float fade, wire;
	float3 thickness;
	half3 emission = half3(0,0,0);
	WFSWire(i.dist, i.uv, mask, thickness, wire);

	#ifdef _FADE_ON
		WFSFade(length(i.posWorld - _WorldSpaceCameraPos), wire, fade);
	#endif

	#ifdef _GLOW_ON
		WFSGlow(i.dist, thickness, mask, fade, col, emission);
	#endif

	half4 wireCol = WFSWireColor(i.dist, thickness, i.uv);
					
	// BLEND
	wireCol.rgb = lerp(surfCol.rgb, wireCol.rgb, wireCol.a); // alpha blend wire to surface
	half transparency = lerp(surfCol.a*_WTransparency, _WTransparency, wireCol.a);

	// WIRELERP
	col.rgb = lerp(wireCol.rgb, col.rgb, wire);
	col.a = lerp(transparency, col.a, wire);

	#if defined(_EMISSION) && !defined(_WLIGHT_UNLIT)
		// BLEND
		half3 wireEmi = lerp(0, _WEmission*wireCol.rgb, wireCol.a);
		// WIRELERP
		emission = lerp(emission, lerp(wireEmi, emission, wire), col.a);
	#endif

	#if defined(_ALPHATEST_ON)
		clip(col.a - _Cutoff);
	#endif 

	#ifdef _WLIGHT_UNLIT
		half3 albedo = col.rgb;
	#endif


	// Lighting
    #ifndef USING_DIRECTIONAL_LIGHT
        half3 lightDir = normalize(UnityWorldSpaceLightDir(i.posWorld));
    #else
        half3 lightDir = _WorldSpaceLightPos0.xyz;
    #endif

    #ifdef _NORMALMAP
        float3 normal = UnpackNormal(tex2D (_BumpMap, i.uv));
        half3 worldN = normalize(i.tSpace[0] * normal.x + i.tSpace[0] * normal.y + i.tSpace[2] * normal.z);
        #ifdef _WLIGHT_OVERLAY
        	worldN = lerp(normalize(i.tSpace[2].xyz), worldN, wire);
        #endif
    #else
        half3 worldN = i.worldNormal;
    #endif



    half nl = max (0, dot (worldN, lightDir));
    half diff = nl * _LightColor0.rgb;
    UNITY_LIGHT_ATTENUATION(atten, i, i.posWorld)
    half3 lighting = diff * atten + i.ambient;
    col.rgb *= lighting;




    #ifdef _EMISSION
    	col.rgb += emission;
	#endif
	#ifdef _WLIGHT_UNLIT
		col.rgb = lerp(albedo, col.rgb, wire);
	#endif


    UNITY_APPLY_FOG(i.fogCoord, c);
    return col;
}

#include "WFSGeom.cginc"

#endif // WFS_DIFFUSE_INCLUDED