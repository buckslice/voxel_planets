using UnityEngine;

public struct Voxel {
    public sbyte density;
    public byte material;
    public Voxel(sbyte density, byte material) {
        this.density = density;
        this.material = material;
    }
}

public class Density {

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

    public static Voxel Eval(Vector3 worldPos, float voxelSize) {
        float d = 0.0f;

        // FRACTAL PLANET ----------------------------------------------------------------
        //float rad = 800.0f;
        //Vector3 wp = worldPos;
        //wp.y -= rad;
        //float planet = (wp - new Vector3(0, -rad, 0)).magnitude - rad;
        //planet += Noise.Fractal3(wp, new Vector3(-100, 10, 25), 5, 0.009f, 0.45f, 2.07f) * 25.0f;

        //float sphere = Sphere(worldPos, Vector3.zero, 785.0f);
        //sphere += Noise.Fractal3(worldPos, Vector3.zero, 3, 0.05f);

        //d = Union(planet, sphere);
        ////d = sphere;
        //--------------------------------------------------------------------------------

        // BIG PLANET ------------------------------------------------------
        //float rad = 14000.0f;
        //Vector3 wp = worldPos;
        //wp.y -= rad;
        //float planet = (wp - new Vector3(0, -rad, 0)).magnitude - rad;
        ////planet += Noise.Fractal3(wp, Vector3.one * 1000.0f, 5, 0.0005f, 0.5f, 2f) * 30.0f;
        //planet += Noise.Fractal3(wp, Vector3.one * 1000.0f, 7, 0.001f, 0.5f, 2f) * 100.0f;
        //d = planet;

        ////float sphere = Sphere(worldPos, Vector3.zero, 785.0f);
        ////sphere += Noise.Fractal3(worldPos, Vector3.zero, 3, 0.05f);
        ////d = Union(planet, sphere);
        ////d = sphere;
        //------------------------------------------------------------------

        //warping test----------------------------
        float rad = 1000.0f;
        //Vector3 wp = worldPos;
        //wp.y -= rad;
        //float qx = Noise.Fractal3(wp, new Vector3(0.0f, 0.0f, 0.0f), 3, 0.01f);
        //float qy = Noise.Fractal3(wp, new Vector3(5.2f, 1.3f, -2.0f), 3, 0.01f);
        //float qz = Noise.Fractal3(wp, new Vector3(1.5f, 2.7f, 3.7f), 3, 0.01f);
        //wp = wp + 20.0f * new Vector3(qx, qy, qz);

        //float planet = (wp - new Vector3(0, -rad, 0)).magnitude - rad;
        //planet += Noise.Fractal3(wp, Vector3.one * 1000.0f, 8, 0.001f, 0.5f, 2f) * 100.0f;
        //d = planet;

        //worldPos.y -= rad;
        //d = Sphere(worldPos, Vector3.zero, rad);
        d = Sphere(worldPos, new Vector3(0, 0, 0), rad);

        float offset = Noise.Fractal3(worldPos, Vector3.one * 1000.0f, 8, 0.001f, 0.5f, 2f) * 100.0f;
        d += offset;

        //d = Sphere(worldPos, Vector3.zero, 14000.0f);

        //-----------------------------------------

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

        //simple rotated cube test -------------------------------------------------------
        //worldPos = Quaternion.AngleAxis(45.0f, Vector3.right) * worldPos;
        //worldPos = Quaternion.AngleAxis(45.0f, Vector3.up) * worldPos;
        //d = Cuboid(worldPos, new Vector3(20, 0, 10), Vector3.one * 5);
        //--------------------------------------------------------------------------------

        //d = worldPos.y - 10.0f;

        //float sphere = Sphere(worldPos, new Vector3(20.0f, 20.0f, 20.0f), 8.0f);
        //float torus = Torus(worldPos, new Vector3(20.0f, 20.0f, 30.0f), new Vector2(12.0f, 4.0f));
        //d = Union(sphere,torus);

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
        //d = worldPos.y;
        //d += Noise.Fractal3(worldPos, new Vector3(-100, 600, -500), 9, 0.01f, 0.5f, 2.0f) * 20.0f;
        //--------------------------------------------------------------------------------

        Voxel v;
        // this seems wierd still i dunno
        v.density = (sbyte)(Mathf.Clamp(Mathf.Round(d * 128.0f / voxelSize), -128.0f, 127.0f));

        float col = Noise.Fractal3(worldPos, Vector3.one * 100.0f, 4, 0.005f);
        //float col = offset;
        // need to figure out way to understand how to think in terms of density lol...
        byte mat = 0;
        if(col < -.7f) {
            mat = 0;
        }else if (col < 0.3f) {
            mat = 1;
        }else {
            mat = 2;
        }

        v.material = mat;

        return v;
    }

}
