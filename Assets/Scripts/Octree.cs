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

    private const int SIZE = 16;        // size of visible voxel grid
    private const int TSIZE = SIZE + 5;   // total size with voxel border
    // so while size is 32, which means theres 32 blocks 
    // you need 33 verts to store those blocks think of 33 vertical lines and the blocks are the 32 white space in between
    // then add a buffer of 2 around (front and back so +4) for smoothing and normal calculation
    // so mesh goes from 2-34 basically (0 1, 35 36) are extras

    public const int MAX_DEPTH = 6;
    private float voxelSize;   // size of voxels for this tree (in meters)

    public Bounds area; // bounding box for area this tree represents (gonna be worthless once planets can rotate lol)
                        //private Bounds meshBounds;

    public Mesh mesh;


    public Octree(VoxelBody vox, Vector3 center, int depth, int branch) {

        voxels = new float[TSIZE][][];
        for (int x = 0; x < TSIZE; x++) {
            voxels[x] = new float[TSIZE][];
            for (int y = 0; y < TSIZE; y++) {
                voxels[x][y] = new float[TSIZE];
            }
        }
        this.vox = vox;
        this.depth = depth;
        this.branch = branch;
        voxelSize = Mathf.Pow(2, (MAX_DEPTH - depth));

        this.center = center;
        pos = center - new Vector3(2, 2, 2) * voxelSize - Vector3.one * (SIZE / 2f) * voxelSize;

        area = new Bounds(center, Vector3.one * voxelSize * SIZE);
    }

    public void generate() {
        createVoxels();
        smoothVoxels();
        //calculateNormals();
        createMesh();
    }

    public void createVoxels() {
        //float startTime = Time.realtimeSinceStartup;
        //Creates the data the mesh is created form. Fills m_voxels with values between -1 and 1 where
        //-1 is a solid voxel and 1 is a empty voxel.

        // if root generate all new
        // otherwise generate every other because you inherited some from parent
        int s = (depth == 0) ? 0 : 1;
        s = 0;
        for (int x = s; x < TSIZE; x += s + 1) {
            for (int y = s; y < TSIZE; y += s + 1) {
                for (int z = s; z < TSIZE; z += s + 1) {
                    Vector3 worldPos = new Vector3(x, y, z) * voxelSize + pos;

                    float sqrMag = worldPos.sqrMagnitude;
                    //float blend = 1f;

                    float freq = 0.01f;
                    double d = SimplexNoise.noise(worldPos.x * freq, worldPos.y * freq, worldPos.z * freq);
                    //WorleySample w = Noise.Worley3(worldPos.x * freq, worldPos.y * freq, worldPos.z * freq, 2, DistanceFunction.EUCLIDIAN);
                    //double d = w.F[1] - w.F[0];

                    //d = (d + 1.0) / 2.0;
                    float surfHeight = vox.radius;
                    surfHeight += (float)d * 50f;

                    // need to figure out way to distribute smooth values over certain ranges
                    // for each block type so it looks smooth
                    // like give each block id a 0-1float range and blend between
                    //if (sqrMag < surfHeight * surfHeight) {   // assumes world is at origin
                    //    voxels[x][y][z] = (surfHeight * surfHeight - sqrMag)/1000f;
                    //} else {
                    //    voxels[x][y][z] = Mathf.Max(-1000f, (surfHeight * surfHeight - sqrMag))/1000f;
                    //}

                    // sqrt of 100,000 is around 300 so blending over that range
                    // so what if voxels was a byte array then blend from 0-127 inside then 127-255 outside
                    // and use each bit for lerping smoothly
                    // need to factor in stride somehow too probly
                    // because when low lod levels the distance to surface will be greater
                    //voxels[x][y][z] = (surfHeight * surfHeight - sqrMag) / 100000f;

                    voxels[x][y][z] = (float)d;

                    voxels[x][y][z] = Mathf.Clamp(voxels[x][y][z], -1.0f, 1.0f);
                    //voxels[x][y][z] = (float)d;
                }
            }
        }
        //Debug.Log("Create voxels time = " + (Time.realtimeSinceStartup-startTime).ToString() );
    }

    public void smoothVoxels() {
        //float startTime = Time.realtimeSinceStartup;

        //This averages a voxel with all its neighbours. Its is a optional step
        //but I think it looks nicer. You might what to do a fancier smoothing step
        //like a gaussian blur

        float[][][] smoothed = new float[TSIZE][][];
        for (int x = 0; x < TSIZE; x++) {
            smoothed[x] = new float[TSIZE][];
            for (int y = 0; y < TSIZE; y++) {
                smoothed[x][y] = new float[TSIZE];
            }
        }

        for (int x = 1; x < TSIZE - 1; x++) {
            for (int y = 1; y < TSIZE - 1; y++) {
                for (int z = 1; z < TSIZE - 1; z++) {
                    float ht = 0.0f;

                    for (int i = 0; i < 27; i++) {
                        ht += voxels[x + sampler[i][0]][y + sampler[i][1]][z + sampler[i][2]];
                    }

                    smoothed[x][y][z] = ht / 27.0f;
                }
            }
        }

        voxels = smoothed;

        //Debug.Log("Smooth voxels time = " + (Time.realtimeSinceStartup-startTime).ToString() );
    }

    public void createMesh() {
        //float startTime = Time.realtimeSinceStartup;

        mesh = MarchingCubes.CreateMesh(voxels, voxelSize, 2, 2);
        if (mesh == null) return;

        int size = mesh.vertices.Length;

        if (normals != null) {
            Vector3[] norms = new Vector3[size];
            Vector3[] verts = mesh.vertices;

            //Each verts in the mesh generated is its position in the voxel array
            //and you can use this to find what the normal at this position.
            //The verts are not at whole numbers though so you need to use trilinear interpolation
            //to find the normal for that position

            for (int i = 0; i < size; i++)
                norms[i] = triLinearInterpolateNorms(verts[i] / voxelSize);

            mesh.normals = norms;
        } else {
            mesh.RecalculateNormals();
        }

        Color32 col = Color.HSVToRGB(2f / 3f / MAX_DEPTH * (MAX_DEPTH - depth), 1f, 1f);

        Color32[] colors = new Color32[size];
        for (int i = 0; i < size; i++) {
            colors[i] = col;
        }
        mesh.colors32 = colors;


        //Color[] control = new Color[size];
        //Vector3[] meshNormals = mesh.normals;

        //for (int i = 0; i < size; i++) {
        //    //This creates a control map used to texture the mesh based on the slope
        //    //of the vert. Its very basic and if you modify how this works yoou will
        //    //you will probably need to modify the shader as well.
        //    float dpUp = Vector3.Dot(meshNormals[i], Vector3.up);

        //    //Red channel is the sand on flat areas
        //    float R = (Mathf.Max(0.0f, dpUp) < 0.8f) ? 0.0f : 1.0f;
        //    //Green channel is the gravel on the sloped areas
        //    float G = Mathf.Pow(Mathf.Abs(dpUp), 2.0f);

        //    //Whats left end up being the rock face on the vertical areas

        //    control[i] = new Color(R, G, 0, 0);
        //}

        ////May as well store in colors 
        //mesh.colors = control;

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

    public void calculateNormals() {
        //float startTime = Time.realtimeSinceStartup;

        //This calculates the normal of each voxel. If you have a 3d array of data
        //the normal is the derivitive of the x, y and z axis.
        //Normally you need to flip the normal (*-1) but it is not needed in this case.
        //If you dont call this function the normals that Unity generates for a mesh are used.

        normals = new Vector3[TSIZE][][];
        for (int x = 0; x < TSIZE; x++) {
            normals[x] = new Vector3[TSIZE][];
            for (int y = 0; y < TSIZE; y++) {
                normals[x][y] = new Vector3[TSIZE];
            }
        }

        for (int x = 2; x < TSIZE - 2; x++) {
            for (int y = 2; y < TSIZE - 2; y++) {
                for (int z = 2; z < TSIZE - 2; z++) {
                    float dx = voxels[x + 1][y][z] - voxels[x - 1][y][z];
                    float dy = voxels[x][y + 1][z] - voxels[x][y - 1][z];
                    float dz = voxels[x][y][z + 1] - voxels[x][y][z - 1];

                    normals[x][y][z] = -Vector3.Normalize(new Vector3(dx, dy, dz));
                }
            }
        }

        //Debug.Log("Calculate normals time = " + (Time.realtimeSinceStartup-startTime).ToString() );

    }

    private Vector3 triLinearInterpolateNorms(Vector3 p) {
        int x = (int)p.x;
        int y = (int)p.y;
        int z = (int)p.z;

        float fx = p.x - x;
        float fy = p.y - y;
        float fz = p.z - z;

        if (x < 0 || x >= TSIZE || y < 0 || y >= TSIZE || z < 0 || z >= TSIZE) {
            Debug.Log(x + " " + y + " " + z);
        }

        Vector3 x0 = normals[x][y][z] * (1.0f - fx) + normals[x + 1][y][z] * fx;
        Vector3 x1 = normals[x][y][z + 1] * (1.0f - fx) + normals[x + 1][y][z + 1] * fx;

        Vector3 x2 = normals[x][y + 1][z] * (1.0f - fx) + normals[x + 1][y + 1][z] * fx;
        Vector3 x3 = normals[x][y + 1][z + 1] * (1.0f - fx) + normals[x + 1][y + 1][z + 1] * fx;

        Vector3 z0 = x0 * (1.0f - fz) + x1 * fz;
        Vector3 z1 = x2 * (1.0f - fz) + x3 * fz;

        return z0 * (1.0f - fy) + z1 * fy;
    }

    private static int[][] sampler = new int[][] {
        new int[]{1,-1,0},
        new int[]{1,-1,1},
        new int[]{0,-1,1},
        new int[]{-1,-1,1},
        new int[]{-1,-1,0},
        new int[]{-1,-1,-1},
        new int[]{0,-1,-1},
        new int[]{1,-1,-1},
        new int[]{0,-1,0},
        new int[]{1,0,0},
        new int[]{1,0,1},
        new int[]{0,0,1},
        new int[]{-1,0,1},
        new int[]{-1,0,0},
        new int[]{-1,0,-1},
        new int[]{0,0,-1},
        new int[]{1,0,-1},
        new int[]{0,0,0},
        new int[]{1,1,0},
        new int[]{1,1,1},
        new int[]{0,1,1},
        new int[]{-1,1,1},
        new int[]{-1,1,0},
        new int[]{-1,1,-1},
        new int[]{0,1,-1},
        new int[]{1,1,-1},
        new int[]{0,1,0}
    };

}
