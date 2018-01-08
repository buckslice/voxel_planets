using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MCSimpleDispatch : MonoBehaviour {

    [SerializeField]
    int resolution = 16;     // should be same as Octree.SIZE
    [SerializeField]
    ComputeShader MarchingCubesCS;
    int kernelMC;

    public Material meshMat;

    MeshFilter mf;

    ComputeBuffer appendBuffer;
    ComputeBuffer argBuffer;

    float[] data;
    int[] count;
    bool working = false;
    bool haveWorked = false;
    bool retrievedData = false;
    bool retrievedCount = false;

    // Use this for initialization
    void Awake() {
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mf = gameObject.AddComponent<MeshFilter>();
        mr.material = meshMat;

        kernelMC = MarchingCubesCS.FindKernel("MarchingCubes");
        //kernelTripleCount = MarchingCubesCS.FindKernel("TripleCount");

        MarchingCubesCS.SetInt("_gridSize", resolution);
        MarchingCubesCS.SetFloat("_isoLevel", 0.0f);    // halfway point for noise in range [-1, 1]

        // * 5 since up to 5 triangles per cube in marching cubes
        // sizeof(float)*6 because thats the size of a vertex, * 3 cuz 3 verts per triangle
        // also one less than resolution because 64x64x64 density map represents 63*63*63 cubes only
        int maxTris = (resolution - 1) * (resolution - 1) * (resolution - 1) * 5;
        int floatsPerTri = 6 * 3;

        appendBuffer = new ComputeBuffer(maxTris, floatsPerTri * sizeof(float), ComputeBufferType.Append);
        argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        appendBuffer.SetCounterValue(0);

        // init retrieval arrays
        data = new float[maxTris * floatsPerTri];
        count = new int[4];

        long bytes = maxTris * floatsPerTri * sizeof(float);
        Debug.Log((float)bytes / 1000000 + " mb per chunk");

    }

    void Start() {
        Texture tex = Camera.main.GetComponent<RaymarchGenericTexture>().volume;
        if (tex) {
            Launch(tex);
        }
    }

    int[] defaultArgs = new int[] { 0, 1, 0, 0 };
    public void Launch(Texture density) {
        // can be either Texture3D or RenderTexture
        Debug.Assert(density.dimension == UnityEngine.Rendering.TextureDimension.Tex3D);
        // set compute shader references
        Graphics.ClearRandomWriteTargets(); // not sure if needed anymore
        appendBuffer.SetCounterValue(0);

        MarchingCubesCS.SetBuffer(kernelMC, ShaderProps.trianglesRW, appendBuffer);
        MarchingCubesCS.SetTexture(kernelMC, ShaderProps.densityTexture, density);

        MarchingCubesCS.Dispatch(kernelMC, resolution / 8, resolution / 8, resolution / 8);

        argBuffer.SetData(defaultArgs);

        // copy the counter variables from first buffer into second
        ComputeBuffer.CopyCount(appendBuffer, argBuffer, 0);

        AsyncTextureReader.RequestBufferData(appendBuffer);
        AsyncTextureReader.RequestBufferData(argBuffer);

        working = true;
        // launch coroutine to wait and update
    }

    public void Update() {
        if (!working) {
            return;
        }

        if (!retrievedData) {
            AsyncTextureReader.Status status = AsyncTextureReader.RetrieveBufferData(appendBuffer, data);
            if (status == AsyncTextureReader.Status.Succeeded) {
                retrievedData = true;
            }
        }
        if (!retrievedCount) {
            AsyncTextureReader.Status status = AsyncTextureReader.RetrieveBufferData(argBuffer, count);
            if (status == AsyncTextureReader.Status.Succeeded) {
                retrievedCount = true;
            }
        }
        if (retrievedData && retrievedCount) {
            MeshData data = BuildMeshData(1.0f);
            Mesh mesh = data.CreateMesh();
            mf.mesh = mesh;
            working = false;
        }
    }

    MeshData BuildMeshData(float voxelSize) {
        int c = count[0]; // get number of triangles (used to have to triple because graphics.drawprocind needed number of verts)
        if (c == 0) {
            Debug.Log("Empty Mesh");
            return null;
        }

        // this array construction could be done on a background thread
        // still need to build mesh on main thread tho
        Vector3[] verts = new Vector3[c * 3];
        Vector3[] norms = new Vector3[c * 3];
        int[] tris = new int[c * 3];

        for (int i = 0; i < verts.Length; i++) {
            float f0 = data[i * 6 + 0];
            float f1 = data[i * 6 + 1];
            float f2 = data[i * 6 + 2];
            float f3 = data[i * 6 + 3];
            float f4 = data[i * 6 + 4];
            float f5 = data[i * 6 + 5];

            //verts[i] = (new Vector3(f0, f1, f2)+Vector3.one*0.5f) * resolution * voxelSize;
            verts[i] = (new Vector3(f0, f1, f2)) * resolution * voxelSize;
            norms[i] = new Vector3(f3, f4, f5);

            tris[i] = i;
        }

        return new MeshData(verts, norms, tris);

    }

    public void Destroy() {
        appendBuffer.Release();
        argBuffer.Release();

        AsyncTextureReader.ReleaseTempResources(appendBuffer);
        AsyncTextureReader.ReleaseTempResources(argBuffer);
    }

}
