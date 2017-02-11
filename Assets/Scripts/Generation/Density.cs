using UnityEngine;

public class Density  {

    private static float Sphere(Vector3 worldPos, Vector3 origin, float radius) {
        return (worldPos - origin).magnitude - radius;
    }

    private static float Torus(Vector3 worldPos, Vector3 origin, Vector2 t) {
        Vector3 p = worldPos - origin;
        Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - t.x, p.y);
        return q.magnitude - t.y;
    }

    private static float Cuboid(Vector3 worldPos, Vector3 origin, Vector3 halfDim) {
        Vector3 localPos = worldPos - origin;

        Vector3 d = new Vector3();
        d.x = Mathf.Abs(localPos.x) - halfDim.x;
        d.y = Mathf.Abs(localPos.y) - halfDim.y;
        d.z = Mathf.Abs(localPos.z) - halfDim.z;

        float m = Mathf.Max(d.x, Mathf.Max(d.y, d.z));

        Vector3 v = new Vector3();
        v.x = Mathf.Max(d.x, 0.0f);
        v.y = Mathf.Max(d.y, 0.0f);
        v.z = Mathf.Max(d.z, 0.0f);

        return Mathf.Min(m, Vector3.Magnitude(v));
    }

    private static float Union(float a, float b) {
        return Mathf.Min(a, b);
    }

    // a - b
    private static float Subtraction(float a, float b) {
        return Mathf.Max(a, -b);
    }

    private static float Intersection(float a, float b) {
        return Mathf.Max(a, b);
    }

    public static float Eval(Vector3 worldPos) {
        float d = 0.0f;

        // FRACTAL PLANET ----------------------------------------------------------------
        float rad = 800.0f;
        Vector3 wp = worldPos;
        wp.y -= rad;
        float planet = (wp - new Vector3(0, -rad, 0)).magnitude - rad;
        planet += Noise.Fractal3(wp, new Vector3(-100, 10, 25), 5, 0.009f, 0.45f, 2.07f) * 25.0f;

        float sphere = Sphere(worldPos, Vector3.zero, 785.0f);
        sphere += Noise.Fractal3(worldPos, Vector3.zero, 3, 0.05f);

        d = Union(planet, sphere);
        //--------------------------------------------------------------------------------

        // WORLEY NOISE TEST--------------------------------------------------------------
        // change fractal3 code to use worley instead
        // this is currently broke and may show vurnerability in mesh generation
        // because parents arent getting meshes sometimes with this
        //d += Noise.Fractal3(worldPos, new Vector3(-100, 10, 25), 3, 0.001f, 0.45f, 2.07f) * 25.0f;
        //--------------------------------------------------------------------------------

        //float warp = Noise.Simplex3(worldPos.x * 0.04f, worldPos.y * 0.04f, worldPos.z * 0.04f);
        //worldPos += Vector3.one * warp * 20;


        //d += (float)Noise.Simplex3(worldPos.x * 4.03, worldPos.y * 4.03, worldPos.z * 4.03) * 5f;
        //d += (float)Noise.Simplex3(worldPos.x * 1.96, worldPos.y * 1.96, worldPos.z * 1.96) * 0.5f;
        //d += (float)Noise.Simplex3(worldPos.x * 1.01, worldPos.y * 1.01, worldPos.z * 1.01) * 1.0f;

        //Quaternion q = Quaternion.AngleAxis(45.0f, Vector3.up);
        //worldPos = q * worldPos;

        //float sphere = Sphere(worldPos, new Vector3(200.0f, 200.0f, 160.0f), 200.0f);
        //float torus = Torus(worldPos, new Vector3(200.0f, 200.0f, -80.0f), new Vector2(200.0f, 80.0f));
        //float cube = Cuboid(worldPos, new Vector3(0.0f, 400.0f, 0.0f), Vector3.one * 240.0f);

        //d = Union(sphere, torus);
        //d = Subtraction(d, cube);

        // bounds of 64 size area
        //return Cuboid(worldPos, new Vector3(0.0f, 0.0f, 0.0f), Vector3.one * 32.0f);

        //float sqrMag = worldPos.sqrMagnitude;

        //float freq = 0.01f;
        //double d = SimplexNoise.noise(worldPos.x * freq, worldPos.y * freq, worldPos.z * freq);
        ////WorleySample w = Noise.Worley3(worldPos.x * freq, worldPos.y * freq, worldPos.z * freq, 2, DistanceFunction.EUCLIDIAN);
        ////double d = w.F[1] - w.F[0];

        // BASIC FLAT FRACTAL ------------------------------------------------------------
        //d = -worldPos.y + 20.0f;
        //d += Noise.Fractal3(worldPos, new Vector3(0, 25, 25), 9, 0.01f, 0.5f, 2.0f) * 20.0f;
        //--------------------------------------------------------------------------------

        //d = -worldPos.y + 4.0f;
        //d += Noise.Fractal3(worldPos, new Vector3(0, 0, 25), 9, 0.005f, 0.5f, 2.0f) * 20.0f;

        return d;
    }

}
