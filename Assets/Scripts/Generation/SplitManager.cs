using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

public class SplitManager : MonoBehaviour {
    [Range(1,8)]
    public int maxConcurrentTasks = 2;

    // shared list between main and worker thread of octrees to split
    public static List<Octree> splitList = new List<Octree>();
    public static List<Task<SplitData>> taskList = new List<Task<SplitData>>();

    public static Stack<ChunkObject> freeObjects = new Stack<ChunkObject>();
    public static Stack<ColliderObject> freeColliders = new Stack<ColliderObject>();

    private static Transform tform;

    public Text splitCountText;

    // Use this for initialization
    void Awake() {
        tform = FindObjectOfType<SplitManager>().transform;
    }

    int totalResolved = 0;
    
    // Update is called once per frame
    void Update() {
        int taskLaunchesPerFrame = 1;
        // check if there are splits to do, havent launched too many tasks this frame, and theres less than 8 tasks going
        while(splitList.Count > 0 && taskLaunchesPerFrame > 0 && taskList.Count < 2) {
            // find octree closest to cam and split that
            int count = splitList.Count;
            int endIndex = count - 1;
            float closestDist = float.MaxValue;
            int closestIndex = endIndex;
            for(int i = 0; i < count; ++i) {
                float dist = splitList[i].GetSqrDistToCamFromCenter();
                if (dist < closestDist) {
                    closestIndex = i;
                    closestDist = dist;
                }
            }

            Octree toSplit = splitList[closestIndex];
            if (count > 1) {    // remove from list fast
                splitList[closestIndex] = splitList[endIndex];
                splitList.RemoveAt(endIndex);
            } else {
                splitList.Clear();
            }

            // one last check before queueing up task
            if (toSplit.ShouldSplit()) {
                taskList.Add(toSplit.SplitAsync());
                --taskLaunchesPerFrame;
            } else {    // otherwise just remove from list
                toSplit.splitting = false;
            }
            
        }

        // check and resolve tasks that are complete
        int resolutionsPerFrame = 1;
        for(int i = 0; i < taskList.Count;) {
            if (taskList[i].IsCompleted) {
                //Debug.Log("resolved: " + ++totalResolved);
                SplitData sd = taskList[i].Result;
                sd.tree.SplitResolve(sd.data);
                taskList.RemoveAt(i);
                if (--resolutionsPerFrame <= 0) {
                    break;
                }
            } else {
                ++i;
            }
        }

        splitCountText.text = "splits: " + splitList.Count;

    }

    public static void AddToSplitList(Octree node) {
        splitList.Add(node);
    }

    // todo reimplement these to generate closest first
    public int NearestToFarthest(Octree o1, Octree o2) {
        return o1.GetSqrDistToCamFromCenter().CompareTo(o2.GetSqrDistToCamFromCenter());
    }

    public int FurthestToNearest(Octree o1, Octree o2) {
        return o2.GetSqrDistToCamFromCenter().CompareTo(o1.GetSqrDistToCamFromCenter());
    }

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
        if(freeColliders.Count > 0) {
            ColliderObject free = freeColliders.Pop();
            free.go.SetActive(true);
            return free;
        }

        return new ColliderObject();
    }

    public static void ReturnObject(ChunkObject obj) {
        obj.mr.enabled = true;
        obj.ov.shouldDraw = false;
        obj.go.SetActive(false);
        obj.go.transform.parent = tform;

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
    public MaterialPropertyBlock mpb;

    public ChunkObject() {
        go = new GameObject("Chunk");
        mf = go.AddComponent<MeshFilter>();
        mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        ov = go.AddComponent<OctreeViewer>();
        mpb = new MaterialPropertyBlock();
    }

    public void SetTransparency(float t) {
        mpb.SetFloat("_Transparency", t);
        mr.SetPropertyBlock(mpb);
    }
}

public class ColliderObject {
    public GameObject go;
    public MeshCollider mc;

    public ColliderObject() {
        go = new GameObject("Collider");
        mc = go.AddComponent<MeshCollider>();
    }
}