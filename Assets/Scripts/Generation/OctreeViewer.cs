using UnityEngine;
using System.Collections;

public class OctreeViewer : MonoBehaviour {

    public int depth;
    public int branch;
    public Bounds bounds;
    public Vector3 center;
    private Color c;
    //private float r
    public bool shouldDraw = false;

    void OnDrawGizmosSelected() {
        if (shouldDraw) {
            Gizmos.color = c;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            //Gizmos.DrawSphere(center, r);
        }
    }

    public void init(int depth, int branch, Bounds bounds, Color c) {
        shouldDraw = true;
        this.depth = depth;
        this.branch = branch;
        this.bounds = bounds;
        this.c = c;

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
