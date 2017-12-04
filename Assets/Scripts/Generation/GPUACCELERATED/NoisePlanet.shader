//
//	Code repository for GPU noise development blog
//	http://briansharpe.wordpress.com
//	https://github.com/BrianSharpe
//
//	I'm not one for copyrights.  Use the code however you wish.
//	All I ask is that credit be given back to the blog or myself when appropriate.
//	And also to let me know if you come up with any changes, improvements, thoughts or interesting uses for this stuff. :)
//	Thanks!
//
//	Brian Sharpe
//	brisharpe CIRCLE_A yahoo DOT com
//	http://briansharpe.wordpress.com
//	https://github.com/BrianSharpe
//
//===============================================================================
//  Scape Software License
//===============================================================================
//
//Copyright (c) 2007-2012, Giliam de Carpentier
//All rights reserved.
//
//Redistribution and use in source and binary forms, with or without
//modification, are permitted provided that the following conditions are met: 
//
//1. Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer. 
//2. Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
//
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNERS OR CONTRIBUTORS BE LIABLE 
//FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
//DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
//CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
//OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.;

Shader "Noise/NoisePlanet"
{
    Properties
    {
        _Octaves("Octaves", Float) = 5.0
        _Frequency("Frequency", Float) = 1.0
        //_Amplitude("Amplitude", Float) = 1.0
        _Lacunarity("Lacunarity", Float) = 2.0
        _Persistence("Persistence", Float) = 0.5
        _Offset("Offset", Vector) = (0.0, 0.0, 0.0, 0.0)
    }

    CGINCLUDE

    #include "UnityCustomRenderTexture.cginc"
    #include "NoiseLib.cginc"

    //#pragma target 5.0

    fixed _Octaves;
    float _Frequency;
    float _Amplitude;
    float3 _Offset;
    uniform float3 _LocalOffset;
    uniform float _Resolution;
	uniform float _Size;

    float _Lacunarity;
    float _Persistence;

    // User facing vertex to fragment structure for initialization materials
    // WOW pretty sure its bugged  so i had to recreate the init stuff here
    // myesss this works tho. just call initialize on the customrendertexture
    // yehaww
    // now just need to fix problem with marching cubes generation lol
    struct v2f_init_customrendertexture2 {
        float4 vertex : SV_POSITION;
        float3 texcoord : TEXCOORD0; //<---- float2 in orig source thanks blizzard
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
		o.texcoord.x = _Resolution / resmm * v.texcoord.x - 1.0/(2.0*resmm);
		o.texcoord.y = _Resolution / resmm * v.texcoord.y - 1.0/(2.0*resmm);
		o.texcoord.z = v.texcoord.z;	// this is already done correctly in the blitting function
        o.texcoord *= _Resolution * _Size; // voxelSize
        o.texcoord += _LocalOffset;
        // at this point texcoord is basically worldpos into frag 'density' function
        return o;
    }

    //v2f_customrendertexture
    float frag(v2f_init_customrendertexture2 IN) : SV_Target
    {
        //float h = SimplexBillowed(IN.texcoord, _Octaves, _Offset+_LocalOffset, _Frequency, _Amplitude, _Lacunarity, _Persistence);

        //float curl = fbm(IN.texcoord + _Offset + _LocalOffset, 4, 0.1, 0.55, 2.0)*.5;
        //float h = ridged(IN.texcoord + curl + _Offset + _LocalOffset, _Octaves, _Frequency, _Persistence, _Lacunarity);
        //return h;

        float rad = 2000.0;
        float3 p = IN.texcoord;
        float s = 5500.0;
        p = Repeat(p, float3(s,s,s));
        float f = Sphere(p, float3(0.0,0.0,0.0), rad);
        //float g = Sphere(p, float3(5000., 0., 0.), rad);
        //return Intersection(f, g);

        return f;
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