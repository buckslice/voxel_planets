// density functions
float Sphere(float3 worldPos, float3 origin, float radius) {
    return length(worldPos - origin) - radius;
}

float Torus(float3 worldPos, float3 origin, float2 t) {
    float3 p = worldPos - origin;
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float Union(float a, float b) {
    return min(a, b);
}

float Subtraction(float a, float b) {
    return max(a, -b);
}

float Intersection(float a, float b) {
    return max(a, b);
}

// hlsl fmod is pos so this is glsl mod equivalent
// made as macro so it works with all types automatically
#define mod(x,y) (x-y*floor(x/y))

// p is original pos
// c is repeat frequency in each dimension
float3 Repeat(float3 p, float3 c) {
    float3 q = mod(p,c) - 0.5*c;
    return q;
}


// PRNG function.
float nrand(float2 uv, float salt) {
    uv += float2(salt, 0);
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
// 3D version
float rand(float3 p) {
    return frac(sin(dot(p.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
}

///////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////
//https://github.com/keijiro/NoiseShader/blob/master/Assets/HLSL/SimplexNoise3D.hlsl

float3 mod289(float3 x) {
    return x - floor(x / 289.0) * 289.0;
}

float4 mod289(float4 x) {
    return x - floor(x / 289.0) * 289.0;
}

float4 permute(float4 x) {
    return mod289((x * 34.0 + 1.0) * x);
}

float4 taylorInvSqrt(float4 r) {
    return 1.79284291400159 - r * 0.85373472095314;
}

float snoise(float3 v) {
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);

    // First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    // x1 = x0 - i1  + 1.0 * C.xxx;
    // x2 = x0 - i2  + 2.0 * C.xxx;
    // x3 = x0 - 1.0 + 3.0 * C.xxx;
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - 0.5;

    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    float4 p =
        permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0))
            + i.y + float4(0.0, i1.y, i2.y, 1.0))
            + i.x + float4(0.0, i1.x, i2.x, 1.0));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)

    float4 x_ = floor(j / 7.0);
    float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)

    float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
    float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;

    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 g0 = float3(a0.xy, h.x);
    float3 g1 = float3(a0.zw, h.y);
    float3 g2 = float3(a1.xy, h.z);
    float3 g3 = float3(a1.zw, h.w);

    // Normalise gradients
    float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
    g0 *= norm.x;
    g1 *= norm.y;
    g2 *= norm.z;
    g3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    m = m * m;

    float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
    return 42.0 * dot(m, px);
}

float4 snoise_grad(float3 v) {
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);

    // First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    // x1 = x0 - i1  + 1.0 * C.xxx;
    // x2 = x0 - i2  + 2.0 * C.xxx;
    // x3 = x0 - 1.0 + 3.0 * C.xxx;
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - 0.5;

    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    float4 p =
        permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0))
            + i.y + float4(0.0, i1.y, i2.y, 1.0))
            + i.x + float4(0.0, i1.x, i2.x, 1.0));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)

    float4 x_ = floor(j / 7.0);
    float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)

    float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
    float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;

    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 g0 = float3(a0.xy, h.x);
    float3 g1 = float3(a0.zw, h.y);
    float3 g2 = float3(a1.xy, h.z);
    float3 g3 = float3(a1.zw, h.w);

    // Normalise gradients
    float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
    g0 *= norm.x;
    g1 *= norm.y;
    g2 *= norm.z;
    g3 *= norm.w;

    // Compute noise and gradient at P
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    float4 m2 = m * m;
    float4 m3 = m2 * m;
    float4 m4 = m2 * m2;
    float3 grad =
        -6.0 * m3.x * x0 * dot(x0, g0) + m4.x * g0 +
        -6.0 * m3.y * x1 * dot(x1, g1) + m4.y * g1 +
        -6.0 * m3.z * x2 * dot(x2, g2) + m4.z * g2 +
        -6.0 * m3.w * x3 * dot(x3, g3) + m4.w * g3;
    float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
    return 42.0 * float4(grad, dot(m4, px));
}

float fbm(float3 x, int octaves, float frequency, float persistence, float lacunarity) {
    float sum = 0.0;
    float amplitude = 1.0;
    for (int i = 0; i < octaves; i++) {
        sum += snoise(x * frequency) * amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    return sum;
}

float ridged(float3 x, int octaves, float frequency, float persistence, float lacunarity) {
    float sum = 0.0;
    float amplitude = 1.0;
    for (int i = 0; i < octaves; i++) {
        float n = snoise(x * frequency);
        n = 1.0 - abs(n);
        sum += n * amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    return (sum - 1.1)*1.25;
}

///////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////


// FROM TURBULENCE LIBRARY
//
//	FAST32_hash
//	A very fast hashing function.  Requires 32bit support.
//	http://briansharpe.wordpress.com/2011/11/15/a-fast-and-simple-32bit-floating-point-hash-function/
//
//	The hash formula takes the form....
//	hash = mod( coord.x * coord.x * coord.y * coord.y, SOMELARGEFLOAT ) / SOMELARGEFLOAT
//	We truncate and offset the domain to the most interesting part of the noise.
//	SOMELARGEFLOAT should be in the range of 400.0->1000.0 and needs to be hand picked.  Only some give good results.
//	3D Noise is achieved by offsetting the SOMELARGEFLOAT value by the Z coordinate
//
void FAST32_hash_3D(float3 gridcell,
    float3 v1_mask,		//	user definable v1 and v2.  ( 0's and 1's )
    float3 v2_mask,
    out float4 hash_0,
    out float4 hash_1,
    out float4 hash_2)		//	generates 3 random numbers for each of the 4 3D cell corners.  cell corners:  v0=0,0,0  v3=1,1,1  the other two are user definable
{
    //    gridcell is assumed to be an integer coordinate

    //	TODO: 	these constants need tweaked to find the best possible noise.
    //			probably requires some kind of brute force computational searching or something....
    const float2 OFFSET = float2(50.0, 161.0);
    const float DOMAIN = 69.0;
    const float3 SOMELARGEFLOATS = float3(635.298681, 682.357502, 668.926525);
    const float3 ZINC = float3(48.500388, 65.294118, 63.934599);

    //	truncate the domain
    gridcell.xyz = gridcell.xyz - floor(gridcell.xyz * (1.0 / DOMAIN)) * DOMAIN;
    float3 gridcell_inc1 = step(gridcell, float3(DOMAIN - 1.5, DOMAIN - 1.5, DOMAIN - 1.5)) * (gridcell + 1.0);

    //	compute x*x*y*y for the 4 corners
    float4 P = float4(gridcell.xy, gridcell_inc1.xy) + OFFSET.xyxy;
    P *= P;
    float4 V1xy_V2xy = lerp(P.xyxy, P.zwzw, float4(v1_mask.xy, v2_mask.xy));		//	apply mask for v1 and v2
    P = float4(P.x, V1xy_V2xy.xz, P.z) * float4(P.y, V1xy_V2xy.yw, P.w);

    //	get the lowz and highz mods
    float3 lowz_mods = float3(1.0 / (SOMELARGEFLOATS.xyz + gridcell.zzz * ZINC.xyz));
    float3 highz_mods = float3(1.0 / (SOMELARGEFLOATS.xyz + gridcell_inc1.zzz * ZINC.xyz));

    //	apply mask for v1 and v2 mod values
    v1_mask = (v1_mask.z < 0.5) ? lowz_mods : highz_mods;
    v2_mask = (v2_mask.z < 0.5) ? lowz_mods : highz_mods;

    //	compute the final hash
    hash_0 = frac(P * float4(lowz_mods.x, v1_mask.x, v2_mask.x, highz_mods.x));
    hash_1 = frac(P * float4(lowz_mods.y, v1_mask.y, v2_mask.y, highz_mods.y));
    hash_2 = frac(P * float4(lowz_mods.z, v1_mask.z, v2_mask.z, highz_mods.z));
}
//
//	Given an arbitrary 3D point this calculates the 4 vectors from the corners of the simplex pyramid to the point
//	It also returns the integer grid index information for the corners
//
void Simplex3D_GetCornerVectors(float3 P,					//	input point
    out float3 Pi,			//	integer grid index for the origin
    out float3 Pi_1,			//	offsets for the 2nd and 3rd corners.  ( the 4th = Pi + 1.0 )
    out float3 Pi_2,
    out float4 v1234_x,		//	vectors from the 4 corners to the intput point
    out float4 v1234_y,
    out float4 v1234_z) {
    //
    //	Simplex math from Stefan Gustavson's and Ian McEwan's work at...
    //	http://github.com/ashima/webgl-noise
    //

    //	simplex math constants
    const float SKEWFACTOR = 1.0 / 3.0;
    const float UNSKEWFACTOR = 1.0 / 6.0;
    const float SIMPLEX_CORNER_POS = 0.5;
    const float SIMPLEX_PYRAMID_HEIGHT = 0.70710678118654752440084436210485;	// sqrt( 0.5 )	height of simplex pyramid.

    P *= SIMPLEX_PYRAMID_HEIGHT;		// scale space so we can have an approx feature size of 1.0  ( optional )

                                        //	Find the vectors to the corners of our simplex pyramid
    Pi = floor(P + dot(P, float3(SKEWFACTOR, SKEWFACTOR, SKEWFACTOR)));
    float3 x0 = P - Pi + dot(Pi, float3(UNSKEWFACTOR, UNSKEWFACTOR, UNSKEWFACTOR));
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    Pi_1 = min(g.xyz, l.zxy);
    Pi_2 = max(g.xyz, l.zxy);
    float3 x1 = x0 - Pi_1 + UNSKEWFACTOR;
    float3 x2 = x0 - Pi_2 + SKEWFACTOR;
    float3 x3 = x0 - SIMPLEX_CORNER_POS;

    //	pack them into a parallel-friendly arrangement
    v1234_x = float4(x0.x, x1.x, x2.x, x3.x);
    v1234_y = float4(x0.y, x1.y, x2.y, x3.y);
    v1234_z = float4(x0.z, x1.z, x2.z, x3.z);
}
//
//	Calculate the weights for the 3D simplex surflet
//
float4 Simplex3D_GetSurfletWeights(float4 v1234_x,
    float4 v1234_y,
    float4 v1234_z) {
    //	perlins original implementation uses the surlet falloff formula of (0.6-x*x)^4.
    //	This is buggy as it can cause discontinuities along simplex faces.  (0.5-x*x)^3 solves this and gives an almost identical curve

    //	evaluate surflet. f(x)=(0.5-x*x)^3
    float4 surflet_weights = v1234_x * v1234_x + v1234_y * v1234_y + v1234_z * v1234_z;
    surflet_weights = max(0.5 - surflet_weights, 0.0);		//	0.5 here represents the closest distance (squared) of any simplex pyramid corner to any of its planes.  ie, SIMPLEX_PYRAMID_HEIGHT^2
    return surflet_weights*surflet_weights*surflet_weights;
}
//
//	SimplexPerlin3D  ( simplex gradient noise )
//	Perlin noise over a simplex (tetrahedron) grid
//	Return value range of -1.0->1.0
//	http://briansharpe.files.wordpress.com/2012/01/simplexperlinsample.jpg
//
//	Implementation originally based off Stefan Gustavson's and Ian McEwan's work at...
//	http://github.com/ashima/webgl-noise
//
float SimplexPerlin3D(float3 P) {
    //	calculate the simplex vector and index math
    float3 Pi;
    float3 Pi_1;
    float3 Pi_2;
    float4 v1234_x;
    float4 v1234_y;
    float4 v1234_z;
    Simplex3D_GetCornerVectors(P, Pi, Pi_1, Pi_2, v1234_x, v1234_y, v1234_z);

    //	generate the random vectors
    //	( various hashing methods listed in order of speed )
    float4 hash_0;
    float4 hash_1;
    float4 hash_2;
    FAST32_hash_3D(Pi, Pi_1, Pi_2, hash_0, hash_1, hash_2);
    hash_0 -= 0.49999;
    hash_1 -= 0.49999;
    hash_2 -= 0.49999;

    //	evaluate gradients
    float4 grad_results = rsqrt(hash_0 * hash_0 + hash_1 * hash_1 + hash_2 * hash_2) * (hash_0 * v1234_x + hash_1 * v1234_y + hash_2 * v1234_z);

    //	Normalization factor to scale the final result to a strict 1.0->-1.0 range
    //	x = sqrt( 0.75 ) * 0.5
    //	NF = 1.0 / ( x * ( ( 0.5 ? x*x ) ^ 3 ) * 2.0 )
    //	http://briansharpe.wordpress.com/2012/01/13/simplex-noise/#comment-36
    const float FINAL_NORMALIZATION = 37.837227241611314102871574478976;

    //	sum with the surflet and return
    return dot(Simplex3D_GetSurfletWeights(v1234_x, v1234_y, v1234_z), grad_results) * FINAL_NORMALIZATION;
}