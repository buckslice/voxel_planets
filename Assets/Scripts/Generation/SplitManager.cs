using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

// refactor to be like singleton like marchingcubesdispatcher?
// doesnt really matter actually i dunno, this ways kinda simpler
public class SplitManager : MonoBehaviour {

    // shared list between main and worker thread of octrees to split
    static List<Octree> splitList = new List<Octree>();
    static List<Task<SplitData>> taskList = new List<Task<SplitData>>();

    static Stack<ChunkObject> freeObjects = new Stack<ChunkObject>();
    static Stack<ColliderObject> freeColliders = new Stack<ColliderObject>();

    static Transform tform;

    public Text splitCountText;

    const int taskLaunchesPerFrame = 1;
    const int maxConcurrentTasks = 4;

    // Use this for initialization
    void Awake() {
        tform = transform;

        //maxConcurrentTasks = Mathf.Max(1, Environment.ProcessorCount / 2);
        //Debug.Log("maxConcurrentTasks set: " + maxConcurrentTasks);

    }

    //// Update is called once per frame
    //void Update() {
    //    int newTasks = 0;
    //    while (splitList.Count > 0   // while there are things to split
    //       && newTasks < taskLaunchesPerFrame  // and havent launched too many tasks this frame
    //       && taskList.Count < maxConcurrentTasks) { // and there are less tasks going than max

    //        // find lowest depth of trees on list
    //        int count = splitList.Count;
    //        int minDepth = Octree.MAX_DEPTH;
    //        for (int i = 0; i < count; ++i) {
    //            minDepth = Mathf.Min(minDepth, splitList[i].depth);
    //        }

    //        int closest = 0;
    //        float closestDist = float.MaxValue;
    //        for (int i = 0; i < count; ++i) {
    //            if (splitList[i].depth > minDepth) {
    //                continue;
    //            }

    //            float dist = splitList[i].GetSqrDistToCamFromCenter();
    //            if (dist < closestDist) {
    //                closest = i;
    //                closestDist = dist;
    //            }
    //        }

    //        Octree toSplit = splitList[closest];
    //        if (count > 1) {    // remove from list fast
    //            splitList[closest] = splitList[count - 1];
    //            splitList.RemoveAt(count - 1);
    //        } else {
    //            splitList.Clear();
    //        }

    //        // one last check before queueing up task
    //        if (toSplit.ShouldSplit()) {
    //            taskList.Add(toSplit.SplitAsync());
    //            newTasks++;
    //        } else {    // otherwise just remove from list
    //            toSplit.SetSplitting(false);
    //        }

    //    }

    //    // check and resolve tasks that are complete
    //    int resolutionsPerFrame = 1;
    //    for (int i = 0; i < taskList.Count;) {
    //        if (taskList[i].IsCompleted) {
    //            //Debug.Log("resolved: " + ++totalResolved);
    //            SplitData sd = taskList[i].Result;
    //            sd.tree.SplitResolve(sd.data);
    //            taskList.RemoveAt(i);
    //            if (--resolutionsPerFrame <= 0) {
    //                break;
    //            }
    //        } else {
    //            ++i;
    //        }
    //    }

    //    //splitCountText.text = "splits: " + splitList.Count;
    //    //if(splitList.Count == 0) {
    //    //    Debug.Log(Time.time);
    //    //}

    //}

    //public static void AddToSplitList(Octree node) {
    //    splitList.Add(node);
    //}

    //// todo reimplement these to generate closest first
    //public int NearestToFarthest(Octree o1, Octree o2) {
    //    return o1.GetSqrDistToCamFromCenter().CompareTo(o2.GetSqrDistToCamFromCenter());
    //}

    //public int FurthestToNearest(Octree o1, Octree o2) {
    //    return o2.GetSqrDistToCamFromCenter().CompareTo(o1.GetSqrDistToCamFromCenter());
    //}

    // returns a gameobject with proper components
    public static ChunkObject GetObject() {
        if (freeObjects.Count > 0) {
            ChunkObject free = freeObjects.Pop();
            free.go.SetActive(true);
            return free;
        }

        return new ChunkObject();
    }

    public static ColliderObject GetCollider() {
        if (freeColliders.Count > 0) {
            ColliderObject free = freeColliders.Pop();
            free.go.SetActive(true);
            return free;
        }

        return new ColliderObject();
    }

    public static void ReturnObject(ChunkObject obj) {
        obj.Reset(tform);
        freeObjects.Push(obj);
    }

    public static void ReturnCollider(ColliderObject obj) {
        obj.go.SetActive(false);
        obj.go.transform.parent = tform;
        obj.mc.sharedMesh = null;

        freeColliders.Push(obj);
    }
}

public class ChunkObject {
    public GameObject go;
    public MeshRenderer mr;
    public MeshFilter mf;
    public OctreeViewer ov;
    public static MaterialPropertyBlock mpb = null;
    float lastTrans = -1.0f;

    public ChunkObject() {
        go = new GameObject("Chunk");
        mf = go.AddComponent<MeshFilter>();
        mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        ov = go.AddComponent<OctreeViewer>();
        if (mpb == null) {
            mpb = new MaterialPropertyBlock();
        }
    }

    // resets components back to default values
    public void Reset(Transform parent) {
        mr.enabled = true;
        ov.shouldDraw = false;
        go.SetActive(false);
        go.transform.parent = parent;
        lastTrans = -1.0f;
    }

    public void SetTransparency(float t) {
        Debug.Assert(t >= 0.0f && t <= 1.0f, "bad transparency value: " + t);
        if (t == lastTrans) {
            return; // i have a feeling setting a property block has a decent cost (so only do when changes)
        }
        mpb.SetFloat(ShaderProps.Transparency, t);
        UpdatePropBlock();
        lastTrans = t;
    }

    public void UpdatePropBlock() { // not sure if this is a good way to do this but yolo
        mr.SetPropertyBlock(mpb);
    }
}

public class ColliderObject {
    public GameObject go;
    public MeshCollider mc;

    public ColliderObject() {
        go = new GameObject("Collider");
        go.layer = Layers.Terrain;
        mc = go.AddComponent<MeshCollider>();
    }
}