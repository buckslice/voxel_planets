// implemented from here, should add some more eventually
//http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

// signed distance functions and operators

float sdSphere(float3 worldPos, float3 origin, float radius) {
    return length(worldPos - origin) - radius;
}

// t.x is radius, t.y is thickness i think
float sdTorus(float3 worldPos, float3 origin, float2 t) {
    float3 p = worldPos - origin;
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}


// SD operators
// union
float opUni(float a, float b) {
    return min(a, b);
}
// union with material data
float2 opUnim(float2 a, float2 b) {
    return (a.x < b.x) ? a : b;
}
// subtraction
float opSub(float a, float b) {
    return max(a, -b);
}
// intersection
float opInt(float a, float b) {
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
