using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlanetWalker : MonoBehaviour {

    private float moveSpeed;
    private float jumpSpeed;
    private float groundControl = 1.0f;
    private float airControl = 0.2f;
    private bool shouldJump;

    private float mouseSensitivity = 300f;
    private float verticalLookLimit = 90.0f;
    private float verticalLook = 0f;

    private Transform camTransform;

    private bool turnsTowardGravity = true;
    public Vector3 gravitySource = new Vector3();
    public float largestGravSource = 0f;

    private Transform myTransform;
    private Rigidbody myRigidBody;

    // gonna need something like this eventually
    // private Vector3D galaxyCoordinate;

    private bool freeFlightMode = true;
    private float inputRight;
    private float inputForward;
    private float currentFlightSpeed;
    private float targetFlightSpeed = 2000f;
    private float curHoriz;
    private float curVerti;

    private Text text;
    private bool textOn = true;
    private string instructions =
        "WASD  : move\n" +
        "Space : ascend / jump\n" +
        "Shift : descend\n" +
        "Wheel : change flight speed\n" +
        "F : toggle terrain update\n" +
        "Q : toggle wireframe\n" +
        "R : regererate new terrain\n" +
        "T : regenerate terrain\n" +
        "F1: toggle flymode\n" +
        "F2: toggle instructions";

    void Start() {
        //text = GameObject.Find("Text").GetComponent<Text>();
        //text.text = instructions;

        myTransform = transform;

        myRigidBody = GetComponent<Rigidbody>();
        myRigidBody.freezeRotation = true;
        myRigidBody.useGravity = false;
        myRigidBody.isKinematic = freeFlightMode;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameObject playerObject = GameObject.Find("Player");
        camTransform = Camera.main.transform;
        camTransform.parent = playerObject.transform;
        camTransform.localPosition = playerObject.transform.Find("CameraOffset").localPosition;


    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.F2)) {
            textOn = !textOn;
        }
        //if (textOn) {
        //    text.text = freeFlightMode ? "Flight Speed : " + targetFlightSpeed.ToString("F1") + "\n" : "";
        //    text.text += instructions;
        //} else {
        //    text.text = "";
        //}

        inputRight = Input.GetAxis("Horizontal");
        inputForward = Input.GetAxis("Vertical");
        float inputModifyFactor = (inputRight != 0.0f && inputForward != 0.0f) ? .7071f : 1.0f;
        inputRight *= inputModifyFactor;
        inputForward *= inputModifyFactor;
        float inputUp = Input.GetButton("Jump") ? 1.0f : 0.0f;

        if (Input.GetKeyDown(KeyCode.F1)) {
            freeFlightMode = !freeFlightMode;
            myRigidBody.isKinematic = freeFlightMode;
            currentFlightSpeed = targetFlightSpeed = 50f;
        }

        // Rotate around Y so you can steer with mouse
        // Also adjusts pitch of camera here
        float horizontalLook = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        verticalLook -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        verticalLook = Mathf.Clamp(verticalLook, -verticalLookLimit, verticalLookLimit);

        //curHoriz = Mathf.Lerp(curHoriz, horizontalLook, Time.deltaTime * 10f);
        //curVerti = Mathf.Lerp(curVerti, verticalLook, Time.deltaTime * 10f);
        curHoriz = horizontalLook;
        curVerti = verticalLook;

        camTransform.localRotation = Quaternion.Euler(curVerti, 0, 0);

        myTransform.Rotate(0, curHoriz, 0);

        if (freeFlightMode) {
            Vector3 gravityDir = -(myTransform.position - gravitySource).normalized;
            Vector3 gravityForward = Vector3.Cross(gravityDir, myTransform.right);
            myTransform.rotation = Quaternion.LookRotation(gravityForward, -gravityDir);

            float increment = Mathf.Min(targetFlightSpeed / 5f, 200f) * -Input.GetAxis("Mouse ScrollWheel");
            targetFlightSpeed += increment;
            if (targetFlightSpeed < 50f) {
                targetFlightSpeed = 50f;
            } else if (targetFlightSpeed > 10000f) {
                targetFlightSpeed = 10000f;
            }

            //Debug.Log(targetFlightSpeed);

            currentFlightSpeed = Mathf.Lerp(currentFlightSpeed, targetFlightSpeed, Time.deltaTime * 3);
            transform.position += camTransform.forward * currentFlightSpeed * inputForward * Time.deltaTime;
            transform.position += camTransform.right * currentFlightSpeed * inputRight * Time.deltaTime;
            transform.position += myTransform.up * currentFlightSpeed * inputUp * Time.deltaTime;

        } else {
            if (Input.GetKey(KeyCode.LeftShift)) {
                moveSpeed = Mathf.Lerp(moveSpeed, 24, Time.deltaTime * 5f);
                jumpSpeed = 24;
            } else {
                moveSpeed = Mathf.Lerp(moveSpeed, 8, Time.deltaTime * 5f);
                jumpSpeed = 10;
            }

            shouldJump = shouldJump || Input.GetKeyDown(KeyCode.Space);
        }

    }

    void FixedUpdate() {
        if (freeFlightMode) {
            return;
        }

        // rotate the body to stay upright if a source of gravity is affecting you
        if (turnsTowardGravity && largestGravSource != 0f) {
            Vector3 gravityDir = -(myTransform.position - gravitySource).normalized;

            float rotationRate = 0.0025f * largestGravSource;

            Vector3 gravityForward = Vector3.Cross(gravityDir, myTransform.right);
            Quaternion targetRotation = Quaternion.LookRotation(gravityForward, -gravityDir);
            myRigidBody.rotation = Quaternion.Lerp(myRigidBody.rotation, targetRotation, rotationRate);
            if (Quaternion.Angle(myRigidBody.rotation, targetRotation) < .01f) {
                myRigidBody.rotation = targetRotation;
            }
        }
        largestGravSource = 0f;

        bool grounded = false;
        if (myTransform.parent != null) {
            // transform.position still returns world position
            // when a object is childed the editor shows its localPosition instead
            // just learned this lol
            Vector3 gravityDir = -(myTransform.position - myTransform.parent.position).normalized;

            Vector3 raycastStart = myTransform.position;
            raycastStart += -gravityDir.normalized * 0.6f;
            Vector3 raycastEnd = (gravityDir.normalized * 1f);

            //CapsuleCollider cc = GetComponent<CapsuleCollider>();
            RaycastHit hit = new RaycastHit();
            grounded = Physics.SphereCast(new Ray(raycastStart, raycastEnd), 0.5f, out hit, 0.7f);

            // get slope (0 is completely flat, 1 is vertical, .5 if 45 degree slope)
            //Debug.Log(Vector3.ProjectOnPlane(hit.normal,transform.up).magnitude);

        }

        Vector3 forward = (myTransform.parent != null) ? myTransform.forward : camTransform.forward;

        // Add velocity change for movement on the local horizontal plane
        Vector3 targetVelocity = (forward * inputForward + myTransform.right * inputRight) * moveSpeed;
        Vector3 localVelocity = myTransform.InverseTransformDirection(myRigidBody.velocity);
        Vector3 velocityChange = myTransform.InverseTransformDirection(targetVelocity);

        if (myTransform.parent != null) {         // you have a parent when you are in atmosphere
            velocityChange -= localVelocity;	// this is basically air resistance/friction
        } else {
            velocityChange *= Time.deltaTime;	// reduce this when in space cuz you never slow down
        }

        // The velocity change is clamped to the control velocity
        // The vertical component is either removed or set to result in the absolute jump velocity
        if (myTransform.parent != null) {
            velocityChange = Vector3.ClampMagnitude(velocityChange, grounded ? groundControl : airControl);
            velocityChange.y = shouldJump && grounded ? -localVelocity.y + jumpSpeed : 0;
        }
        velocityChange = myTransform.TransformDirection(velocityChange);
        myRigidBody.AddForce(velocityChange, ForceMode.VelocityChange);

        shouldJump = false;

    }

}