using UnityEngine;
using UnityEditor;

public class VoxelBody : MonoBehaviour {


    public Material mat;
    public float[] squareSplitLevels;
    public Transform cam;
    public float radius = 500f;

    private Octree root = null;



    // Use this for initialization
    void Start() {
        root = new Octree(this, Vector3.zero, 0, 0);
        root.BuildGameObject(root.Generate());

        squareSplitLevels = new float[Octree.MAX_DEPTH + 1];
        for (int i = 0; i < squareSplitLevels.Length; i++) {
            squareSplitLevels[i] = Mathf.Pow(2f, Octree.MAX_DEPTH - i) * 50f;
            squareSplitLevels[i] *= squareSplitLevels[i];
        }
        cam = Camera.main.transform;
    }

    // Update is called once per frame
    void Update() {
        root.Update();
    }

}
