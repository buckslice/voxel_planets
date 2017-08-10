// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Atmosphere/AtmosphericPP_skybox" 
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

			uniform float3 _Globals_Origin;
			uniform sampler2D _MainTex;
			
			float4 _CameraDepthNormalsTexture_ST;
			sampler2D _CameraDepthNormalsTexture;

			uniform float3 _Sun_WorldSunDir; 
			uniform float4 _Sun_Color;

			float3 OuterSunRadiance(float3 viewdir)
			{
				float3 col = float3(0,0,0);
				float cosA = 0.99995109238; //sun disk angular radius cos(0.0098901990946345) or 34 arcminutes
				
				if ( dot(viewdir, _Sun_WorldSunDir) >= cosA)
					col = _Sun_Color*_Sun_Intensity;
				
			    return max(float3(0,0,0),col);
			}

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
    			
				OUT.dir = mul(unity_ObjectToWorld, v.vertex).xyz 
               	- _WorldSpaceCameraPos;
    			
    			return OUT; 
			}
			
			float4 frag(v2f IN) : COLOR
			{
				half4 col =  tex2D(_MainTex,IN.uv);
			
				float3 d = -normalize(IN.dir);
				
				float3 skyColor = float3(0,0,0);
				
			    float3 sunColor = OuterSunRadiance(d);

			    float3 extinction;
			    float3 inscatter = SkyRadiance(_Globals_Origin-_WorldSpaceCameraPos, d, _Sun_WorldSunDir, extinction, 0.0);

			    skyColor = sunColor * extinction + hdr(inscatter);
			    return float4(skyColor,1);

			}

			ENDCG
		}
	}
 	Fallback off
}
