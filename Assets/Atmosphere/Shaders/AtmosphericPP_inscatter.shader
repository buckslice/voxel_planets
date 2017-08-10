// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Atmosphere/AtmosphericPP_inscatter" 
{
	Properties {
		_MainTex ("", 2D) = "white" {}
		_Sun_Glare("Sun Glare", 2D) = "black" {}
	}
	SubShader 
	{
		Tags { "Queue"="Background" "RenderType"="Background" }
		Cull Off ZWrite Off Fog { Mode Off }
		
		Pass {
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma glsl

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "Assets/Proland/Shaders/Core/Utility.cginc"
			#include "Assets/Proland/Shaders/Atmo/Atmosphere.cginc"

			uniform float4x4 _Globals_CameraToWorld;
			uniform float4x4 _Globals_ScreenToCamera; 

			uniform float3 _Globals_WorldCameraPos;
			uniform float3 _Globals_Origin;

			uniform sampler2D _MainTex;

			uniform float4 _Sun_Color;
			uniform float3 _Sun_WorldSunDir;

			uniform float TanHalfFOV;
			uniform float ViewAspect;
			
			uniform float _MinDistance;
			
			uniform float4 _CameraDepthNormalsTexture_ST;
			uniform sampler2D _CameraDepthNormalsTexture;

			struct v2f 
			{
    			float4 pos : SV_POSITION;
    			float2 uv : TEXCOORD0;
    			float3 dir : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f OUT;

    			OUT.pos = UnityObjectToClipPos (float4(v.vertex.xyz,1.0));
    			OUT.uv = v.texcoord.xy;
    			
    			OUT.dir = float3(-OUT.pos.x*TanHalfFOV*ViewAspect, 
				-_ProjectionParams.x*OUT.pos.y*TanHalfFOV, OUT.pos.w);

				OUT.dir = mul((float3x3)_Globals_CameraToWorld, OUT.dir);
				OUT.dir = -OUT.dir;
				
    			return OUT; 
			}
			
			float4 frag(v2f IN) : COLOR
			{
				half4 col =  tex2D(_MainTex,IN.uv);
			
				float3 WSD = _Sun_WorldSunDir;
				float3 WCP = _Globals_WorldCameraPos;
			
				float3 viewN;
				
				float depth;
		        DecodeDepthNormal( tex2D(_CameraDepthNormalsTexture,IN.uv), depth, viewN );
		        
		        float z = depth;
		        
				depth = depth*_ProjectionParams.z;
				
				float3 WorldPos = _WorldSpaceCameraPos + IN.dir*depth;
				
				float3 planetColor = col.rgb;
				if (z < 1.0){
					//return float4(WorldPos,1);
					float3 extinction_planet = 1;
					float3 inscatter_planet;
					
					float fade = min(depth, _MinDistance)/_MinDistance;
					inscatter_planet = InScattering(_Globals_Origin-_WorldSpaceCameraPos, _Globals_Origin-WorldPos, WSD, extinction_planet, 0.0);
					
					planetColor = col * extinction_planet + hdr(inscatter_planet) * fade;
				}
				return float4(planetColor,1);
			}

			ENDCG
		}
	}
 	Fallback off
}
