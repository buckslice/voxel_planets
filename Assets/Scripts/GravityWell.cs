using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CelestialBody))]
public class GravityWell : MonoBehaviour {
    public float gravity = 9.81f; // should eventually base this off planet mass
    public LayerMask obeysGravity;
    public bool drawGizmo = true;
    public Mesh sphereMesh;

    private const int maxColliders = 64;
    private int colliderCount = 0;
    private Collider[] colliders = new Collider[maxColliders];
    private List<Collider> lastColliders = new List<Collider>();
    private Transform lastPos;	// needs to be transform so it is updated by floating origin
    private CelestialBody cb;

    private Transform tform;

    // Use this for initialization
    void Awake() {
        tform = transform;
        cb = GetComponent<CelestialBody>();

        GameObject go = new GameObject(gameObject.name + " Last Position");
        lastPos = go.transform;
        lastPos.position = transform.position;
        // makes them parent of global game object so they get out of heirarchy
        // can't make them child of this transform though because otherwise they
        // wouldn't stay the same when parent moves. pretty annoying. need transforms
        // though otherwise floating origin script won't catch them each frame
        // -checking back on this im probably just stupid, but this works fine
        lastPos.parent = GameObject.Find("_Global").transform;
    }

    void FixedUpdate() {
        Vector3 planetVelocity = (tform.position - lastPos.position) / Time.deltaTime;

        // add gravity to all rigidbodies in gravity radius (have to be in ObeysGravity layer)
        colliderCount = Physics.OverlapSphereNonAlloc(tform.position, cb.gravityRadius, colliders, obeysGravity.value);
        for (int i = 0; i < colliderCount; ++i) {
            Collider c = colliders[i];
            Rigidbody rb = c.GetComponent<Rigidbody>();

            if (rb && !rb.isKinematic) {
                Vector3 g = (tform.position - c.transform.position).normalized * gravity;
                rb.AddForce(g * rb.mass);

                if (c.CompareTag(Tags.Player)) {
                    TPRBPlanetWalker player = c.gameObject.GetComponent<TPRBPlanetWalker>();
                    if (player && player.gravityStrength <= gravity) {
                        player.gravityStrength = gravity;
                        player.gravitySource = tform.position;
                    }
                }
            }
        }

        // parent all rigidbodies in atmosphere radius
        // and subtract planets velocity from them
        colliderCount = Physics.OverlapSphereNonAlloc(tform.position, cb.atmosphereRadius, colliders, obeysGravity.value);
        for (int i = 0; i < colliderCount; ++i) {
            Rigidbody rb = colliders[i].GetComponent<Rigidbody>();
            if (rb && !rb.isKinematic) {
                if (rb.transform.parent == null) {
                    rb.transform.parent = tform;
                    rb.velocity -= planetVelocity;
                    //Debug.Log("entered atmosphere: " + Time.time);
                }
            }
        }

        // check for all rigidbodies that left the atmosphere
        // and add planets velocity to them
        for (int i = 0; i < lastColliders.Count; ++i) {
            Collider c = lastColliders[i];
            Rigidbody rb = c.GetComponent<Rigidbody>();
            if (rb && !rb.isKinematic && !contains(c, colliders)) { // should do something better once a lot of objects
                c.transform.parent = null;
                rb.velocity += planetVelocity;
                //Debug.Log("left atmosphere: " + Time.time);
            }
        }

        // save last frames colliders so you can check if any left later
        lastColliders.Clear();
        for (int i = 0; i < colliderCount; ++i) {
            lastColliders.Add(colliders[i]);
        }
        // save last position to calculate velocity deltas (i think... havnt done this code in while)
        lastPos.position = tform.position;
    }

    private bool contains(Collider collider, Collider[] colliders) {
        for (int i = 0; i < colliders.Length; i++) {
            if (collider == colliders[i]) {
                return true;
            }
        }
        return false;
    }

    // draws sphere of influence
    void OnDrawGizmos() {
        if (drawGizmo && cb) {
            Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.position, cb.gravityRadius);
            Gizmos.DrawWireMesh(sphereMesh, transform.position, Quaternion.identity, Vector3.one * cb.gravityRadius);
        }
    }
}

