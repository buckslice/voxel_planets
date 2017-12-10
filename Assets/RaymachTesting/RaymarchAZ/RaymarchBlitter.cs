using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RaymarchBlitter : MonoBehaviour {

    public Material rayMarchMat;

	// Use this for initialization
	void Start () {
		
	}
	
    void OnPostRender() {
        //Camera.main.targetTexture = null;

        Graphics.Blit(null, null, rayMarchMat);
    }
}
