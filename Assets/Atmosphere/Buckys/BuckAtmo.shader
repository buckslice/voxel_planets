Shader "Custom/BuckAtmo" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Scattering", 2D) = "white" {}
	}
	SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off

        Cull Front
        CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf CustomLambert alpha:fade noshadow noambient

            // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        half4 LightingCustomLambert(SurfaceOutput s, half3 lightDir, half atten) {
            half NdotL = dot(s.Normal, lightDir);
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
            c.a = max(s.Alpha * NdotL, 0.01);   // never fully alpha (for gameplay purposes)
            return c;
        }

        struct Input {
            float2 angleAndDist;   // .x is angle to sun (dot product), .y is dist to atmo (density kinda)
            float2 uv_MainTex;
        };

        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutput o) {
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG



        //Cull Back
        //CGPROGRAM
        //    // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf CustomLambert alpha:fade noshadow noambient

        //    // Use shader model 3.0 target, to get nicer looking lighting
        //#pragma target 3.0

        //sampler2D _MainTex;

        //half4 LightingCustomLambert(SurfaceOutput s, half3 lightDir, half atten) {
        //    half NdotL = dot(s.Normal, lightDir);
        //    half4 c;
        //    c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
        //    c.a = max(s.Alpha * NdotL, 0.01);
        //    return c;
        //}

        //struct Input {
        //    float2 uv_MainTex;
        //};

        //fixed4 _Color;

        //void surf(Input IN, inout SurfaceOutput o) {
        //    // Albedo comes from a texture tinted by color
        //    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
        //    o.Albedo = c.rgb;
        //    o.Alpha = c.a;
        //}
        //ENDCG
        
	}
	FallBack "Diffuse"
}
