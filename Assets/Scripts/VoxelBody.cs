using UnityEngine;
using System.Collections.Generic;

public class VoxelBody : MonoBehaviour {

    private Octree root = null;

    public Material mat;

    public int testDepth = 0;

    public float[] squareSplitLevels;
    public Transform cam;

    public float radius = 500f;

    // Use this for initialization
    void Start() {
        root = new Octree(this, Vector3.zero, testDepth, 0);
        root.generate();

        squareSplitLevels = new float[Octree.MAX_DEPTH + 1];
        for (int i = 0; i < squareSplitLevels.Length; i++) {
            squareSplitLevels[i] = Mathf.Pow(2f, Octree.MAX_DEPTH - i) * 50f;
            squareSplitLevels[i] *= squareSplitLevels[i];
        }
        cam = Camera.main.transform;
    }

    // Update is called once per frame
    void Update() {
        //if (Input.GetKeyDown(KeyCode.E)) {
        //    Octree t = root;
        //    while (t.hasChildren) {
        //        t = t.children[0];
        //    }
        //    t.split();
        //}

        root.update();
    }

}
