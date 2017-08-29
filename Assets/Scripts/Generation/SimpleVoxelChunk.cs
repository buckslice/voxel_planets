using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleVoxelChunk : MonoBehaviour {

    public Material mat;

    // Use this for initialization
    void Start() {

        float voxelSize = 1.0f;
        int size = 33;

        Array3<Voxel> voxels = WorldGenerator.CreateVoxels(size, 0, voxelSize, Vector3.zero);
        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize);
        data.CalculateNormals();

        Mesh mesh = null;
        if (data != null) {
            mesh = data.CreateMesh();
        }

        ChunkObject obj = SplitManager.GetObject();
        obj.ov.shouldDraw = true;
        obj.mr.material = mat;
        obj.mf.mesh = mesh;


        Vector3 center = Vector3.one * (size - 1) / 2.0f;
        Bounds area = new Bounds(center, Vector3.one * voxelSize * (size - 1));

        obj.ov.init(0, 0, area, null, Color.red);

    }

    // Update is called once per frame
    void Update() {

    }
}
