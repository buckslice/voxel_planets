// implemented from here, should add some more eventually
//http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

// signed distance functions and operators

//https://www.youtube.com/watch?v=s8nFqwOho-s

float sdSphere(float3 worldPos, float3 origin, float radius) {
    return length(worldPos - origin) - radius;
}

float udBox(float3 p, float3 b) {   // unsigned distance, not really sure looks the same as signed tho...
    return length(max(abs(p) - b, 0.0));
}
float sdBox(float3 p, float3 b) {
    float3 d = abs(p) - b;
    return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}

// t.x is radius, t.y is thickness i think
float sdTorus(float3 worldPos, float3 origin, float2 t) {
    float3 p = worldPos - origin;
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float sdPlane(float3 p, float4 n) {
    // n.xyz must be normalized
    return dot(p, n.xyz) + n.w;
}
float sdPlaneY(float3 p) {
    return p.y;
}


// SD operators
// union
float opUnion(float a, float b) {
    return min(a, b);
}
// union with material data
float2 opUnion(float2 a, float2 b) {
    return (a.x < b.x) ? a : b;
}
// subtraction
float opSubtract(float a, float b) {
    return max(a, -b);
}
// intersection
float opIntersect(float a, float b) {
    return max(a, b);
}

// hlsl fmod is pos so this is glsl mod equivalent
// made as macro so it works with all types automatically
#define mod(x,y) (x-y*floor(x/y))

// repeats the shape (use this on the worldpos passed into shape)
// p is original pos
// c is repeat frequency in each dimension
float3 opRep(float3 p, float3 c) {
    return mod(p,c) - 0.5*c;
}
float opRep1(float p, float c) {
    return mod(p, c) - 0.5*c;
}

// should explore these kinda ones with inout, cool!
float opMod1(inout float p, float size) {
    float hs = size*0.5;
    float c = floor((p + hs) / size);
    p = mod(p, size) - hs;
    //p = opRep1(p, size);
    return c;
}
