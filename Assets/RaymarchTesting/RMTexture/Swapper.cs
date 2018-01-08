using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swapper : MonoBehaviour {

    public RaymarchGenericTexture raymarcher;
    public GameObject obj;

	// Use this for initialization
	void Start () {
        raymarcher.enabled = true;
        obj.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.O)) {
            raymarcher.enabled = !raymarcher.enabled;
            obj.SetActive(!obj.activeSelf);
        }
	}
}
