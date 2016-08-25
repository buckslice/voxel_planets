using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class SplitManager : MonoBehaviour {
    // shared list between main and worker thread of octrees to split
    public static List<Octree> splitList = new List<Octree>();
    public static List<Task<SplitData>> resolves = new List<Task<SplitData>>();

    public static Stack<ChunkObject> freeObjects = new Stack<ChunkObject>();
    public static Stack<ColliderObject> freeColliders = new Stack<ColliderObject>();

    private static Transform tform;

    // Use this for initialization
    void Awake() {
        tform = FindObjectOfType<SplitManager>().transform;
    }

    int totalResolved = 0;
    
    // Update is called once per frame
    void Update() {
        int newTasksPerFrame = 1;
        while(splitList.Count > 0 && newTasksPerFrame > 0 && resolves.Count < 8) {
            // find octree closest to cam and split that
            int count = splitList.Count;
            int endIndex = count - 1;
            float closestDist = float.MaxValue;
            int closestIndex = endIndex;
            for(int i = 0; i < count; ++i) {
                float dist = splitList[i].GetSqrDistToCam();
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
                resolves.Add(toSplit.SplitAsync());
                --newTasksPerFrame;
            } else {    // otherwise just remove from list
                toSplit.splitting = false;
            }
            
        }

        // check and resolve tasks that are complete
        int resolutionsPerFrame = 1;
        for(int i = 0; i < resolves.Count;) {
            if (resolves[i].IsCompleted) {
                //Debug.Log("resolved: " + ++totalResolved);
                SplitData sd = resolves[i].Result;
                sd.tree.SplitResolve(sd.data);
                resolves.RemoveAt(i);
                if (--resolutionsPerFrame <= 0) {
                    break;
                }
            } else {
                ++i;
            }
        }
    }

    public static void AddToSplitList(Octree node) {
        splitList.Add(node);
    }

    // todo reimplement these to generate closest first
    public int NearestToFarthest(Octree o1, Octree o2) {
        return o1.GetSqrDistToCam().CompareTo(o2.GetSqrDistToCam());
    }

    public int FurthestToNearest(Octree o1, Octree o2) {
        return o2.GetSqrDistToCam().CompareTo(o1.GetSqrDistToCam());
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

    public ChunkObject() {
        go = new GameObject("Chunk");
        mf = go.AddComponent<MeshFilter>();
        mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        ov = go.AddComponent<OctreeViewer>();
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