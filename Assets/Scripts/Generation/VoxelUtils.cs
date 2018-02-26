using UnityEngine;
using System.Collections.Generic;

// much code here is based off of scrawkblogs marching cubes tutorial
// and sebastian lagues proc gen youtube series
public static class VoxelUtils {
    // TODO test if generics slow stuff down
    public static T[][][] Init3DArray<T>(int size) {
        T[][][] array = new T[size][][];
        for (int x = 0; x < size; x++) {
            array[x] = new T[size][];
            for (int y = 0; y < size; y++) {
                array[x][y] = new T[size];
            }
        }
        return array;
    }

    private static int[][] sampler = new int[][] {
        new int[]{1,-1,0}, new int[]{1,-1,1},  new int[]{0,-1,1}, new int[]{-1,-1,1},
        new int[]{-1,-1,0},new int[]{-1,-1,-1},new int[]{0,-1,-1},new int[]{1,-1,-1},
        new int[]{0,-1,0}, new int[]{1,0,0},   new int[]{1,0,1},  new int[]{0,0,1},
        new int[]{-1,0,1}, new int[]{-1,0,0},  new int[]{-1,0,-1},new int[]{0,0,-1},
        new int[]{1,0,-1}, new int[]{0,0,0},   new int[]{1,1,0},  new int[]{1,1,1},
        new int[]{0,1,1},  new int[]{-1,1,1},  new int[]{-1,1,0}, new int[]{-1,1,-1},
        new int[]{0,1,-1}, new int[]{1,1,-1},  new int[]{0,1,0}
    };
    // kinda filth that this returns a new array
    // i think it has to though since each voxel needs to sample nearby voxels
    public static float[][][] SmoothVoxels(float[][][] voxels) {
        int size = voxels.Length;
        float[][][] smoothed = Init3DArray<float>(size);

        for (int x = 1; x < size - 1; x++) {
            for (int y = 1; y < size - 1; y++) {
                for (int z = 1; z < size - 1; z++) {
                    float ht = 0.0f;

                    for (int i = 0; i < 27; i++) {
                        ht += voxels[x + sampler[i][0]][y + sampler[i][1]][z + sampler[i][2]];
                    }

                    smoothed[x][y][z] = ht / 27.0f;
                }
            }
        }

        return smoothed;
    }

    // takes in voxel array and MeshData vertices and returns array of mesh normals
    public static Vector3[] CalculateSmoothNormals(Array3<sbyte> voxels, float voxelSize, Vector3[] verts) {
        // calculates the normal of each voxel. If you have a 3d array of data
        // the normal is the derivitive of the x, y and z axis.
        // normally you need to flip the normal (*-1) but it is not needed in this case.
        int size = voxels.size;
        Vector3[][][] normals = Init3DArray<Vector3>(size); // TODO reuse this
        for (int x = 2; x < size - 2; x++) {
            for (int y = 2; y < size - 2; y++) {
                for (int z = 2; z < size - 2; z++) {
                    float dx = voxels[x + 1, y, z] - voxels[x - 1, y, z];
                    float dy = voxels[x, y + 1, z] - voxels[x, y - 1, z];
                    float dz = voxels[x, y, z + 1] - voxels[x, y, z - 1];

                    normals[x][y][z] = Vector3.Normalize(new Vector3(dx, dy, dz) / 128.0f * voxelSize);
                }
            }
        }
        int numVerts = verts.Length;
        Vector3[] meshNorms = new Vector3[numVerts];
        for (int i = 0; i < numVerts; ++i) {
            meshNorms[i] = TriLerpNormals(verts[i] / voxelSize, normals);
        }
        return meshNorms;
    }

    // performs trilinear interpolation to find normal
    // each verts in the mesh generated is its position in the voxel array
    // and you can use this to find what the normal at this position.
    // the verts are not at whole numbers though so you need to use trilinear interpolation
    // to find the normal for that position
    private static Vector3 TriLerpNormals(Vector3 p, Vector3[][][] normals) {
        int x = (int)p.x;
        int y = (int)p.y;
        int z = (int)p.z;

        float fx = p.x - x;
        float fy = p.y - y;
        float fz = p.z - z;

        int size = normals.Length;

        // if not generating additional voxel data around edges then this will assert probably
        //Debug.Assert(x >= 0 && x < size-1 && y >= 0 && y < size-1 && z >= 0 && z < size-1);

        Vector3 x0 = normals[x][y][z] * (1.0f - fx) + normals[x + 1][y][z] * fx;
        Vector3 x1 = normals[x][y][z + 1] * (1.0f - fx) + normals[x + 1][y][z + 1] * fx;

        Vector3 x2 = normals[x][y + 1][z] * (1.0f - fx) + normals[x + 1][y + 1][z] * fx;
        Vector3 x3 = normals[x][y + 1][z + 1] * (1.0f - fx) + normals[x + 1][y + 1][z + 1] * fx;

        Vector3 z0 = x0 * (1.0f - fz) + x1 * fz;
        Vector3 z1 = x2 * (1.0f - fz) + x3 * fz;

        return z0 * (1.0f - fy) + z1 * fy;
    }

}

// kinda feel like removing the functions to elsewhere and turn this back into struct...
public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals = null;
    public Color32[] colors = null;

    public MeshData() {
    }

    public MeshData(Vector3[] vertices) {
        this.vertices = vertices;

        // marching cubes indices are non shared and easy to generate (TODO change this back to normal so can be used by other stuff)
        int len = vertices.Length;
        triangles = new int[len];
        for (int i = 0; i < len; ++i) {
            triangles[i] = i;
        }
    }

    public MeshData(Vector3[] vertices, int[] triangles) {
        this.vertices = vertices;
        this.triangles = triangles;
    }

    public MeshData(Vector3[] vertices, Vector3[] normals, int[] triangles) {
        this.vertices = vertices;
        this.normals = normals;
        this.triangles = triangles;
    }

    public MeshData(Vector3[] vertices, Vector3[] normals, Color32[] colors, int[] triangles) {
        this.vertices = vertices;
        this.normals = normals;
        this.colors = colors;
        this.triangles = triangles;
    }

    //public MeshData(List<Vector3> vertices, List<int> triangles) {
    //    this.vertices = vertices;
    //    this.triangles = triangles;
    //}

    // this will calculate normals the conventional way
    // separating it out so we can do it before mesh is created
    // to maximize threading gains
    public void CalculateNormals() {
        normals = new Vector3[vertices.Length];
        int tris = triangles.Length / 3;
        for (int i = 0; i < tris; ++i) {
            int a = triangles[i * 3];
            int b = triangles[i * 3 + 1];
            int c = triangles[i * 3 + 2];

            Vector3 va = vertices[a];
            Vector3 vb = vertices[b];
            Vector3 vc = vertices[c];

            Vector3 norm = Vector3.Cross(vb - va, vc - va).normalized;
            //Vector3 norm = Vector3.Cross(vb - va, vc - va);

            normals[a] = norm;
            normals[b] = norm;
            normals[c] = norm;

            //// only do this part and last part if using vertex sharing
            //normals[a] += norm;
            //normals[b] += norm;
            //normals[c] += norm;
        }
        //for (int i = 0; i < normals.Length; ++i) {
        //    normals[i].Normalize();
        //}
    }

    public void CalculateSharedNormals() {
        normals = new Vector3[vertices.Length];
        int tris = triangles.Length / 3;
        for (int i = 0; i < tris; ++i) {
            int a = triangles[i * 3];
            int b = triangles[i * 3 + 1];
            int c = triangles[i * 3 + 2];

            Vector3 va = vertices[a];
            Vector3 vb = vertices[b];
            Vector3 vc = vertices[c];

            Vector3 norm = Vector3.Cross(vb - va, vc - va);

            // only do this part and last part if using vertex sharing
            normals[a] += norm;
            normals[b] += norm;
            normals[c] += norm;
        }
        for (int i = 0; i < normals.Length; ++i) {
            normals[i].Normalize();
        }
    }

    // removes duplicate verts and recalculates triangles
    public void CalculateVertexSharing() {
        Dictionary<Vector3, int> shared = new Dictionary<Vector3, int>();
        List<Vector3> uniques = new List<Vector3>();
        int len = vertices.Length; // default marching cubes triangles have no sharing
        for (int i = 0; i < len; ++i) {
            Vector3 v = vertices[i]; // normally it would be vertices[triangles[i]]
            int index = 0;
            if (!shared.TryGetValue(v, out index)) {    // cant find in dict so add new
                index = uniques.Count;
                shared[v] = index;
                uniques.Add(v);
            }   // else it was found so index will be correct
            triangles[i] = index;
        }
        vertices = uniques.ToArray();

    }

    public void SplitEdgesCalcSmoothness() {
        // at this point vertices are sharing and have smooth normals
        int len = triangles.Length;

        Vector3[] finalVerts = new Vector3[len];
        Vector3[] finalNorms = new Vector3[len];

        for (int i = 0; i < len; i += 3) {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            Vector3 v1 = vertices[a];
            Vector3 v2 = vertices[b];
            Vector3 v3 = vertices[c];

            Vector3 n1 = normals[a];
            Vector3 n2 = normals[b];
            Vector3 n3 = normals[c];

            finalVerts[i] = v1;
            finalVerts[i + 1] = v2;
            finalVerts[i + 2] = v3;

            Vector3 va = vertices[a];
            Vector3 vb = vertices[b];
            Vector3 vc = vertices[c];
            Vector3 faceNormal = Vector3.Cross(vb - va, vc - va).normalized;

            // if under min angle, totally smooth, if over max, totally sharp, otherwise blend between
            // 16.0 was good threshold angle between sharp and smooth if no blend range (according to jlx)
            const float minAngle = 20.0f;
            const float maxAngle = 40.0f;
            float t1 = Mathf.InverseLerp(minAngle, maxAngle, Vector3.Angle(faceNormal, n1));
            float t2 = Mathf.InverseLerp(minAngle, maxAngle, Vector3.Angle(faceNormal, n2));
            float t3 = Mathf.InverseLerp(minAngle, maxAngle, Vector3.Angle(faceNormal, n3));

            finalNorms[i] = Vector3.Lerp(n1, faceNormal, t1);
            finalNorms[i + 1] = Vector3.Lerp(n2, faceNormal, t2);
            finalNorms[i + 2] = Vector3.Lerp(n3, faceNormal, t3);

            // reassign tris back no sharing mode
            triangles[i] = i;
            triangles[i + 1] = i + 1;
            triangles[i + 2] = i + 2;
        }

        vertices = finalVerts;
        normals = finalNorms;

    }

    public void CalculateColorsByDepth(int depth) {
        Color32 col = Color.HSVToRGB(2f / 3f / Octree.MAX_DEPTH * (Octree.MAX_DEPTH - depth), 1f, 1f);
        int size = vertices.Length;
        colors = new Color32[size];
        for (int i = 0; i < size; i++) {
            colors[i] = col;
        }
    }

    public Mesh CreateMesh() {
        if (vertices.Length == 0 || triangles.Length == 0) {
            return null;
        }

        Mesh mesh = new Mesh();
        if (triangles.Length > 65000) {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        // if you want meshes to be centered on planet
        //for(int i = 0; i < vertices.Length; ++i) {
        //    vertices[i] += pos;
        //}

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        if (normals == null) {  // if no normals already assigned then recalculate them
            CalculateNormals();
        }
        mesh.normals = normals;

        if (colors != null) {
            //colors = WorldGenerator.GenerateControlMap(normals);
            mesh.colors32 = colors;
        }

        mesh.RecalculateBounds();   // didnt have this call before i dunno not sure if needed
        return mesh;
    }

}

public class SplitData {
    public Octree tree;
    public MeshData[] data;
    private int i = 0;

    public SplitData(Octree tree) {
        this.tree = tree;
        data = new MeshData[8];
    }

    public void Add(MeshData data) {
        this.data[i++] = data;
    }
}


public class MeshBuilder {
    private List<int> indices = new List<int>();
    private List<Vector3> vertices = new List<Vector3>();
    //private List<Vector3> normals = new List<Vector3>();

    public void AddIndex(int i) {
        indices.Add(i);
    }

    public void AddVertex(Vector3 v) {//, Vector3 n) {
        vertices.Add(v);
        //normals.Add(n);
    }

    public int GetLastAddedVertIndex() {
        return vertices.Count - 1;
    }

    public MeshData ToMeshData() {
        MeshData data = new MeshData();
        data.vertices = vertices.ToArray();
        //data.normals = normals.ToArray();
        data.triangles = indices.ToArray();
        return data;
    }

}