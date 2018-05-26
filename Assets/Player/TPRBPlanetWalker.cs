using UnityEngine;
using System.Collections.Generic;

// Third Person RigidBody Controller
// usage steps:
// add rigidbody and collider
// add child gameobject called "Model"
// use model to make pivot point of main game object at its feet
[RequireComponent(typeof(Rigidbody))]
public class TPRBPlanetWalker : MonoBehaviour {

    public float walkSpeed = 7f;
    public float runSpeed = 14f;
    public float jumpSpeed = 10f;
    public bool canSprint = true;
    public bool canJump = true;
    [Range(0.0f, 1.0f)]
    [Tooltip("How much control player has while in air\n(0.0 being none and 1.0 being equal to ground control")]
    public float airControl = 0.25f;
    [Range(1.0f, 90.0f)]
    public float steepnessThreshold = 45.0f;
    [Range(1.0f, 10.0f)]
    public float mouseSensitivity = 5.0f;
    public LayerMask cameraCollisionLayer;
    public float camDistance = 10f; // r in spherecial coordinates
    public Vector3 gravitySource = Vector3.zero;
    public float gravityStrength = 10.0f;
    public bool turnTowardsGravity = true;  // TODO FIX THIS DOESNT WORK IN FLYMODE
    public Transform camPivot; // should be child empty gameobject at eye level for camera
    public Transform model;    // should be child gameobject that is parent of all visuals for player
    public SkinnedMeshRenderer skr;
    public Animator animator;
    public bool debugRendering = false;
    public CelestialBody planet;

    float curSpeed = 0f;
    const float turnRate = 0.2f;   // rate at which model lerps to transform direction

    bool grounded = false;
    bool lastGrounded = false;
    bool jumping = false;
    bool anyInput = false;
    bool flyMode = false;

    float timeSinceGrounded = 1.0f;
    float timeSinceHitJump = 1.0f;
    float jumpCooldown = 0.0f;
    const float comeToRestTime = 0.5f;
    float timeTillRest = comeToRestTime;
    float targCamDistance;
    bool firstPerson = false;

    public Rigidbody rigid { get; private set; }
    public Transform cam;
    Transform tform;

    // get the average of the last few hit normals to undo
    // the interpolation of physics raycasting and spherecasting
    const int normalCount = 5;
    List<Vector3> hitNormals = new List<Vector3>();

    void Start() {
        tform = transform;

        rigid = GetComponent<Rigidbody>();
        rigid.freezeRotation = true;
        rigid.useGravity = false;

        model = tform.Find("Model");
        skr = model.GetComponentInChildren<SkinnedMeshRenderer>();
        cam.parent = camPivot.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        targCamDistance = camDistance;

        // buffer the hit normal array
        for (int i = 0; i < normalCount; ++i) {
            hitNormals.Add(Vector3.up);
        }

    }

    // Update is called once per frame
    void Update() {
        // calculate 3rd person camera rotation around player
        Vector3 euler = camPivot.localRotation.eulerAngles;
        euler.x -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * 50.0f;
        if (euler.x > 180.0f) {
            euler.x -= 360.0f;
        }
        euler.x = Mathf.Clamp(euler.x, -85.0f, 85.0f);
        euler.y += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * 50.0f;
        camPivot.localRotation = Quaternion.Euler(euler);

        // calc and set camera following distance
        const float minDist = 1.5f;
        targCamDistance -= Input.GetAxis("Mouse ScrollWheel") * targCamDistance * 2.0f;
        targCamDistance = Mathf.Clamp(targCamDistance, minDist, 100.0f);
        firstPerson = Mathf.Approximately(targCamDistance, minDist);
        camDistance = Mathf.Lerp(camDistance, firstPerson ? 0.0f : targCamDistance, Time.deltaTime * 5.0f);
        cam.localPosition = new Vector3(0, 0, -camDistance);

        skr.shadowCastingMode = camDistance >= minDist ?
            UnityEngine.Rendering.ShadowCastingMode.TwoSided :
            UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        // collide camera with anything in camera collision layer
        if (!firstPerson) {
            RaycastHit hit;
            if (Physics.Raycast(camPivot.position, -cam.forward, out hit, camDistance + 1.0f, cameraCollisionLayer.value)) {
                cam.position = hit.point + cam.forward;
            }
            if (debugRendering) {
                Debug.DrawRay(camPivot.position, -cam.forward * camDistance, Color.cyan);
            }
        }

        // check time since hit jump
        timeSinceHitJump += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) && canJump) {
            timeSinceHitJump = 0.0f;
        }

        // get raw inputs
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        anyInput = Mathf.Abs(inputX) >= float.Epsilon || Mathf.Abs(inputY) >= float.Epsilon;
        if (anyInput) { // if any inputs
            anyInput = true;
            Vector3 gravDir = (gravitySource - tform.position).normalized;

            Vector3 xzCamForward = Vector3.Cross(gravDir, cam.right).normalized;
            Vector3 forward = (xzCamForward * inputY + cam.right * inputX).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forward, -gravDir);
            Quaternion camSave = camPivot.rotation;
            Quaternion rotSave = model.rotation;
            tform.rotation = targetRotation;

            // restore model and cam pivot rotations
            model.rotation = rotSave;    // this will be lerped later
            camPivot.rotation = camSave; // this wont be

            // figure out current speed
            if (grounded) {
                if (Input.GetKey(KeyCode.LeftShift) && canSprint) {
                    curSpeed = runSpeed;
                } else {
                    curSpeed = walkSpeed;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F1)) {
            flyMode = !flyMode;
            //rigid.isKinematic = flyMode;
        }

        Octree tree = null;
        if (planet && planet.root != null) {
            tree = planet.root.FindOctree(transform.position);
        }
        //if (tree == null) {   
        //    Debug.Log("null tree");
        //}
        //if (!tree.IsMaxDepth()) {
        //    Debug.Log("tree not max depth: " + tree.depth);
        //}

        rigid.isKinematic = flyMode || tree == null || !tree.IsMaxDepth();
        if (tree != null && tree.obj != null) {
            tree.obj.ov.color = Color.green;
        }

        if (flyMode) {
            Vector3 gravDir = (gravitySource - tform.position).normalized;
            Vector3 gravForward = Vector3.Cross(gravDir, tform.right).normalized;
            if (turnTowardsGravity) {
                tform.rotation = Quaternion.LookRotation(gravForward, -gravDir);
            }

            // todo: hit button to increase or decrease flight speed by log amount or something
            float flySpeed = 100.0f;
            if (Input.GetKey(KeyCode.LeftControl)) {
                flySpeed *= 100.0f;
            }
            if (anyInput) {
                transform.position += gravForward * flySpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.LeftShift)) {
                transform.position += gravDir * flySpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Space)) {
                transform.position -= gravDir * flySpeed * Time.deltaTime;
            }
        }


        if (model.gameObject.activeSelf) {

            // lerp model to follow main transform rotation 
            model.rotation = Quaternion.Lerp(model.rotation, tform.rotation, turnRate);

            if (animator) {
                // set animation speeds
                float animSpeed = rigid.velocity.magnitude;
                // if animSpeed gets too close to one controller starts freakin
                animSpeed = Mathf.Clamp(animSpeed, 0.1f, 10f);
                if (!anyInput) {
                    animSpeed = 0.1f;
                }
                animator.SetFloat("Speed", animSpeed);

                if (rigid.isKinematic) {
                    animator.SetFloat("AirTime", 0.0f);
                    animator.SetBool("Jumping", false);
                } else {
                    animator.SetFloat("AirTime", timeSinceGrounded);
                    Vector3 v = tform.InverseTransformDirection(rigid.velocity);
                    animator.SetBool("Jumping", v.y > 6f);
                }
            }
        }
    }

    void FixedUpdate() {
        if (rigid.isKinematic) {  // if kinematic nothing physics based will do anything
            return;
        }

        lastGrounded = grounded;
        Vector3 gravDir = (gravitySource - tform.position).normalized;

        // rotate body to stay upright if being affect by a gravity source
        if (turnTowardsGravity && gravityStrength > 0.0f) {
            float rotationRate = 0.0025f * gravityStrength;
            Vector3 gravForward = Vector3.Cross(gravDir, tform.right);
            Quaternion targetRot = Quaternion.LookRotation(gravForward, -gravDir);
            rigid.rotation = Quaternion.Lerp(rigid.rotation, targetRot, rotationRate);
            if (Quaternion.Angle(rigid.rotation, targetRot) < 0.01) { // so wont lerp forever
                rigid.rotation = targetRot;
            }
        }

        // spherecast downwards to see if player is grounded
        RaycastHit info;
        Vector3 castStart = tform.position + (-gravDir * 0.5f);
        grounded = Physics.SphereCast(castStart, 0.45f, gravDir, out info, 0.1f);

        // calculate time since grounded, jumping status, and steepness
        timeSinceGrounded += Time.deltaTime;
        if (grounded) {
            // add hit normal to array for velocity projection smoothing step
            hitNormals.Add(info.normal);
            while (hitNormals.Count > normalCount) {
                hitNormals.RemoveAt(0);
            }

            if (!lastGrounded) {
                jumping = false;
            }

            timeSinceGrounded = 0f;
            if (Vector3.Angle(-gravDir, info.normal) > steepnessThreshold) {
                if (debugRendering) {
                    Debug.DrawRay(info.point, info.normal, Color.red, 10.0f);
                }
                return; // too steep so return early
            }
        }

        // prevents sliding down hills (dont move this up above otherwise will happen when sliding down cliffs still)
        if (grounded && lastGrounded) {
            timeTillRest -= Time.deltaTime;
        }

        if (anyInput) { // calculate velocity change
            rigid.isKinematic = false;
            timeTillRest = comeToRestTime;

            // input is gathered in Update() so at this point player will be pointing in the direction they want to go
            // (theres no strafing currently)
            Vector3 velocityChange = tform.forward * (grounded ? 1.0f : airControl);
            rigid.AddForce(velocityChange, ForceMode.VelocityChange);

        }

        // get rigidbody velocity local to player transform
        Vector3 v = tform.InverseTransformDirection(rigid.velocity);

        jumpCooldown -= Time.deltaTime;
        // jump if pressed button and recently grounded
        if (timeSinceHitJump < 0.2f && timeSinceGrounded < 0.2f && jumpCooldown <= 0.0f) {
            jumping = true;
            rigid.isKinematic = false;
            timeTillRest = comeToRestTime;
            jumpCooldown = 1.0f;
            v.y = jumpSpeed;    // set current y velocity to jump
        }

        // if not trying to move then apply friction
        if (!anyInput) {
            // feels better than original planetwalker but much filthier feeling in code
            // todo make this frame independent (tho i guess it maybe is since in FixedUpdate... )
            float drag = grounded ? timeTillRest * (1.0f / comeToRestTime) : 0.95f;
            v = new Vector3(v.x * drag, v.y, v.z * drag);
            if (timeTillRest <= 0.0f) {
                timeTillRest = 0.0f;
                rigid.isKinematic = true;
            }

        }

        // clamp velocity values in the x and z directions
        // todo: change to how dtb handles velocity clamping
        v = Vector3.ClampMagnitude(new Vector3(v.x, 0f, v.z), curSpeed) + new Vector3(0f, v.y, 0f);

        // transform back and reapply to rigidbody velocity
        rigid.velocity = tform.TransformDirection(v);

        // help stick to ground by projecting movement onto hit plane
        if (!jumping && lastGrounded) {
            // take the average of previous hit normals
            Vector3 avgn = Vector3.zero;
            for (int i = 0; i < normalCount - 1; ++i) {
                avgn += hitNormals[i];
            }
            avgn /= normalCount - 1;

            rigid.velocity = Vector3.ProjectOnPlane(rigid.velocity, avgn);

            if (debugRendering) {
                Debug.DrawRay(info.point, avgn, Color.green, 10.0f);
            }
        }

    }

}
