﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MarchingCubesDispatcher : MonoBehaviour {

    [SerializeField]
    int resolution = 16;     // should be same as Octree.SIZE
    [SerializeField]
    int numWorkers = 8;     // number of concurrent workers
    [SerializeField]
    ComputeShader MarchingCubesCS;
    [SerializeField]
    Material noiseMat;
    static MarchingCubesWorker[] workers;

    int kernelMC;
    //int kernelTripleCount;  // unused in this mesh version

    //static List<ChunkRequest> jobList;

    static FastPriorityQueue<ChunkRequest> queue;

    public Text countText;

    // Use this for initialization
    static MarchingCubesDispatcher instance; // just used to make sure only one of these at once
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

        queue = new FastPriorityQueue<ChunkRequest>(5000);

        kernelMC = MarchingCubesCS.FindKernel("MarchingCubes");
        //kernelTripleCount = MarchingCubesCS.FindKernel("TripleCount");

        MarchingCubesCS.SetInt("_gridSize", resolution);
        MarchingCubesCS.SetFloat("_isoLevel", 0.0f);    // halfway point for noise in range [-1, 1]

        workers = new MarchingCubesWorker[numWorkers];
        for (int i = 0; i < workers.Length; ++i) {
            workers[i] = new MarchingCubesWorker(resolution, kernelMC, MarchingCubesCS, noiseMat);
        }
    }
    
    // Update is called once per frame
    void Update() {
        UpdateWorkers();
        countText.text = "Requests: " + queue.Count;
    }

    void OnPostRender() {   // can speed up async reading (if requested earlier in update can be ready by end of frame)
        UpdateWorkers();    
    }

    void UpdateWorkers() {
        for (int i = 0; i < workers.Length; ++i) {
            workers[i].Update();
            if (workers[i].free) {
                // get request
                ChunkRequest req = null;
                while (queue.Count > 0) {
                    req = queue.Dequeue();
                    if (req.force || req.tree.parent.ShouldSplit()) {
                        workers[i].Launch(req);
                        break;
                    } else {
                        req.tree.splitting = false;
                    }
                }
            }
        }
    }
   

    //public static void RequestChunk(Vector3 offset, float voxelSize, OnReceived callback) {
    //    //jobList.Add(new ChunkRequest(offset, voxelSize, callback));
    //}

    public static void Enqueue(Octree tree, OnReceived callback, bool force = false) {
        queue.Enqueue(new ChunkRequest(tree, callback, force), tree.GetSqrDistToCamFromCenter());
    }

    // clears chunk request list (tells each worker to get rid of next mesh if they are mid process)
    public static void ClearRequests() {
        queue.Clear();
        for (int i = 0; i < workers.Length; ++i) {
            workers[i].forgetNextResult = true;
        }
    }

    void OnDestroy() {
        for (int i = 0; i < workers.Length; ++i) {
            workers[i].Destroy();
        }
    }

}

public delegate void OnReceived(MeshData mesh);

class ChunkRequest : FastPriorityQueueNode {
    public Octree tree;
    public OnReceived callback;
    public bool force;  // happens no matter what (no last minute checks)
    public ChunkRequest(Octree tree, OnReceived callback, bool force) {
        this.tree = tree;
        this.callback = callback;
        this.force = force; 
    }
}

class MarchingCubesWorker {
    int resolution;
    int kernelMC;
    ComputeShader mccs;
    Material noiseMat;

    ComputeBuffer appendBuffer;
    ComputeBuffer argBuffer;

    RenderTexture density = null;

    float[] data;
    int[] count;

    public bool free = true;
    bool retrievedData = false;
    bool retrievedCount = false;
    bool needToFree = false; // only for editor rly incase closed before getting any requests (rare)

    ChunkRequest cur = null;

    public bool forgetNextResult = false;

    public MarchingCubesWorker(int resolution, int kernelMC, ComputeShader mccs, Material noiseMat) {
        this.resolution = resolution;
        this.kernelMC = kernelMC;
        this.mccs = mccs;
        this.noiseMat = noiseMat;

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
        forgetNextResult = false;
        retrievedData = false;
        retrievedCount = false;
        cur = cr;   // save current chunk request

        BlitNoise();

        DispatchMC();

        // this maybe can fail? should prob check for this case
        AsyncTextureReader.RequestBufferData(appendBuffer);
        AsyncTextureReader.RequestBufferData(argBuffer);
        needToFree = true;
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
            if (!forgetNextResult) {
                cur.callback(BuildMeshData());
            }

            free = true;
        }
    }

    public void Destroy() {
        appendBuffer.Release();
        argBuffer.Release();

        if (needToFree) {
            AsyncTextureReader.ReleaseTempResources(appendBuffer);
            AsyncTextureReader.ReleaseTempResources(argBuffer);
        }
    }

    void BlitNoise() {
        if (!density.IsCreated()) {    // texture can get uncreated in certain situations i think
            Debug.LogWarning("Something is wrong lol!");
            return;
        }
        if (!noiseMat.SetPass(0)) { // in case shader complation error
            return;
        }

        noiseMat.SetVector(ShaderProps.LocalOffset, cur.tree.worldPos);
        noiseMat.SetFloat(ShaderProps.Resolution, resolution);
        noiseMat.SetFloat(ShaderProps.Size, cur.tree.voxelSize);

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

    int[] defaultArgs = new int[] { 0, 1, 0, 0 };
    void DispatchMC() {
        // set compute shader references
        Graphics.ClearRandomWriteTargets(); // not sure if needed anymore
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
            verts[i] = (new Vector3(f0, f1, f2)) * resolution * cur.tree.voxelSize;
            norms[i] = new Vector3(f3, f4, f5);

            tris[i] = i;
        }

        return new MeshData(verts, norms, tris);

    }
}