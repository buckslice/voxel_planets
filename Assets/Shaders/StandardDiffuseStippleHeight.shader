
Shader "Custom/StandardDiffuseStippleHeight" {
    Properties{
        _Striations("Striations", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Transparency("Transparency", Range(0,1)) = 1.0
        _LocalOffset("Local Offset", Vector) = (0,0,0,1)
    }
        SubShader{
        Tags{
            "RenderType" = "Opaque"
            "DisableBatching" = "True" // with current setup need to disable this cuz it messes with world positions
        }

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows nolightmap
        #pragma target 3.0
        //#pragma debug

        sampler2D _Striations;
        float4 _Striations_ST;

        half _Glossiness;
        half _Metallic;
        half _Transparency;
        float4 _LocalOffset;

        struct Input {
            float height;
            float3 norm;
            float4 screenPos;   // built in param used for transparency stippling
        };

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // this indicates direction from center of planet to this vertex (up vector)
            float3 pos = v.vertex + _LocalOffset;
            o.norm = v.normal;
            o.height = length(pos);

        }

        static const float4x4 thresholdMatrix = {
            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0,  11.0 / 17.0,
            13.0 / 17.0, 5.0 / 17.0,  15.0 / 17.0, 7.0 / 17.0,
            4.0 / 17.0,  12.0 / 17.0, 2.0 / 17.0,  10.0 / 17.0,
            16.0 / 17.0, 8.0 / 17.0,  14.0 / 17.0, 6.0 / 17.0
        };
        static const float4x4 rowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

        void surf(Input IN, inout SurfaceOutputStandard o) {

            float heightPerturb = 0.2 * IN.norm.y / _Striations_ST.x;
            
            o.Albedo = tex2D(_Striations, float2(IN.height + heightPerturb, IN.norm.x) * _Striations_ST.xy + _Striations_ST.zw);

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