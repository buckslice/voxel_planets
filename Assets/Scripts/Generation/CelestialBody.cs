using UnityEngine;
using UnityEngine.UI;

public class CelestialBody : MonoBehaviour {

    public Material terrainMat;
    
    public float[] splitLevels;
    Transform playerTransform;
    public Vector3 player;
    //public float surfaceRadius = 500.0f;    // need to actually set these based off generation
    public float atmosphereRadius = 1000.0f;
    public float gravityRadius = 1500.0f;
    public Material testMat;
    public Octree root = null;

    const float maxDepthDist = 100.0f;  // aka nodes should be maximally split within 100 units of player

    Vector3 rotation;

    public Matrix4x4 currentMatrix = new Matrix4x4();
    public Text countText;

#if true
    // Use this for initialization
    void Start() {
        // will later need to set this in chunk material property blocks i think
        //mat.SetVector("_PlanetCenter", transform.position);

        root = new Octree(this, null, Vector3.zero, 0, 0);
        //root.BuildGameObject(root.GenerateMesh(true));

        // not sure if this is great way to do it
        root.BuildGameObjectCompute();
        MarchingCubesDispatcher.Enqueue(root, root.AssignMesh, true);
        //root.SplitCompute();

        // calculating split levels by hand now
        // too hard to get right witha formula to look good both on surface and
        // from high up in space (may need to change strat, like dynamic split more chunks based on total num tris)
        Debug.Assert(splitLevels.Length >= Octree.MAX_DEPTH + 1);
        //splitLevels = new float[Octree.MAX_DEPTH + 1];
        //int len = splitLevels.Length;
        //splitLevels[len - 1] = maxDepthDist;
        //for (int i = len - 2; i >= 0; --i) {
        //    splitLevels[i] = splitLevels[i + 1] * 1.74f;
        //    //splitLevels[i] = splitLevels[i + 1] * 2.0f;
        //}

        // old way of calculating squaresplit levels
        //for (int i = 0; i < squareSplitLevels.Length; i++) {
        //    float level = Mathf.Pow(2f, Octree.MAX_DEPTH - i) * 100f; //64.0f;
        //    squareSplitLevels[i] = level * level;
        //}
        rotation = Random.onUnitSphere; // * speed?

        playerTransform = GameObject.Find("Player").transform;
    }

    float invalidCheckTimer = 0.0f;
    void Update() {
        if (Input.GetKey(KeyCode.R)) {
            transform.Rotate(rotation * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.P)) {
            MarchingCubesDispatcher.ClearRequests();
            root.ResetMeshes();
        }
        currentMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // cache player position this frame because like a billion things reference
        // this and the cost of transform.getposition adds up
        player = playerTransform.position;

        root.Update();
        invalidCheckTimer -= Time.deltaTime;
        if (invalidCheckTimer < 0.0f) {
            if (!root.IsTreeValid()) {
                Debug.LogError("INVALID TREE DETECTED");
            }
            invalidCheckTimer = 2.0f;
            int num = root.GetNumGameObjects(false);
            countText.text = "Trees: " + num;
            //Debug.Log(root.GetNumGameObjects(true));
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
    // make sure to switch to proper world gen procedure that is visible from 0-32m world units
#if false

    ChunkObject go;
    ChunkObject go2;
    VoxelMining miner;

    void Start() {
        miner = GameObject.Find("Player").GetComponent<VoxelMining>();

        Array3<Voxel> voxels = WorldGenerator.CreateVoxels(33, 0, 1.0f, Vector3.zero);
        //Array3<Voxel> voxels = WorldGenerator.CreateVoxels(64, 0, 1.0f, Vector3.zero);

        MeshData data = MarchingCubes.CalculateMeshData(voxels, 1.0f);
        Mesh mesh = data.CreateMesh();
        go = SplitManager.GetObject();
        go.mr.enabled = false;
        go.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        go.mf.sharedMesh = mesh;
        go.mr.material = testMat;

        MeshData data2 = MarchingTetrahedra.CalculateMeshData(voxels, 1.0f);
        data2.CalculateSharedNormals();
        Mesh mesh2 = data2.CreateMesh();
        go2 = SplitManager.GetObject();
        go2.mf.sharedMesh = mesh2;
        go2.mr.material = testMat;
        go2.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        miner.forceDrawChunk = go2;

    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            go.mr.enabled = !go.mr.enabled;
            go2.mr.enabled = !go2.mr.enabled;
            miner.forceDrawChunk = go.mr.enabled ? go : go2;
        }

    }
#endif

}
