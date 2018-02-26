using UnityEngine;
using System.Collections;

public class OctreeViewer : MonoBehaviour {

    public int depth;
    public int branch;
    public Vector3 offset;
    public bool shouldDraw = false;
    public bool splitting = false;
    public Color color;
    Color origColor;

    Bounds bounds;
    CelestialBody body;

    Vector3[] v = new Vector3[8];
    void OnDrawGizmosSelected() {
        if (shouldDraw) {
            Gizmos.color = color;

            //Matrix4x4 oldMat = Gizmos.matrix;
            // maybe calc this once in planet each frame
            if (body != null && body.currentMatrix != null) {
                Gizmos.matrix = body.currentMatrix;
            }

            Gizmos.DrawWireCube(bounds.center, bounds.size);

            //Vector3 c = bounds.center;
            //Vector3 e = bounds.extents;

            //v[0] = Octree.LocalToWorld(planet, new Vector3(c.x - e.x, c.y - e.y, c.z - e.z));
            //v[1] = Octree.LocalToWorld(planet, new Vector3(c.x + e.x, c.y - e.y, c.z - e.z));
            //v[2] = Octree.LocalToWorld(planet, new Vector3(c.x - e.x, c.y + e.y, c.z - e.z));
            //v[3] = Octree.LocalToWorld(planet, new Vector3(c.x + e.x, c.y + e.y, c.z - e.z));
            //v[4] = Octree.LocalToWorld(planet, new Vector3(c.x - e.x, c.y - e.y, c.z + e.z));
            //v[5] = Octree.LocalToWorld(planet, new Vector3(c.x + e.x, c.y - e.y, c.z + e.z));
            //v[6] = Octree.LocalToWorld(planet, new Vector3(c.x - e.x, c.y + e.y, c.z + e.z));
            //v[7] = Octree.LocalToWorld(planet, new Vector3(c.x + e.x, c.y + e.y, c.z + e.z));
            //for (int i = 0; i < 4; ++i) {
            //    // forward lines
            //    Gizmos.DrawLine(v[i], v[i + 4]);

            //    // right lines
            //    Gizmos.DrawLine(v[i * 2], v[i * 2 + 1]);

            //    // up lines
            //    int b = i < 2 ? 0 : 2;
            //    Gizmos.DrawLine(v[i + b], v[i + b + 2]);
            //}

            color = origColor;

        }
    }

    public void init(int depth, int branch, Vector3 offset, Bounds bounds, CelestialBody body, Color color) {
        this.depth = depth;
        this.branch = branch;
        this.offset = offset;
        this.bounds = bounds;
        this.body = body;
        this.color = color;
        origColor = color;

        //r = Mathf.Pow(2f, Octree.MAX_DEPTH - depth);
        //switch (branch) {
        //    case 0:
        //        c = Color.red;
        //        break;
        //    case 1:
        //        c = Color.yellow;
        //        break;
        //    case 2:
        //        c = Color.green;
        //        break;
        //    case 3:
        //        c = Color.cyan;
        //        break;
        //    case 4:
        //        c = Color.blue;
        //        break;
        //    case 5:
        //        c = Color.magenta;
        //        break;
        //    case 6:
        //        c = Color.white;
        //        break;
        //    case 7:
        //        c = Color.black;
        //        break;
        //    default:
        //        c = Color.gray;
        //        break;
        //}
    }
}
