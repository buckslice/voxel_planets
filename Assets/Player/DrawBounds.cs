using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawBounds : MonoBehaviour {

    public Material lineMat;

    Camera cam;
    // Use this for initialization
    void Start() {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update() {

    }

    Bounds bounds;

    public void SetBounds(Bounds b) {
        bounds = b;
    }

    void OnPostRender() {
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        Vector3[] v = new Vector3[8];
        v[0] = new Vector3(c.x - e.x, c.y - e.y, c.z - e.z);
        v[1] = new Vector3(c.x + e.x, c.y - e.y, c.z - e.z);
        v[2] = new Vector3(c.x - e.x, c.y + e.y, c.z - e.z);
        v[3] = new Vector3(c.x + e.x, c.y + e.y, c.z - e.z);
        v[4] = new Vector3(c.x - e.x, c.y - e.y, c.z + e.z);
        v[5] = new Vector3(c.x + e.x, c.y - e.y, c.z + e.z);
        v[6] = new Vector3(c.x - e.x, c.y + e.y, c.z + e.z);
        v[7] = new Vector3(c.x + e.x, c.y + e.y, c.z + e.z);

        GL.PushMatrix();

        lineMat.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(Color.red);

        for (int i = 0; i < 4; ++i) {
            // forward lines
            GL.Vertex(v[i]);
            GL.Vertex(v[i + 4]);

            // right lines
            GL.Vertex(v[i * 2]);
            GL.Vertex(v[i * 2 + 1]);

            // up lines
            int b = i < 2 ? 0 : 2;
            GL.Vertex(v[i + b]);
            GL.Vertex(v[i + b + 2]);
        }

        GL.End();

        GL.PopMatrix();
    }
}
