using UnityEngine;
using UnityEditor;

public class CelestialBody : MonoBehaviour {

    public Material mat;
    public Material testMat;
    public float[] splitLevels;
    public Transform cam;
    public Transform player;
    //public float surfaceRadius = 500.0f;    // need to actually set these based off generation
    public float atmosphereRadius = 1000.0f;
    public float gravityRadius = 1500.0f;

    public Octree root = null;

    const float maxDepthDist = 50.0f;  // aka nodes should be maximally split within 100 units of player

#if true
    // Use this for initialization
    void Start() {
        root = new Octree(this, null, Vector3.zero, 0, 0);
        root.BuildGameObject(root.GenerateMesh(true));

        splitLevels = new float[Octree.MAX_DEPTH + 1];
        int len = splitLevels.Length;
        splitLevels[len - 1] = maxDepthDist;
        for (int i = len - 2; i >= 0; --i) {
            //splitLevels[i] = splitLevels[i + 1] * 1.74f;
            splitLevels[i] = splitLevels[i + 1] * 2.0f;
        }

        // old way of calculating squaresplit levels
        //for (int i = 0; i < squareSplitLevels.Length; i++) {
        //    float level = Mathf.Pow(2f, Octree.MAX_DEPTH - i) * 100f; //64.0f;
        //    squareSplitLevels[i] = level * level;
        //}

        cam = Camera.main.transform;
        player = GameObject.Find("Player").transform;
    }

    float invalidCheckTimer = 0.0f;
    // Update is called once per frame
    void Update() { 
        root.Update();

        invalidCheckTimer -= Time.deltaTime;
        if (invalidCheckTimer < 0.0f) {
            if (!root.IsTreeValid()) {
                Debug.LogError("TREE INVALID DETECTED");
            }
            invalidCheckTimer = 2.0f;
        }
    }
#endif

// example testing out the simplification post processing step of meshes
#if false
    float[][][] voxels;
    Simplification simp;
    ChunkObject go;
    MeshData data;
    Mesh mesh;
    void Start() {
        voxels = WorldGenerator.CreateVoxels(65, 0, 1, Vector3.zero);

        data = MarchingCubes.CalculateMeshData(voxels, 1);
        data.CalculateVertexSharing();
        data.CalculateNormals();

        simp = new Simplification(data.vertices, data.triangles);

        mesh = data.CreateMesh();

        go = SplitManager.GetObject();
        go.mr.material = mat;
        go.mf.sharedMesh = mesh;

    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            for (int i = 0; i < 1000; ++i) {
                var edgeCost = simp.costs.RemoveFront();
                simp.CollapseEdge(edgeCost);
            }

            simp.ToMesh(out data.vertices, out data.triangles);
            if (go.mf.sharedMesh != null) {
                Destroy(go.mf.sharedMesh);
            }
            data.CalculateNormals();

            if (Input.GetKey(KeyCode.LeftShift)) {
                data.SplitEdgesCalcSmoothness();
            }

            mesh = data.CreateMesh();
            go.mf.sharedMesh = mesh;
        }

    }
#endif


// simple example here
// currently testing marching tetrahedra implementation
#if false

    void Start() {


        Array3<sbyte> voxels = WorldGenerator.CreateVoxels(33, 0, 1.0f, Vector3.zero);

        MeshData data = MarchingCubes.CalculateMeshData(voxels, 1.0f);

        Mesh mesh = data.CreateMesh();

        ChunkObject go = SplitManager.GetObject();
        go.mf.sharedMesh = mesh;
        go.mr.material = testMat;
        //go.ov.bounds = new Bounds()
        //go.ov.shouldDraw = true;

        MeshData data2 = MarchingTetrahedra.CalculateMeshData(voxels, 1.0f);
        data2.CalculateSharedNormals();
        Mesh mesh2 = data2.CreateMesh();

        ChunkObject go2 = SplitManager.GetObject();
        go2.mf.sharedMesh = mesh2;
        go2.mr.material = testMat;

    }
#endif

}
