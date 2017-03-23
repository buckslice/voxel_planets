using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMining : MonoBehaviour {

    public CelestialBody planet;
    public Transform visualizer;

    public LayerMask terrainLayer;

    public Material meshMat;
    public DrawBounds drawer;
    public Light flashLight;

    Camera cam;


    // Use this for initialization
    void Start() {
        drawer.enabled = false;
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

                Octree tree = planet.root.FindOctree(hit.point);

                if (tree != null && tree.IsMaxDepth()) {
                    tree.EditVoxels(hit.point, leftClick);
                }
            }

        } else {
            visualizer.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.F2)) {
            drawer.enabled = !drawer.enabled;
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            flashLight.enabled = !flashLight.enabled;
        }


        if (drawer.enabled) {
            Octree tree = planet.root.FindOctree(transform.position);
            if (tree != null) {
                drawer.SetBounds(tree.area);

                Graphics.DrawMesh(tree.GetMesh(), tree.obj.go.transform.position, Quaternion.identity, meshMat, 0);
            }
        }


    }


}
