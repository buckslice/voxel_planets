using UnityEngine;
using System.Text;
using System.Threading;

public class Octree {

    // front right - front left - back left - back right, then same order but for bottom layer
    public Octree[] children;
    // front left back right up down
    //public Octree[] neighbors = new Octree[6];

    public bool hasChildren = false;
    public int depth;  // 0 is root, MAX_LEVEL is depth limit
    private int branch;  // which child of your parent are you

    private float[][][] voxels;

    private Vector3 pos;    // position of voxel grid (denotes the corner so it gets offset to remain centered)
                            //private CelestialBody cb;
    public readonly Vector3 center; // world space center of octree area
    public VoxelBody vox;

    public ChunkObject obj;

    public const int SIZE = 16;        // size of visible voxel grid
    //public const int TSIZE = SIZE + 5;   // total size with voxel border (done for normal smoothing)
    public const int TSIZE = SIZE + 1;
    // so while size is 16, which means theres 16 blocks 
    // you need 17 verts to store those blocks (think of 17 points in a grid and the blocks are the 16 white space in between)
    // then add a buffer of 2 around (front and back so +4) for smoothing and normal calculation
    // so mesh goes from 2-19 basically (0 1, 20, 21) are not visible in final result

    public const int MAX_DEPTH = 6;     // max depth meshes can split to
    public float voxelSize;   // size of each voxel for this tree (in meters)

    public Bounds area; // bounding box for area this tree represents (gonna be bad.. once planets can rotate lol)
    //private Bounds meshBounds;

    public Mesh mesh;

    private bool splitting = false; // set when a tree is trying to split
    private bool dying = false; // gets set when a child is merged into parent

    public Octree(VoxelBody vox, Vector3 center, int depth, int branch) {
        this.vox = vox;
        this.depth = depth;
        this.branch = branch;
        voxelSize = Mathf.Pow(2, (MAX_DEPTH - depth));

        this.center = center;
        //pos = center - new Vector3(2, 2, 2) * voxelSize - Vector3.one * (SIZE / 2f) * voxelSize;
        pos = center - Vector3.one * (SIZE / 2f) * voxelSize;

        area = new Bounds(center, Vector3.one * voxelSize * SIZE);
    }

    public MeshData Generate() {
        voxels = WorldGenerator.CreateVoxels(TSIZE, depth, voxelSize, pos);
        //voxels = VoxelUtils.SmoothVoxels(voxels);
        //MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize, 2, 2);
        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize);

        data.CalculateVertexSharing();

        //Simplification simp = new Simplification(data.vertices, data.triangles);

        data.CalculateNormals();
        //data.SplitEdgesCalcSmoothness();
        //data.normals = VoxelUtils.CalculateSmoothNormals(voxels, voxelSize, data.vertices);
        //data.CalculateColorsByDepth(depth);

        return data;
    }

    // all the stuff that requires the main thread for Unity functions is done here
    // most other work is offloaded to helper threads. builds mesh and game object
    // dont actually need to build a gameobject if theres no mesh...
    // or at least have an empty object for the parent transform and dont attach components
    public void BuildGameObject(MeshData data) {

        mesh = data.CreateMesh();
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


        obj.go.transform.parent = vox.transform;
        obj.go.transform.localPosition = pos;

        if(mesh == null) {
            return;
        }

        obj.mr.material = vox.mat;
        obj.mf.mesh = mesh;

        if (depth == 0) {
            obj.ov.init(depth, branch, center, area, Color.blue);
        } else {
            obj.ov.init(depth, branch, center, area, Color.red);
            obj.ov.shouldDraw = true;
        }

        // wait to try out colliders for now
        //MeshCollider collider = m_mesh.AddComponent<MeshCollider>();
        //collider.sharedMesh = mesh;

    }

    public void Update() {
        if (hasChildren) {
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
        }
    }

    //public Octree GetBlockingNeighbors() {
    //    for (int i = 0; i < 6; i++) {
    //        Octree tree = neighbors[i];
    //        if (tree != null && tree.depth < depth && !tree.splitting) {
    //            return tree;
    //        }
    //    }
    //    return null;
    //}

    public SplitData Split() {
        //Debug.Assert(depth <= MAX_DEPTH);

        //Octree n = GetBlockingNeighbors();
        //while (n != null) {
        //    n.Split();
        //    n = GetBlockingNeighbors();
        //}

        SplitData data = new SplitData(this);
        children = new Octree[8];
        for (int i = 0; i < 8; i++) {
            Vector3 coff = childOffsets[i];
            Octree child = new Octree(vox, center + coff * SIZE * voxelSize * .25f, depth + 1, i);
            //coff = ((coff + Vector3.one) / 2f) * (SIZE / 2f);
            //o.passVoxels(voxels, (int)coff.x, (int)coff.y, (int)coff.z);
            data.Add(child.Generate());
            children[i] = child;
        }

        return data;
    }

    public void SplitResolve(MeshData[] data) {
        splitting = false;
        if (dying || !ShouldSplit()) {
            return;
        }

        for (int i = 0; i < 8; ++i) {
            children[i].BuildGameObject(data[i]);
            children[i].obj.go.transform.parent = obj.go.transform;
        }

        //SetChildNeighbors();
        UpdateNeighbors(true);
        obj.mr.enabled = false;
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
        return (vox.cam.position - center).sqrMagnitude;
    }

    private bool ShouldSplit() {
        return depth < MAX_DEPTH && GetSqrDistToCam() < vox.squareSplitLevels[depth];
    }

    private bool ShouldMerge() {
        return CanMerge() && GetSqrDistToCam() > vox.squareSplitLevels[depth];
    }

    private bool CanMerge() {
        for (int i = 0; i < 8; i++) {
            if (children[i].hasChildren) {
                return false;
            }
        }
        // or if childrens neighbor has children?
        return true;
    }

    private void Merge() {
        UpdateNeighbors(false);
        for (int i = 0; i < 8; i++) {
            UnityEngine.Object.Destroy(children[i].mesh);
            SplitManager.ReturnObject(children[i].obj);
            children[i].dying = true;
            children[i] = null;
        }
        hasChildren = false;
        obj.mr.enabled = true;
        if (mesh.vertexCount > 0) {
            obj.ov.shouldDraw = true;
        }
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
