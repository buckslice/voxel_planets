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

    Camera cam;

    // Use this for initialization
    void Start() {
        boundsDrawers = FindObjectsOfType<DrawBounds>();
        for(int i = 0; i < boundsDrawers.Length; ++i) {
            boundsDrawers[i].enabled = false;
        }
        flashLight.enabled = false;

        cam = Camera.main;
    }

    // Update is called once per frame
    void Update() {

        // dont let player be kinematic while doing this
        // also when at edge need to edit voxels of neighbor as well

        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);
        if ((leftClick || rightClick) && !(leftClick && rightClick)) {

            RaycastHit hit;
            if (Physics.Raycast(transform.position + transform.up * 3.0f, cam.transform.forward, out hit, 50.0f, terrainLayer.value)) {
                visualizer.gameObject.SetActive(true);
                visualizer.transform.position = hit.point;

                //Octree tree = planet.root.FindOctree(hit.point);

                //if (tree != null && tree.IsMaxDepth()) {
                //    tree.EditVoxels(hit.point, leftClick);
                //}

                Bounds b = new Bounds(hit.point, Vector3.one * 2.0f * Octree.BASE_VOXEL_SIZE);
                planet.root.EditVoxels(b, leftClick ? 2.0f : -2.0f);
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
            Octree tree = planet.root.FindOctree(transform.position);
            if (tree != null) {
                // draw chunk boundaries
                for (int i = 0; i < boundsDrawers.Length; ++i) {
                    boundsDrawers[i].SetBounds(tree.area);
                }
                // draw chunk mesh wireframe
                Graphics.DrawMesh(tree.GetMesh(), tree.obj.go.transform.position, Quaternion.identity, meshMat, 0);
            }
        }


    }


}
