using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CelestialBody))]
public class GravityWell : MonoBehaviour {

    public float gravity = 9.81f; // should eventually base this off planet mass
    public LayerMask obeysGravity;
    private float gravityRadius;    // should be based off mass
    private float atmosphereRadius;

    private Collider[] colliders = new Collider[0];
    private Collider[] lastColliders;
    private Transform lastPos;	// needs to be transform so it is updated by floating origin
    private CelestialBody cb;

    private Transform myTransform;

    // Use this for initialization
    void Start() {
        myTransform = transform;
        cb = GetComponent<CelestialBody>();

        gravityRadius = cb.surfaceRadius * 3;
        atmosphereRadius = cb.atmosphereRadius;

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
        // copy last frames colliders so you can check if any left later
        lastColliders = new Collider[colliders.Length];
        System.Array.Copy(colliders, lastColliders, colliders.Length);

        Vector3 planetVelocity = (myTransform.position - lastPos.position) / Time.deltaTime;

        // add gravity to all rigidbodies in gravity radius (have to be tagged!)
        colliders = Physics.OverlapSphere(myTransform.position, gravityRadius, obeysGravity.value);
        foreach (Collider collider in colliders) {
            if (collider.GetComponent<Rigidbody>() && !collider.GetComponent<Rigidbody>().isKinematic) {
                Vector3 g = (myTransform.position - collider.transform.position).normalized * gravity;
                collider.GetComponent<Rigidbody>().AddForce(g * collider.GetComponent<Rigidbody>().mass);

                // this is stupid and should be rewritten eventually
                PlanetWalker player = collider.gameObject.GetComponent<PlanetWalker>();
                if (player != null) {
                    if (player.largestGravSource <= gravity) {
                        player.largestGravSource = gravity;
                        player.gravitySource = myTransform.position;
                    }
                }
            }
        }

        // parent all rigidbodies in atmosphere radius
        // and subtract planets velocity from them
        colliders = Physics.OverlapSphere(myTransform.position, atmosphereRadius, obeysGravity.value);
        foreach (Collider collider in colliders) {
            if (collider.GetComponent<Rigidbody>() && !collider.GetComponent<Rigidbody>().isKinematic) {
                if (collider.transform.parent == null) {
                    collider.transform.parent = myTransform;
                    collider.GetComponent<Rigidbody>().velocity -= planetVelocity;
                    //Debug.Log("entered atmosphere: " + Time.time);
                }
            }
        }

        // check for all rigidbodies that left the atmosphere
        // and add planets velocity to them
        foreach (Collider collider in lastColliders) {
            if (collider.GetComponent<Rigidbody>() && !collider.GetComponent<Rigidbody>().isKinematic) {
                if (!contains(collider, colliders)) {
                    collider.transform.parent = null;
                    collider.GetComponent<Rigidbody>().velocity += planetVelocity;
                    //Debug.Log("left atmosphere: " + Time.time);
                }
            }
        }

        lastPos.position = myTransform.position;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, gravityRadius);

    }
}

