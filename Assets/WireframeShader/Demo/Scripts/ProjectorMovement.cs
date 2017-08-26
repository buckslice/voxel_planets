using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WFShaderDemo {
    public class ProjectorMovement : MonoBehaviour {
        public float speed;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            var t = Time.realtimeSinceStartup;

            var pos = Vector3.Lerp(new Vector3(7.7f, 2.53f, 2f), new Vector3(-6f, 2.53f, 2f), Mathf.Sin(t*speed)*0.5f+0.5f);

            transform.localPosition = pos;
        }
    }
}
