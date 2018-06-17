
Shader "Noise/NoisePlanet"
{
    Properties
    {
        _Octaves("Octaves", Float) = 5.0
        _Frequency("Frequency", Float) = 1.0
        _Lacunarity("Lacunarity", Float) = 2.0
        _Persistence("Persistence", Float) = 0.5
        _Offset("Offset", Vector) = (0.0, 0.0, 0.0, 0.0)
    }

    CGINCLUDE

    #include "UnityCustomRenderTexture.cginc"
    #include "Assets/Shaders/Includes/Noise.cginc"
    #include "Assets/Shaders/Includes/Shapes.cginc"

    //#pragma target 5.0

    // properties
    fixed _Octaves;
    float _Frequency;
    float3 _Offset;
    float _Lacunarity;
    float _Persistence;
    // uniforms set thru code (uniform not required but nice to have for clarity)
    uniform float3 _LocalOffset;
    uniform float _Resolution;
	uniform float _Size;

    // User facing vertex to fragment structure for initialization materials
    // WOW pretty sure its bugged  so i had to recreate the init stuff here
    // myesss this works tho. just call initialize on the customrendertexture
    // yehaww
    // now just need to fix problem with marching cubes generation lol
    struct v2f_init_customrendertexture2 {
        float4 vertex : SV_POSITION;
        float3 texcoord : TEXCOORD0; //<---- only a float2 in orig source thanks blizzard
        //float3 direction : TEXCOORD1;
    };

    struct appdata_init_customrendertexture2 {
        float4 vertex : POSITION;
        float3 texcoord : TEXCOORD0;
    };

    // standard custom texture vertex shader that should always be used for initialization shaders
    v2f_init_customrendertexture2 InitCustomRenderTextureVertexShader2(appdata_init_customrendertexture2 v) {
        v2f_init_customrendertexture2 o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        //o.texcoord = float3(v.texcoord.xy, _VolumeSlice);
        //o.texcoord = v.texcoord;

		// texels are positioned at pixel center so interpolates from (0.5 / size) -> (1.0 - 0.5 / size) instead of 0 to 1 as we want
		// so basically inverselerp them to get it back to 0 to 1
		float resmm = _Resolution - 1;
		//o.texcoord.x = _Resolution / resmm * v.texcoord.x - 1.0/(2.0*resmm);
		//o.texcoord.y = _Resolution / resmm * v.texcoord.y - 1.0/(2.0*resmm);
        
        o.texcoord.x = _Resolution / resmm * v.texcoord.x - 1.0/(2.*resmm);
        o.texcoord.y = _Resolution / resmm * v.texcoord.y - 1.0/(2.*resmm);
		o.texcoord.z = v.texcoord.z;	// this is already done correctly in the blitting function
        
        o.texcoord *= _Resolution * _Size; // voxelSize
        o.texcoord += _LocalOffset + _Offset;
        // at this point texcoord is basically worldpos into frag 'density' function
        return o;
    }

    //v2f_customrendertexture
    float4 frag(v2f_init_customrendertexture2 IN) : SV_Target
    {
        float f = 0.0;

        //float h = SimplexBillowed(IN.texcoord, _Octaves, _Offset+_LocalOffset, _Frequency, _Amplitude, _Lacunarity, _Persistence);
        //float curl = fbm(IN.texcoord + _Offset + _LocalOffset, 4, 0.1, 0.55, 2.0)*.5;
        //float h = ridged(IN.texcoord + curl + _Offset + _LocalOffset, _Octaves, _Frequency, _Persistence, _Lacunarity);
        //return h;

        // bunch of dumb spheres
        //float rad = 2000.0;
        //float3 p = IN.texcoord;
        //float s = 5500.0;
        //p = opRep(p, float3(s,s,s));
        //f = sdSphere(p, float3(0.0,0.0,0.0), rad);
        //return float4(0.0, 0.0, 0.0, f);

        float r = 14000.0;
        f = sdSphere(IN.texcoord, float3(0.0, 0.0, 0.0), r);
        float curl = fbm(IN.texcoord + _Offset, 4, 0.01, 0.55, 2.0)*10.;
        float n = ridged(IN.texcoord + curl + _Offset, _Octaves, _Frequency, _Persistence, _Lacunarity);
        f += n * 100.0; 
		// bumpy noise on taller up mountains
		float sn = ridged(IN.texcoord, 4, 0.05, 0.5, 2.0);
		f += sn * 5.0 * saturate(-n);

		float cr = saturate((fbm(IN.texcoord, 2, 0.005, 0.5, 2.0)+1.0)/2.0);
		float cg = saturate(rand(IN.texcoord));
		float cb = saturate((fbm(IN.texcoord, 4, 0.001, 0.52, 2.0)+1.0)/2.0);

        return float4(0.5+cr*0.5, 0.2+cb*0.3, 0.1+cg*0.1, f);
    }

    ENDCG

    SubShader {
        Tags { "PreviewType" = "Plane"}
        Cull Off
        ZWrite Off
        Ztest Always
        Lighting Off
        Blend One Zero

        Pass
        {
            Name "Update"
            CGPROGRAM
            //#pragma fragmentoption ARB_precision_hint_fastest

            #pragma vertex InitCustomRenderTextureVertexShader2
            #pragma fragment frag
            ENDCG
        }
    }

    FallBack "Diffuse"
}