using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
