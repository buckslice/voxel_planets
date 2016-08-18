﻿using UnityEngine;
using UnityEditor;

public class CelestialBody : MonoBehaviour {

    public Material mat;
    public float[] squareSplitLevels;
    public Transform cam;
    public float surfaceRadius = 500.0f;
    public float atmosphereRadius = 1000.0f;

    private Octree root = null;

#if true
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
#endif

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

#if false

    void Start() {
        ChunkObject go = SplitManager.GetObject();

        MeshData data = IMarchingCubes.CalculateMeshData();
        Mesh mesh = data.CreateMesh();

        go.mf.sharedMesh = mesh;
        go.mr.material = mat;

        
    }
#endif

}