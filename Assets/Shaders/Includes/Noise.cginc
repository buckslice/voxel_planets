
// PRNG function.
float nrand(float2 uv, float salt) {
    uv += float2(salt, 0);
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
// 3D version
float rand(float3 p) {
    return frac(sin(dot(p.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
}

// other numbers you can try??
//return fract(sin(dot(n, vec3(95.43583, 93.323197, 94.993431))) * 65536.32);


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
float4 FAST32_hash_3D_Cell(float3 gridcell)	//	generates 4 different random numbers for the single given cell point
{
    //    gridcell is assumed to be an integer coordinate

    //	TODO: 	these constants need tweaked to find the best possible noise.
    //			probably requires some kind of brute force computational searching or something....
    const float2 OFFSET = float2(50.0, 161.0);
    const float DOMAIN = 69.0;
    const float4 SOMELARGEFLOATS = float4(635.298681, 682.357502, 668.926525, 588.255119);
    const float4 ZINC = float4(48.500388, 65.294118, 63.934599, 63.279683);

    //	truncate the domain
    gridcell.xyz = gridcell - floor(gridcell * (1.0 / DOMAIN)) * DOMAIN;
    gridcell.xy += OFFSET.xy;
    gridcell.xy *= gridcell.xy;
    return frac((gridcell.x * gridcell.y) * (1.0 / (SOMELARGEFLOATS + gridcell.zzzz * ZINC)));
}
static const int MinVal = -1;
static const int MaxVal = 1;

// distanceFunc
// 1 = euclidian
// 2 = euclidian squared?


// cool celltype , distance func combos
// 1 7

float worleyNoise(float3 xyz, int cellType, int distanceFunction) {
    int xi = int(floor(xyz.x));
    int yi = int(floor(xyz.y));
    int zi = int(floor(xyz.z));

    float xf = xyz.x - float(xi);
    float yf = xyz.y - float(yi);
    float zf = xyz.z - float(zi);

    float dist1 = 9999999.0;
    float dist2 = 9999999.0;
    float dist3 = 9999999.0;
    float dist4 = 9999999.0;
    float3 cell;

    for (int z = MinVal; z <= MaxVal; z++) {
        for (int y = MinVal; y <= MaxVal; y++) {
            for (int x = MinVal; x <= MaxVal; x++) {
                cell = FAST32_hash_3D_Cell(float3(xi + x, yi + y, zi + z)).xyz;
                cell.x += (float(x) - xf);
                cell.y += (float(y) - yf);
                cell.z += (float(z) - zf);
                float dist = 0.0;
                if (distanceFunction <= 1) {
                    dist = sqrt(dot(cell, cell));
                } else if (distanceFunction > 1 && distanceFunction <= 2) {
                    dist = dot(cell, cell); 
                } else if (distanceFunction > 2 && distanceFunction <= 3) {
                    dist = abs(cell.x) + abs(cell.y) + abs(cell.z);
                    dist *= dist;
                } else if (distanceFunction > 3 && distanceFunction <= 4) {
                    dist = max(abs(cell.x), max(abs(cell.y), abs(cell.z)));
                    dist *= dist;
                } else if (distanceFunction > 4 && distanceFunction <= 5) {
                    dist = dot(cell, cell) + cell.x*cell.y + cell.x*cell.z + cell.y*cell.z;
                } else if (distanceFunction > 5 && distanceFunction <= 6) {
                    dist = pow(abs(cell.x*cell.x*cell.x*cell.x + cell.y*cell.y*cell.y*cell.y + cell.z*cell.z*cell.z*cell.z), 0.25);
                } else if (distanceFunction > 6 && distanceFunction <= 7) {
                    dist = sqrt(abs(cell.x)) + sqrt(abs(cell.y)) + sqrt(abs(cell.z));
                    dist *= dist;
                }
                if (dist < dist1) {
                    dist4 = dist3;
                    dist3 = dist2;
                    dist2 = dist1;
                    dist1 = dist;
                } else if (dist < dist2) {
                    dist4 = dist3;
                    dist3 = dist2;
                    dist2 = dist;
                } else if (dist < dist3) {
                    dist4 = dist3;
                    dist3 = dist;
                } else if (dist < dist4) {
                    dist4 = dist;
                }
            }
        }
    }

    if (cellType <= 1)	// F1
        return dist1;	//	scale return value from 0.0->1.333333 to 0.0->1.0  	(2/3)^2 * 3  == (12/9) == 1.333333
    else if (cellType > 1 && cellType <= 2)	// F2
        return dist2;
    else if (cellType > 2 && cellType <= 3)	// F3
        return dist3;
    else if (cellType > 3 && cellType <= 4)	// F4
        return dist4;
    else if (cellType > 4 && cellType <= 5)	// F2 - F1 
        return dist2 - dist1;
    else if (cellType > 5 && cellType <= 6)	// F3 - F2 
        return dist3 - dist2;
    else if (cellType > 6 && cellType <= 7)	// F1 + F2/2
        return dist1 + dist2 / 2.0;
    else if (cellType > 7 && cellType <= 8)	// F1 * F2
        return dist1 * dist2;
    else if (cellType > 8 && cellType <= 9)	// Crackle
        return max(1.0, 10 * (dist2 - dist1));
    else
        return dist1;
}
float worley(float3 p, int octaves, float frequency, float persistence, float lacunarity, int cellType, int distanceFunction) {
    float sum = 0;
    float amplitude = 1.0;
    for (int i = 0; i < octaves; i++) {
        float h = 0;
        h = worleyNoise(p * frequency, cellType, distanceFunction);
        sum += h * amplitude;
        frequency *= lacunarity;
        amplitude *= persistence;
    }
    return sum;
}