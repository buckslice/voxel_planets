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
        Quaternion q = Quaternion.AngleAxis(45.0f, Vector3.up);
        worldPos = q * worldPos;

        float sphere = Sphere(worldPos, new Vector3(50.0f, 50.0f, 40.0f), 50.0f);
        float torus = Torus(worldPos, new Vector3(50.0f, 50.0f, -20.0f), new Vector2(50.0f, 20.0f));
        float cube = Cuboid(worldPos, new Vector3(0.0f, 100.0f, 0.0f), Vector3.one * 60.0f);
        //float f = Mathf.Max(-cube, sphere);
        //float f = Mathf.Max(-torus, cube);
        //f = Mathf.Max(-sphere, f);

        //float f = Mathf.Min(cube, torus);
        float f = 0.0f;
        f = Union(sphere, torus);
        f = Subtraction(f, cube);

        // bounds of 64 size area
        //return Cuboid(worldPos, new Vector3(0.0f, 0.0f, 0.0f), Vector3.one * 32.0f);

        return f;
    }

}
