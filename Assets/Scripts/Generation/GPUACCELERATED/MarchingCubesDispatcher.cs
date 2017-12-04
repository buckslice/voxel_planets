using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarchingCubesDispatcher : MonoBehaviour {

    static MarchingCubesDispatcher instance;

    [SerializeField]
    int resolution = 16;     // should be same as Octree.SIZE
    [SerializeField]
    ComputeShader MarchingCubesCS;
    [SerializeField]
    Material noiseMat;

    const int WORKERS = 8;
    static MarchingCubesWorker[] workers;

    int kernelMC;
    //int kernelTripleCount;  // unused in this mesh version

    static List<ChunkRequest> requests;

    public Text countText;

    // Use this for initialization
    void Awake() {
        if (!SystemInfo.supportsComputeShaders) {
            Debug.LogError("THIS IS BAD");
            DestroyImmediate(gameObject);
            return;
        }

        if (instance == null) {
            instance = this;
        } else {
            DestroyImmediate(this);
            return;
        }

        requests = new List<ChunkRequest>();

        kernelMC = MarchingCubesCS.FindKernel("MarchingCubes");
        //kernelTripleCount = MarchingCubesCS.FindKernel("TripleCount");

        MarchingCubesCS.SetInt("_gridSize", resolution);
        MarchingCubesCS.SetFloat("_isoLevel", 0.0f);    // halfway point for noise in range [-1, 1]

        workers = new MarchingCubesWorker[WORKERS];
        for (int i = 0; i < workers.Length; ++i) {
            workers[i] = new MarchingCubesWorker(resolution, kernelMC, MarchingCubesCS, noiseMat);
        }
    }

    // Update is called once per frame
    void Update() {

        // update workers
        for (int i = 0; i < workers.Length; ++i) {
            workers[i].Update();
            if (workers[i].free && requests.Count > 0) {
                // get request
                int end = requests.Count - 1;
                ChunkRequest req = requests[end];
                requests.RemoveAt(end);

                workers[i].Launch(req);
            }
        }
        countText.text = "Requests: " + requests.Count;
    }

    public static void RequestChunk(Vector3 offset, float voxelSize, OnReceived callback) {
        requests.Add(new ChunkRequest(offset, voxelSize, callback));
    }

    // clears chunk request list (tells each worker to get rid of next mesh if they are mid process)
    public static void ClearRequests() {
        requests.Clear();
        for(int i = 0; i < workers.Length; ++i) {
            workers[i].trashNext = true;
        }
    }

    void OnDestroy() {
        for (int i = 0; i < workers.Length; ++i) {
            workers[i].Destroy();
        }
    }

}

public delegate void OnReceived(MeshData mesh);

class ChunkRequest {
    public Vector3 offset;
    public float voxelSize;
    public OnReceived callback;
    public ChunkRequest(Vector3 offset, float voxelSize, OnReceived callback) {
        this.offset = offset;
        this.voxelSize = voxelSize;
        this.callback = callback;
    }
}

class MarchingCubesWorker {
    int resolution;
    int kernelMC;
    ComputeShader mccs;
    Material noiseMat;

    ComputeBuffer appendBuffer;
    ComputeBuffer argBuffer;

    RenderTexture density;

    float[] data;
    int[] count;

    public bool free = true;
    bool retrievedData = false;
    bool retrievedCount = false;
    bool requestedEver = false; // only for editor rly incase closed before getting any requests (rare)

    ChunkRequest cur = null;

    int maxTris;
    int floatsPerTri;

    public bool trashNext = false; 

    public MarchingCubesWorker(int resolution, int kernelMC, ComputeShader mccs, Material noiseMat) {
        this.resolution = resolution;
        this.kernelMC = kernelMC;
        this.mccs = mccs;
        this.noiseMat = noiseMat;

        // * 5 since up to 5 triangles per cube in marching cubes
        // sizeof(float)*6 because thats the size of a vertex, * 3 cuz 3 verts per triangle
        // also one less than resolution because 64x64x64 density map represents 63*63*63 cubes only
        maxTris = (resolution - 1) * (resolution - 1) * (resolution - 1) * 5;
        floatsPerTri = 6 * 3;

        appendBuffer = new ComputeBuffer(maxTris, floatsPerTri * sizeof(float), ComputeBufferType.Append);
        argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        appendBuffer.SetCounterValue(0);

        // init retrieval arrays
        data = new float[maxTris * floatsPerTri];
        count = new int[4];

        // create render texture
        // try with full res instead of just RHalf
        density = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear) {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = resolution,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            enableRandomWrite = true,
            autoGenerateMips = false
        };
        density.Create();

        if (!debugged) {
            long bytes = maxTris * floatsPerTri * sizeof(float);
            Debug.Log((float)bytes / 1000000 + " mb per chunk");
            debugged = true;
        }
    }
    static bool debugged = false;

    public void Launch(ChunkRequest cr) {
        free = false;
        trashNext = false;
        retrievedData = false;
        retrievedCount = false;
        cur = cr;   // save current chunk request

        BlitNoise();

        DispatchMC();

        // this maybe can fail? should prob check for this case
        AsyncTextureReader.RequestBufferData(appendBuffer);
        AsyncTextureReader.RequestBufferData(argBuffer);
        requestedEver = true;
    }

    public void Update() {
        if (free) {
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
            if (trashNext) {
                trashNext = false;
            } else {
                cur.callback(BuildMeshData());
            }

            free = true;
        }
    }

    public void Destroy() {
        appendBuffer.Release();
        argBuffer.Release();

        if (requestedEver) {
            AsyncTextureReader.ReleaseTempResources(appendBuffer);
            AsyncTextureReader.ReleaseTempResources(argBuffer);
        }
    }

    void BlitNoise() {
        if (!density.IsCreated()) {    // texture can get uncreated in certain situations i think
            Debug.LogWarning("Something is wrong lol!");
        }

        noiseMat.SetVector(ShaderProps.LocalOffset, cur.offset);
        noiseMat.SetFloat(ShaderProps.Resolution, resolution);
        noiseMat.SetFloat(ShaderProps.Size, cur.voxelSize);

        GL.PushMatrix();
        GL.LoadOrtho();
        for (int i = 0; i < resolution; ++i) {
            Graphics.SetRenderTarget(density, 0, CubemapFace.Unknown, i);
            //noiseMat.SetFloat("_VolumeSlice", i / 64.0f); // this worked but 3D tex coords seems better

            // size-1 so last value ends at 1.0f
            // x and y are getting interpolated in shader which does it different (see shader for more info)
            float z = Mathf.Clamp01(i / (float)(resolution - 1));
            //z = Mathf.Lerp(0.5f / size, 1.0f - 0.5f / size, z);

            noiseMat.SetPass(0);

            // custom blit get at me
            GL.Begin(GL.QUADS);

            GL.TexCoord3(0, 0, z);
            GL.Vertex3(0, 0, 0);
            GL.TexCoord3(1, 0, z);
            GL.Vertex3(1, 0, 0);
            GL.TexCoord3(1, 1, z);
            GL.Vertex3(1, 1, 0);
            GL.TexCoord3(0, 1, z);
            GL.Vertex3(0, 1, 0);

            GL.End();
        }
        GL.PopMatrix();
    }

    int[] defaultArgs = new int[] { 0, 1, 0, 0 };  // could move this outside to avoid garbog
    void DispatchMC() {
        // set compute shader references
        Graphics.ClearRandomWriteTargets();
        appendBuffer.SetCounterValue(0);

        mccs.SetBuffer(kernelMC, ShaderProps.trianglesRW, appendBuffer);
        mccs.SetTexture(kernelMC, ShaderProps.densityTexture, density);

        mccs.Dispatch(kernelMC, resolution / 8, resolution / 8, resolution / 8);

        argBuffer.SetData(defaultArgs);

        // copy the counter variables from first buffer into second
        ComputeBuffer.CopyCount(appendBuffer, argBuffer, 0);

    }

    MeshData BuildMeshData() {
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
            verts[i] = (new Vector3(f0, f1, f2)) * resolution * cur.voxelSize;
            norms[i] = new Vector3(f3, f4, f5);

            tris[i] = i;
        }

        return new MeshData(verts, norms, tris);

    }
}