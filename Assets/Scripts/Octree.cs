using UnityEngine;
using System.Collections;

public class Octree {

    // front right - front left - back left - back right, then same order but for bottom layer
    public Octree[] children = new Octree[8];

    // front left back right up down
    public Octree[] neighbors = new Octree[6];
    public bool hasChildren = false;

    public int depth;  // 0 is root, MAX_LEVEL is depth limit
    private int branch;  // which child of your parent are you

    private float[][][] voxels;
    private Vector3[][][] normals;
    private Vector3 pos;    // position of voxel grid (denotes the corner so it gets offset to remain centered)
                            //private CelestialBody cb;
    private Vector3 center; // world space center of octree area
    private VoxelBody vox;

    public GameObject obj;
    private MeshRenderer mr;

    public const int SIZE = 16;        // size of visible voxel grid
    public const int TSIZE = SIZE + 5;   // total size with voxel border (done for normal smoothing)
    // so while size is 32, which means theres 32 blocks 
    // you need 33 verts to store those blocks think of 33 vertical lines and the blocks are the 32 white space in between
    // then add a buffer of 2 around (front and back so +4) for smoothing and normal calculation
    // so mesh goes from 2-34 basically (0 1, 35 36) are extras

    public const int MAX_DEPTH = 6;
    private float voxelSize;   // size of voxels for this tree (in meters)

    public Bounds area; // bounding box for area this tree represents (gonna be worthless once planets can rotate lol)
                        //private Bounds meshBounds;

    public Mesh mesh;

    public static bool splitThisFrame = true;

    public Octree(VoxelBody vox, Vector3 center, int depth, int branch) {
        this.vox = vox;
        this.depth = depth;
        this.branch = branch;
        voxelSize = Mathf.Pow(2, (MAX_DEPTH - depth));

        this.center = center;
        pos = center - new Vector3(2, 2, 2) * voxelSize - Vector3.one * (SIZE / 2f) * voxelSize;

        area = new Bounds(center, Vector3.one * voxelSize * SIZE);
    }

    public void generate() {
        voxels = WorldGenerator.CreateVoxels(TSIZE, depth, voxelSize, vox.radius, pos);
        voxels = VoxelUtils.SmoothVoxels(voxels);
        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize, 2, 2);

        data.CalculateNormals();
        //data.normals = VoxelUtils.CalculateMeshNormals(voxels, voxelSize, data.vertices);

        BuildGameObject(data);

    }

    // all the stuff that requires the main thread for Unity functions is done here
    // most other work is offloaded to helper threads. builds mesh and game object
    public void BuildGameObject(MeshData data, Vector3[] normals = null) {
        Color32 col = Color.HSVToRGB(2f / 3f / MAX_DEPTH * (MAX_DEPTH - depth), 1f, 1f);
        int size = data.vertices.Length;
        Color32[] colors = new Color32[size];
        for (int i = 0; i < size; i++) {
            colors[i] = col;
        }
        mesh = data.CreateMesh(colors);

        //meshObj = new GameObject("Voxel Mesh " + pos.x.ToString() + " " + pos.y.ToString() + " " + pos.z.ToString());
        obj = new GameObject("Tree " + depth + " " + center.x.ToString() + " " + center.y.ToString() + " " + center.z.ToString());
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mr = obj.AddComponent<MeshRenderer>();
        mr.material = vox.mat;
        mf.mesh = mesh;
        obj.transform.parent = vox.transform;
        obj.transform.localPosition = pos;
        OctreeViewer oct = obj.AddComponent<OctreeViewer>();
        oct.init(depth, branch, center, area);

        //MeshCollider collider = m_mesh.AddComponent<MeshCollider>();
        //collider.sharedMesh = mesh;

        //Debug.Log("Create mesh time = " + (Time.realtimeSinceStartup-startTime).ToString() );
    }


    public void update() {
        if (hasChildren) {
            if (shouldMerge()) {
                merge();
            } else {
                for (int i = 0; i < 8; i++) {
                    children[i].update();
                }
            }
        } else if (shouldSplit()) {
            split();
        }
    }

    public Octree getBlockingNeighbors() {
        for (int i = 0; i < 6; i++) {
            if (neighbors[i] != null && neighbors[i].depth < depth) {
                return neighbors[i];
            }
        }
        return null;
    }


    public void split() {
        splitThisFrame = true;
        Debug.Assert(depth <= MAX_DEPTH);

        Octree n = getBlockingNeighbors();
        while (n != null) {
            n.split();
        }

        for (int i = 0; i < 8; i++) {
            Vector3 coff = getChildOffset(i);
            Octree child = new Octree(vox, center + coff * SIZE * voxelSize * .25f, depth + 1, i);
            coff = ((coff + Vector3.one) / 2f) * (SIZE / 2f);
            //o.passVoxels(voxels, (int)coff.x, (int)coff.y, (int)coff.z);
            child.generate();

            child.obj.transform.parent = obj.transform;
            children[i] = child;
        }
        //setChildNeighbors();
        updateNeighbors(true);

        mr.enabled = false;
        hasChildren = true;
    }

    public void setChildNeighbors() {
        // need more updating for if higher rec neighbors are looking at you when you split

        //children[0].neighbors[0] = neighbors[0];
        //children[1].neighbors[0] = neighbors[0];

        // 0 1 4 5 Front 0
        // 1 2 5 6 Left  4
        // 2 3 6 7 Back  3
        // 3 0 7 4 Right 1
        // 0 1 2 3 Up    2
        // 4 5 6 7 Down  5

        // 0 4
        // 1 5
        // 2 6
        // 3 7
        for (int i = 0; i < 6; i++) {
            for (int j = 0; j < 4; j++) {
                children[neb[i][j]].neighbors[i] = neighbors[i];
            }
        }
    }

    private static int[][] neb = new int[][] {
        new int[] {0,1,4,5},
        new int[] {1,2,5,6},
        new int[] {2,3,6,7},
        new int[] {3,0,7,4},
        new int[] {0,1,2,3},
        new int[] {4,5,6,7}
    };

    private void updateNeighbors(bool splitting) {

    }

    private bool shouldSplit() {
        return depth < MAX_DEPTH && (vox.cam.position - center).sqrMagnitude < vox.squareSplitLevels[depth];
    }

    private bool shouldMerge() {
        return canMerge() && (vox.cam.position - center).sqrMagnitude > vox.squareSplitLevels[depth];
    }

    private bool canMerge() {
        for (int i = 0; i < 8; i++) {
            if (children[i].hasChildren) {
                return false;
            }
        }
        // or if childrens neighbor has children?
        return true;
    }

    private void merge() {
        updateNeighbors(false);
        for (int i = 0; i < 8; i++) {
            Object.Destroy(children[i].mesh);
            children[i] = null;
        }
        hasChildren = false;
        mr.enabled = true;
    }

    public void passVoxels(float[][][] v, int xo, int yo, int zo) {
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

    private Vector3 getChildOffset(int i) {
        switch (i) {
            case 0:
                return new Vector3(1, 1, 1);
            case 1:
                return new Vector3(-1, 1, 1);
            case 2:
                return new Vector3(-1, 1, -1);
            case 3:
                return new Vector3(1, 1, -1);
            case 4:
                return new Vector3(1, -1, 1);
            case 5:
                return new Vector3(-1, -1, 1);
            case 6:
                return new Vector3(-1, -1, -1);
            case 7:
                return new Vector3(1, -1, -1);
            default:
                return Vector3.zero;
        }
    }

    public Bounds getBounds() {
        return new Bounds(center, Vector3.one * voxelSize * SIZE);
    }


}
