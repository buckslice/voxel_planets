
//#define SMOOTH_SHADING

using UnityEngine;
using System.Threading.Tasks;


public class Octree {

    // front right - front left - back left - back right, then same order but for bottom layer
    public Octree[] children;
    // front left back right up down
    //public Octree[] neighbors = new Octree[6];

    public bool hasChildren = false;
    public int depth;  // 0 is root, MAX_LEVEL is depth limit
    private int branch;  // which child of your parent are you

    private Array3<sbyte> voxels; // need to save for vertex modification

    private Vector3 pos;    // position of voxel grid (denotes the corner so it gets offset to remain centered)
    public readonly Vector3 center; // world space center of octree area

    public CelestialBody body;

    public ChunkObject obj = null;
    public ColliderObject col = null;

    public const int SIZE = 16;        // number of voxel cells in octree
    public const int MAX_DEPTH = 6;     // max depth meshes can split to
    public readonly float voxelSize;   // size of each voxel for this tree (in meters)

    public Bounds area; // bounding box for area this tree represents (gonna be bad.. once planets can rotate lol)
    //private Bounds meshBounds;

    public bool splitting = false; // set when a tree is waiting on list/currently being split
    private bool dying = false;    // gets set when a child is merged into parent

    public const float fadeRate = 1.0f; // 0.5f would be half of normal time, so 2 seconds
    private float morphProg = 0.0f;   // "percent morphed" 0 at morph start, 1 when morph is finished

    public Octree(CelestialBody body, Vector3 center, int depth, int branch) {
        this.body = body;
        this.center = center;
        this.depth = depth;
        this.branch = branch;
        voxelSize = Mathf.Pow(2, (MAX_DEPTH - depth)) * 2.0f;
        // voxel are 2m^3 at max depth
        // then 4, 8, 16, etc

#if (SMOOTH_SHADING)
        pos = center - new Vector3(2, 2, 2) * voxelSize - Vector3.one * (SIZE / 2f) * voxelSize;
#else
        pos = center - Vector3.one * (SIZE / 2f) * voxelSize;
#endif

        area = new Bounds(center, Vector3.one * voxelSize * SIZE);
    }

    public MeshData GenerateMesh() {
        //voxels = VoxelUtils.SmoothVoxels(voxels);

        // so while SIZE is 16, which means theres 16 cells/blocks in grid 
        // you need 17 values to be able to construct those blocks
        // (think of 17 points in a grid and the blocks are the 16 spaces in between)
        // if smoothing then need a buffer of 2 around (front and back so +4) for smoothing and normal calculation
        // (so mesh goes from 2-19 basically (0, 1, 20, 21) are not visible in final result)
#if (SMOOTH_SHADING)
        voxels = WorldGenerator.CreateVoxels(SIZE + 5, depth, voxelSize, pos);
        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize, 2, 2);
        //data.CalculateVertexSharing();
        //Simplification simp = new Simplification(data.vertices, data.triangles);
        data.normals = VoxelUtils.CalculateSmoothNormals(voxels, voxelSize, data.vertices);
        //data.SplitEdgesCalcSmoothness();
#else
        voxels = WorldGenerator.CreateVoxels(SIZE + 1, depth, voxelSize, pos);
        //if (!needsMesh) {
        //    return null;
        //}

        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize);
        data.CalculateNormals();
#endif

        //data.CalculateColorsByDepth(depth);
        return data;
    }

    // all the stuff that requires the main thread for Unity functions is done here
    // most other work is offloaded to helper threads. builds mesh and game object
    // dont actually need to build a gameobject if theres no mesh...
    // or at least have an empty object for the parent transform and dont attach components
    public void BuildGameObject(MeshData data) {
        Mesh mesh = null;

        if (data != null) {
            mesh = data.CreateMesh();
        } else {
            //Debug.Log(++meshlessboys);
        }
        obj = SplitManager.GetObject();

        //StringBuilder builder = new StringBuilder();
        //builder.Append("Tree ");
        //builder.Append(depth);
        //builder.Append(" ");
        //builder.Append(center.x);
        //builder.Append(" ");
        //builder.Append(center.y);
        //builder.Append(" ");
        //builder.Append(center.z);
        //obj.go.name = builder.ToString();


        obj.go.transform.parent = body.transform;
        obj.go.transform.localPosition = pos;

        if (mesh == null) {
            obj.ov.shouldDraw = false;
            return;
        }
        obj.ov.shouldDraw = true;

        obj.mr.material = body.mat;
        obj.mf.mesh = mesh;

        if (depth == 0) {
            //Debug.Log("Called");
            obj.ov.init(depth, branch, center, area, Color.blue);
        } else {
            obj.ov.init(depth, branch, center, area, Color.red);
        }

    }

    public void Update() {
        if (hasChildren) {
            GeoMorphInternal();
            if (ShouldMerge()) {
                Merge();
            } else {
                for (int i = 0; i < 8; i++) {
                    children[i].Update();
                }
            }
        } else if (!splitting && ShouldSplit()) {
            splitting = true;
            SplitManager.AddToSplitList(this);
        } else {
            GeoMorphLeaf();
            // if at max depth, have valid mesh, and close to cam then should have a collider
            if (depth == MAX_DEPTH && obj.mf.sharedMesh && GetSqrDistToCam() < 100.0f * 100.0f) {
                if (col == null) {     // if collider is null then spawn one
                    col = SplitManager.GetCollider();
                    col.go.transform.SetParent(obj.go.transform, false);
                    col.go.transform.localPosition = Vector3.zero;
                    col.mc.sharedMesh = obj.mf.sharedMesh;
                }
            } else if (col != null) {   // otherwise if have collider then return it
                SplitManager.ReturnCollider(col);
                col = null;
            }

        }
    }

    // 0 -> 0.5 fade children in
    // 0.5 -> 1.0 fade parent out
    private void GeoMorphInternal() {
        if (morphProg < 1.0f) {
            morphProg += Time.deltaTime * fadeRate;
            if (morphProg >= 1.0f) {
                obj.mr.enabled = false;
            } else if (morphProg >= 0.5f) {
                obj.SetTransparency((1.0f - morphProg) * 2.0f);
                // todo set cast shadow strength here as well!
            }
        }
    }
    private void GeoMorphLeaf() {
        if (morphProg < 0.5f) {
            morphProg += Time.deltaTime * fadeRate;
            if (morphProg >= 0.5f) {
                obj.SetTransparency(1.0f);
                morphProg = 1.01f;
            } else {
                obj.SetTransparency(morphProg * 2.0f);
            }
        }
    }

    //public Octree GetBlockingNeighbors() {
    //    for (int i = 0; i < 6; i++) {
    //        Octree tree = neighbors[i];
    //        if (tree != null && tree.depth < depth && !tree.onSplitList) {
    //            return tree;
    //        }
    //    }
    //    return null;
    //}

    public Task<SplitData> SplitAsync() {
        return Task<SplitData>.Factory.StartNew(() => {
            SplitData data = new SplitData(this);
            children = new Octree[8];
            for (int i = 0; i < 8; i++) {
                Vector3 coff = childOffsets[i];
                Octree child = new Octree(body, center + coff * SIZE * voxelSize * .25f, depth + 1, i);
                //coff = ((coff + Vector3.one) / 2f) * (SIZE / 2f);
                //o.passVoxels(voxels, (int)coff.x, (int)coff.y, (int)coff.z);
                data.Add(child.GenerateMesh());
                children[i] = child;
            }
            return data;
        }, TaskCreationOptions.None);
    }

    public void SplitResolve(MeshData[] data) {
        splitting = false;
        if (!ShouldSplit()) {
            return;
        }

        float curTime = Time.time;
        for (int i = 0; i < 8; ++i) {
            Octree c = children[i];
            c.BuildGameObject(data[i]);
            c.obj.go.transform.parent = obj.go.transform;
            c.obj.SetTransparency(0.0f);
        }
        obj.SetTransparency(1.0f);
        morphProg = 0.0f;
        obj.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        //SetChildNeighbors();
        UpdateNeighbors(true);
        if (depth > 0) {
            obj.ov.shouldDraw = false;
        }
        hasChildren = true;
    }

    //public void SetChildNeighbors() {
    //    // need more updating for if higher rec neighbors are looking at you when you split

    //    //children[0].neighbors[0] = neighbors[0];
    //    //children[1].neighbors[0] = neighbors[0];

    //    // 0 1 4 5 Front 0
    //    // 1 2 5 6 Left  4
    //    // 2 3 6 7 Back  3
    //    // 3 0 7 4 Right 1
    //    // 0 1 2 3 Up    2
    //    // 4 5 6 7 Down  5

    //    // 0 4
    //    // 1 5
    //    // 2 6
    //    // 3 7
    //    for (int i = 0; i < 6; i++) {
    //        for (int j = 0; j < 4; j++) {
    //            children[neb[i][j]].neighbors[i] = neighbors[i];
    //        }
    //    }
    //}

    private static int[][] neb = new int[][] {
        new int[] {0,1,4,5},
        new int[] {1,2,5,6},
        new int[] {2,3,6,7},
        new int[] {3,0,7,4},
        new int[] {0,1,2,3},
        new int[] {4,5,6,7}
    };

    private void UpdateNeighbors(bool splitting) {

    }

    public float GetSqrDistToCam() {
        return (body.cam.position - center).sqrMagnitude;
    }

    // can only split if
    //    depth is less than max depth
    //    not dying
    //    fully opaque
    public bool ShouldSplit() {
        return depth < MAX_DEPTH && !dying && morphProg >= 1.0f && GetSqrDistToCam() < body.squareSplitLevels[depth];
    }

    private bool ShouldMerge() {
        return CanMerge() && GetSqrDistToCam() > body.squareSplitLevels[depth];
    }

    private bool CanMerge() {
        return !children[0].hasChildren
            && !children[1].hasChildren
            && !children[2].hasChildren
            && !children[3].hasChildren
            && !children[4].hasChildren
            && !children[5].hasChildren
            && !children[6].hasChildren
            && !children[7].hasChildren;
    }

    // called when parent merges 8 children
    private void Merge() {
        UpdateNeighbors(false);
        for (int i = 0; i < 8; i++) {
            children[i].OnGettingMerged();
            children[i] = null;
        }
        hasChildren = false;
        obj.mr.enabled = true;
        obj.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        morphProg = 1.0f;
        obj.SetTransparency(morphProg);
        if (obj.mf.mesh.vertexCount > 0) {
            obj.ov.shouldDraw = true;
        }
    }

    // called on children getting merged by their parent
    private void OnGettingMerged() {
        Object.Destroy(obj.mf.mesh);
        SplitManager.ReturnObject(obj);
        if (col != null) {
            SplitManager.ReturnCollider(col);
            col = null;
        }
        dying = true;
    }

    public void PassVoxels(float[][][] v, int xo, int yo, int zo) {
        // PROB DOESNT WORK
        // because smoothing changes the voxels
        // wait F smoothing!!!

        //for (int x = 2; x < SIZE / 2; x++) {
        //    for (int y = 0; y < SIZE / 2; y++) {
        //        for (int z = 0; z < SIZE / 2; z++) {
        //            voxels[x * 2][y * 2][z * 2] = v[x + xo][y + yo][z + zo];
        //        }
        //    }
        //}
    }

    public static Vector3[] childOffsets = {
        new Vector3(1, 1, 1),
        new Vector3(-1, 1, 1),
        new Vector3(-1, 1, -1),
        new Vector3(1, 1, -1),
        new Vector3(1, -1, 1),
        new Vector3(-1, -1, 1),
        new Vector3(-1, -1, -1),
        new Vector3(1, -1, -1)
    };

    public Bounds GetBounds() {
        return new Bounds(center, Vector3.one * voxelSize * SIZE);
    }

}
