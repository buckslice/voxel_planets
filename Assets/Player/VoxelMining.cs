using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMining : MonoBehaviour {

    public CelestialBody planet;
    public Transform visualizer;

    public LayerMask terrainLayer;

    public Material meshMat;
    DrawBounds[] boundsDrawers;
    public Light flashLight;

    // if you want to manually specify a certain chunk to draw the wireframe of
    public ChunkObject forceDrawChunk = null;

    Camera cam;

    // Use this for initialization
    void Start() {
        boundsDrawers = FindObjectsOfType<DrawBounds>();
        for (int i = 0; i < boundsDrawers.Length; ++i) {
            boundsDrawers[i].enabled = false;
        }
        flashLight.enabled = false;

        cam = Camera.main;
    }

    float miningIntent = 0.0f;
    float miningSpeed = 200.0f;
    float miningSize = 2.0f;

    // Update is called once per frame
    RaycastHit hit;
    void Update() {
        // todo: dont let player be kinematic while doing this
        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);
        if ((leftClick || rightClick) && !(leftClick && rightClick)) {
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 50.0f, terrainLayer.value)) {
                // add some to mining in float value (so can be done smoothly over time)
                miningIntent += (leftClick ? miningSpeed : -miningSpeed) * Time.deltaTime;

                visualizer.gameObject.SetActive(true);
                visualizer.transform.position = hit.point;
                // once a whole integer is accrued then actually try to mine
                if (Mathf.Abs(miningIntent) >= 1.0f) {
                    Bounds b = new Bounds(hit.point, Vector3.one * miningSize * Octree.BASE_VOXEL_SIZE);
                    int mineAmount = (int)miningIntent;
                    miningIntent -= mineAmount;
                    planet.root.EditVoxels(b, mineAmount);
                }
            }
        } else {
            visualizer.gameObject.SetActive(false);
        }


        if (Input.GetKeyDown(KeyCode.U)) {
            flashLight.enabled = !flashLight.enabled;
        }

        if (Input.GetKeyDown(KeyCode.F2)) {
            for (int i = 0; i < boundsDrawers.Length; ++i) {
                boundsDrawers[i].enabled = !boundsDrawers[i].enabled;
            }
        }
        if (boundsDrawers[0].enabled) {
            if (forceDrawChunk == null) {
                Octree tree = null;
                if (planet && planet.root != null) {
                    tree = planet.root.FindOctree(transform.localPosition);
                    if (tree != null) {
                        // draw chunk boundaries
                        for (int i = 0; i < boundsDrawers.Length; ++i) {
                            boundsDrawers[i].SetBounds(tree.localArea);
                        }
                        // draw chunk mesh wireframe
                        Graphics.DrawMesh(tree.obj.mf.mesh, tree.obj.go.transform.position, Quaternion.identity, meshMat, 0);
                    }
                }
            } else {
                Graphics.DrawMesh(forceDrawChunk.mf.mesh, forceDrawChunk.go.transform.position, Quaternion.identity, meshMat, 0);
            }
        }


    }


}
