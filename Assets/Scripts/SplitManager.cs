using UnityEngine;
using System.Collections.Generic;

public class SplitManager : MonoBehaviour {

    public static Queue<SplitData> splitResults = new Queue<SplitData>();

    public static Stack<ChunkObject> freeObjects = new Stack<ChunkObject>();

    private static Transform t;

    // Use this for initialization
    void Awake() {
        t = FindObjectOfType<SplitManager>().transform;
	}
	
	// Update is called once per frame
	void Update () {
        int splitsPerFrame = 1;
        while (splitResults.Count > 0 && splitsPerFrame-- > 0) {
            SplitData sd = splitResults.Dequeue();
            sd.tree.SplitResolve(sd.data);
        }
    }

    // returns a gameobject with proper components
    public static ChunkObject GetObject() {
        if(freeObjects.Count > 0) {
            ChunkObject free = freeObjects.Pop();
            free.go.SetActive(true);
            return free;
        }

        return new ChunkObject();
    }

    public static void ReturnObject(ChunkObject obj) {
        obj.mr.enabled = true;
        obj.go.transform.parent = t;
        obj.go.SetActive(false);
        freeObjects.Push(obj);
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
