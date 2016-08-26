
Shader "Custom/StandardDiffuseStipple" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Transparency("Transparency", Range(0,1)) = 1.0
        //_ShadowStrength("Shadow Strength", Range(0,1)) = 1.0
    }
    SubShader{
        Tags{ "RenderType" = "Opaque" }
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        half _Transparency;

        struct Input {
            float2 uv_MainTex;
            float4 screenPos;
        };

        static const float4x4 thresholdMatrix = {
            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0,  11.0 / 17.0,
            13.0 / 17.0, 5.0 / 17.0,  15.0 / 17.0, 7.0 / 17.0,
            4.0 / 17.0,  12.0 / 17.0, 2.0 / 17.0,  10.0 / 17.0,
            16.0 / 17.0, 8.0 / 17.0,  14.0 / 17.0, 6.0 / 17.0
        };
        static const float4x4 rowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;

            // Screen-door transparency: Discard pixel if below threshold.
            float2 pos = IN.screenPos.xy / IN.screenPos.w;
            pos *= _ScreenParams.xy; // pixel position
            clip(_Transparency - thresholdMatrix[fmod(pos.x, 4)] * rowAccess[fmod(pos.y, 4)]);
        }
        ENDCG

    }
    Fallback "Diffuse"
}