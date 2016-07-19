using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

public class SplitManager : MonoBehaviour {
    // shared list between main and worker thread of octrees to split
    public static List<Octree> splitList = new List<Octree>();
    // shared list between main and worker thread of split results
    public static List<SplitData> splitResults = new List<SplitData>();

    // where pending splits are waiting to be added to splitList
    public static List<Octree> mainSplitList = new List<Octree>();
    // list of splitdatas for main thread to process
    public static List<SplitData> mainSplitResults = new List<SplitData>();

    public static Stack<ChunkObject> freeObjects = new Stack<ChunkObject>();

    private static Transform t;

    private EventWaitHandle newItemEvent;

    // Use this for initialization
    void Awake() {
        t = FindObjectOfType<SplitManager>().transform;

        newItemEvent = new AutoResetEvent(false);

        //Thread workerThread = new Thread(ThreadProcedure);
        //workerThread.IsBackground = true;
        //workerThread.Start();
    }

    // Update is called once per frame
    void Update() {
        // gather split results from worker thread
        if (splitResults.Count > 0) {
            lock (splitResults) {
                for (int i = 0; i < splitResults.Count; ++i) {
                    mainSplitResults.Add(splitResults[i]);
                }
                splitResults.Clear();
            }
        }

        // process split results
        int splitsPerFrame = 1;
        while (mainSplitResults.Count > 0 && splitsPerFrame-- > 0) {
            int endIndex = mainSplitResults.Count - 1;
            SplitData sd = mainSplitResults[endIndex];
            mainSplitResults.RemoveAt(endIndex);

            sd.tree.SplitResolve(sd.data);
            //UnityEngine.Debug.Log(Time.realtimeSinceStartup);
        }

        //if (Monitor.TryEnter(splitList)) {
        //    for (int i = 0; i < mainSplitList.Count; ++i) {
        //        splitList.Add(mainSplitList[i]);
        //    }
        //    splitList.Sort(NearestToFarthest);
        //    Monitor.Exit(splitList);
        //    if (mainSplitList.Count > 0) {
        //        newItemEvent.Set();
        //    }
        //    mainSplitList.Clear();
        //}

        if (mainSplitList.Count > 0) {
            //mainSplitList.Sort(NearestToFarthest);
            for (int i = 0; i < mainSplitList.Count; ++i) {
                ThreadPool.QueueUserWorkItem(ThreadPoolProcedure, mainSplitList[i]);
            }
            mainSplitList.Clear();
        }

    }

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

    public static void ReturnObject(ChunkObject obj) {
        obj.mr.enabled = true;
        obj.ov.shouldDraw = true;
        obj.go.SetActive(false);
        obj.go.transform.parent = t;
        freeObjects.Push(obj);
    }

    public static void AddToSplitList(Octree node) {
        mainSplitList.Add(node);
    }

    public void ThreadPoolProcedure(System.Object o) {
        Octree tree = (Octree)o;
        SplitData data = tree.Split();
        lock (splitResults) {
            splitResults.Add(data);
        }
    }

    public void ThreadProcedure() {
        Stopwatch watch = new Stopwatch();
        Stopwatch startTime = new Stopwatch();
        startTime.Start();

        List<Octree> threadSplitList = new List<Octree>();
        List<SplitData> threadSplitResults = new List<SplitData>();
        int splitOp = 0;
        while (newItemEvent.WaitOne()) {
            lock (splitList) {
                for (int i = 0; i < splitList.Count; ++i) {
                    threadSplitList.Add(splitList[i]);
                }
                splitList.Clear();
            }

            while (threadSplitList.Count > 0) {
                int lastIndex = threadSplitList.Count - 1;
                Octree o = threadSplitList[lastIndex];
                threadSplitList.RemoveAt(lastIndex);

                watch.Start();
                SplitData data = o.Split();
                watch.Stop();
                UnityEngine.Debug.Log(startTime.Elapsed.TotalSeconds + " sec, " + watch.ElapsedMilliseconds + ", " + ++splitOp);
                watch.Reset();

                threadSplitResults.Add(data);
            }

            lock (splitResults) {
                for (int i = 0; i < threadSplitResults.Count; ++i) {
                    splitResults.Add(threadSplitResults[i]);
                }
            }
            threadSplitResults.Clear();
        }
    }
}

public class ChunkObject {
    public GameObject go;
    public MeshRenderer mr;
    public MeshFilter mf;
    public OctreeViewer ov;

    public ChunkObject() {
        go = new GameObject();
        mf = go.AddComponent<MeshFilter>();
        mr = go.AddComponent<MeshRenderer>();
        ov = go.AddComponent<OctreeViewer>();
    }
}
