using UnityEngine;
using System.Collections;

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
    public static Vector3[] CalculateMeshNormals(float[][][] voxels, float voxelSize, Vector3[] verts) {
        // calculates the normal of each voxel. If you have a 3d array of data
        // the normal is the derivitive of the x, y and z axis.
        // normally you need to flip the normal (*-1) but it is not needed in this case.
        int size = voxels.Length;
        Vector3[][][] normals = Init3DArray<Vector3>(size);
        for (int x = 2; x < size - 2; x++) {
            for (int y = 2; y < size - 2; y++) {
                for (int z = 2; z < size - 2; z++) {
                    float dx = voxels[x + 1][y][z] - voxels[x - 1][y][z];
                    float dy = voxels[x][y + 1][z] - voxels[x][y - 1][z];
                    float dz = voxels[x][y][z + 1] - voxels[x][y][z - 1];

                    normals[x][y][z] = -Vector3.Normalize(new Vector3(dx, dy, dz));
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
    public static Vector3 TriLerpNormals(Vector3 p, Vector3[][][] normals) {
        int x = (int)p.x;
        int y = (int)p.y;
        int z = (int)p.z;

        float fx = p.x - x;
        float fy = p.y - y;
        float fz = p.z - z;

        int size = normals.Length;
        Debug.Assert(x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size);

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

    public MeshData(Vector3[] vertices, int[] triangles) {
        this.vertices = vertices;
        this.triangles = triangles;
    }

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

            normals[a] = norm;
            normals[b] = norm;
            normals[c] = norm;

            // meshes from MarchingCubes dont have any triangle sharing so 
            // no point in doing this or final normalization at end since will still be flat
            //normals[a] += norm;
            //normals[b] += norm;
            //normals[c] += norm;
        }
        //for (int i = 0; i < norms.Length; ++i) {
        //    normals[i].Normalize();
        //}
    }

    public Mesh CreateMesh(Color32[] colors = null) {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        if (normals == null) {  // if no normals already assigned then recalculate them
            CalculateNormals();
        } else {
            mesh.normals = normals;
        }

        if (colors == null) {
            colors = WorldGenerator.GenerateControlMap(normals);
        }
        mesh.colors32 = colors;

        return mesh;
    }
}