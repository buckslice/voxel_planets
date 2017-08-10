// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Logarithmic/TransparentDiffuse" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass{
		Cull Front
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

	struct inv {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : POSITION;
		float4 worldPos : TEXCOORD0;
		float3 norm: TEXCOORD1;
		float flogz : TEXCOORD2;
	};

	struct Output {
		float4 col : COLOR;
		float dep : DEPTH;
	};

	float4 _Color;
	float _Shininess;
	static const float Fcoef = 1.0 / log2(_ProjectionParams.z + 1.0);

	v2f vert(inv v)
	{
		v2f o;
		o.norm = v.normal;

		o.worldPos = mul(unity_ObjectToWorld, v.vertex);
		o.norm = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);

		// log depth calculations
		o.pos = UnityObjectToClipPos(v.vertex);
		o.pos.z = log2(max(1000, 1.0 + o.pos.w)) * Fcoef;
		o.pos.z *= o.pos.w;
		o.flogz = 1.0 + o.pos.w;
		return o;
	}

	Output frag(v2f i) {
		float3 normalDir = normalize(i.norm);

		float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
		float3 vertexToLight = _WorldSpaceLightPos0.xyz - i.worldPos.xyz;
		float sqrDist = dot(vertexToLight, vertexToLight);
		float atten = max(0.2, 1.0 - sqrDist / (1e6*1e6));
		atten *= atten;

		float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;
		float3 diffuse = max(0.0, dot(normalDir, lightDir)) * _Color * atten;

		Output o;
		o.col.rgb = ambient + diffuse;
		o.col.a = _Color.a;
		o.dep = log2(i.flogz) * Fcoef;
		return o;
	}

	ENDCG
	}

//		Pass{
//		Cull Back
//		ZWrite Off
//		Blend SrcAlpha OneMinusSrcAlpha
//
//		CGPROGRAM
//#pragma vertex vert
//#pragma fragment frag
//#include "UnityCG.cginc"
//
//	struct inv {
//		float4 vertex : POSITION;
//		float3 normal : NORMAL;
//	};
//
//	struct v2f {
//		float4 pos : POSITION;
//		float4 worldPos : TEXCOORD0;
//		float3 norm: TEXCOORD1;
//		float flogz : TEXCOORD2;
//	};
//
//	struct Output {
//		float4 col : COLOR;
//		float dep : DEPTH;
//	};
//
//	float4 _Color;
//	float _Shininess;
//	static const float Fcoef = 1.0 / log2(_ProjectionParams.z + 1.0);
//
//	v2f vert(inv v)
//	{
//		v2f o;
//		o.norm = v.normal;
//
//		o.worldPos = mul(_Object2World, v.vertex);
//		o.norm = normalize(mul(float4(v.normal, 0.0), _World2Object).xyz);
//
//		 log depth calculations
//		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//		o.pos.z = log2(max(1000, 1.0 + o.pos.w)) * Fcoef;
//		o.pos.z *= o.pos.w;
//		o.flogz = 1.0 + o.pos.w;
//		return o;
//	}
//
//	Output frag(v2f i) {
//		float3 normalDir = normalize(i.norm);
//
//		float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
//		float3 vertexToLight = _WorldSpaceLightPos0.xyz - i.worldPos.xyz;
//		float sqrDist = dot(vertexToLight, vertexToLight);
//		float atten = max(0.2, 1.0 - sqrDist / (1e6*1e6));
//		atten *= atten;
//
//		float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;
//		float3 diffuse = max(0.0, dot(normalDir, lightDir)) * _Color * atten;
//
//		Output o;
//		o.col.rgb = ambient + diffuse;
//		o.col.a = _Color.a;
//		o.dep = log2(i.flogz) * Fcoef;
//		return o;
//	}
//
//	ENDCG
//	}

	}
		//FallBack "Diffuse"
}
