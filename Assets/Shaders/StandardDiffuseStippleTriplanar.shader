
Shader "Custom/StandardDiffuseStippleTriplanar" {
    Properties{
        _Side("Side", 2D) = "white" {}
        _Top("Top", 2D) = "white" {}
        _Bottom("Bottom", 2D) = "white" {}
        
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Transparency("Transparency", Range(0,1)) = 1.0
        _TriplanarBlendSharpness("Triplanar Blend", Range(0.1, 32)) = 10.0
        _PlanetCenter("Planet Center", Vector) = (0,0,0,1)
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

        sampler2D _Side, _Top, _Bottom;
        float4 _Side_ST;
        float4 _Top_ST;
        float4 _Bottom_ST;

        half _Glossiness;
        half _Metallic;
        half _Transparency;
        float _TriplanarBlendSharpness;
        float4 _PlanetCenter;

        struct Input {
            float3 wp;
            float3 norm;
            float4 screenPos;
        };

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            o.wp = mul(unity_ObjectToWorld, v.vertex);

            // random shaderforge wiki stuff i tried (maybe should try in surf function)
            //http://acegikmo.com/shaderforge/wiki/index.php?title=File:Sf_object_space_11112015.png
            //o.wp = mul(unity_WorldToObject, float4((o.wp - v.vertex.xyz), 0)).xyz;
            //o.norm = pow(abs(mul(unity_WorldToObject, float4(v.normal, 0)).xyz), _TriplanarBlendSharpness);;
            //o.norm = o.norm / (o.norm.x + o.norm.y + o.norm.z);

            //o.wp = v.vertex;  // if change back to all meshes centered around planet (the way should prob be done
            //planets cant be rotated or move through space right now with this way
            //o.wp = normalize(v.vertex);   // looks crazy XD
            float3 newY = normalize(o.wp - _PlanetCenter.xyz);  // needs to be center of planet
            // going to be a stripe line around the poles
            // caused by uvs having to stretch quickly across one triangle as soon as this if statement flips
            // this looks fine on surface but pretty bad when moving far away (should def revisit this later)
            // also wont work when planets move / rotate (cant use world space, need to use local space)
            // also bottom of planet looks wierd lol
            if (abs(newY.y) > .999) { 
                o.norm = v.normal;
            } else {
                // construct change of basis to rotate normal to the up vector
                float3 newZ = normalize(cross(newY, float3(0, 1, 0)));
                float3 newX = normalize(cross(newY, newZ));
                float3x3 transform = float3x3(newX, newY, newZ);
                o.norm = mul(transform, v.normal);  // apply change of basis to normal

                // also do it to world pos, not sure how this works (look at sphere with debug textures)
                o.wp = mul(transform, v.vertex);
            }

            // alternate attempt which looks worse than above
            //float3 up = normalize(o.wp - float3(0, -14000.0, 0));
            //float3 right = normalize(cross(up, float3(0, 1, 0)));
            //float3 forward = normalize(cross(right, up));
            //o.norm = float3(dot(v.normal, right), dot(v.normal, up), dot(v.normal, forward));

        }

        static const float4x4 thresholdMatrix = {
            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0,  11.0 / 17.0,
            13.0 / 17.0, 5.0 / 17.0,  15.0 / 17.0, 7.0 / 17.0,
            4.0 / 17.0,  12.0 / 17.0, 2.0 / 17.0,  10.0 / 17.0,
            16.0 / 17.0, 8.0 / 17.0,  14.0 / 17.0, 6.0 / 17.0
        };
        static const float4x4 rowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };

        void surf(Input IN, inout SurfaceOutputStandard o) {
            //float3 projNormal = saturate(pow(abs(IN.norm) * 1.4, 4));

            //// SIDE X
            //float3 x = tex2D(_Side, frac(IN.wp.yz * _Side_ST.xy + _Side_ST.zw)) * abs(IN.norm.x);

            //// TOP / BOTTOM
            //float3 y = 0;
            //if (IN.norm.y > 0) {
            //    y = tex2D(_Top, frac(IN.wp.xz * _Top_ST.xy + _Top_ST.zw)) * abs(IN.norm.y);
            //} else {
            //    y = tex2D(_Bottom, frac(IN.wp.xz * _Bottom_ST.xy + _Bottom_ST.zw)) * abs(IN.norm.y);
            //}

            //// SIDE Z	
            //float3 z = tex2D(_Side, frac(IN.wp.xy * _Side_ST.xy + _Side_ST.zw)) * abs(IN.norm.z);

            //o.Albedo = z;
            //o.Albedo = lerp(o.Albedo, x, projNormal.x);
            //o.Albedo = lerp(o.Albedo, y, projNormal.y);

            
            float3 x = tex2D(_Side, IN.wp.zy * _Side_ST.xy + _Side_ST.zw);
            float3 y = 0;
            if (IN.norm.y > 0) {
                y = tex2D(_Top, IN.wp.xz * _Top_ST.xy + _Top_ST.zw);
            } else { // do side tex still if on roof
                y = tex2D(_Side, IN.wp.xz * _Top_ST.xy + _Top_ST.zw);
            }
            float3 z = tex2D(_Side, IN.wp.xy * _Side_ST.xy + _Side_ST.zw);

            // Get the absolute value of the world normal.
            // Put the blend weights to the power of BlendSharpness, the higher the value, 
            // the sharper the transition between the planar maps will be.
            half3 blend = pow(abs(IN.norm), _TriplanarBlendSharpness);
            // Divide our blend mask by the sum of it's components, this will make x+y+z=1
            blend = blend / (blend.x + blend.y + blend.z);
            // Finally, blend together all three samples based on the blend mask.
            o.Albedo = x * blend.x + y * blend.y + z * blend.z;


            // NO BLEND MODE
            //******************************************************************
            //fixed4 c;
            //if (abs(IN.norm.x) > 0.6) {
            //    c = tex2D(_Side, IN.wp.yz * _Side_ST.xy + _Side_ST.zw);
            //} else if(abs(IN.norm.z) > 0.6){
            //    c = tex2D(_Side, IN.wp.xy * _Side_ST.xy + _Side_ST.zw);
            //} else if(IN.norm.y < 0){   // if on roof then still do side tex
            //    c = tex2D(_Side, IN.wp.xz * _Top_ST.xy + _Top_ST.zw);
            //} else {
            //    c = tex2D(_Top, IN.wp.xz * _Top_ST.xy + _Top_ST.zw);
            //}
            //o.Albedo = c.rgb;
            //******************************************************************

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