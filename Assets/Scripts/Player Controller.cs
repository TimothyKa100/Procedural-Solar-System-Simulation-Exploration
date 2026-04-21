using UnityEngine;

public class PlayerController : rbBody
{
    public float maxAcceleration;
    public float walkSpeed = 0.8f;
    public float runSpeed = 1.4f;
    public float angSpeed = 1.0f;

    public float maxVel = 25f;

    public Vector2 pitchMinMax = new Vector2(-40, 85);
    public Vector2 yawMinMax = new Vector2(-80, 80);

    float camYawVal;
    float yawVal;
    float pitchVal;

    float smoothTime = 0.1f;

    Vector3 targetVelocity;
    Vector3 smoothVelocity;
    Vector3 smoothVRef;

    Quaternion camTargetAng;
    Quaternion yaw;
    Quaternion camSmoothedAng;
    Quaternion originalCamAng;

    Camera cam;
    public Camera mainCam;

    public Canvas canvas;

    public GameObject planet;

    Spaceship spaceship;

    Vector3 gravityUp;

    public static event System.Action OnSpaceship;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        rb.interpolation = RigidbodyInterpolation.Interpolate; // More accurate collision detection
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // More accurate collision detection
        rb.useGravity = false;
        rb.isKinematic = false;
        base.Awake();
    }
    
    private void Start()
    {
        if(Time.time<10f)
        gameObject.SetActive(false); // (Only) in the beginning of the game it should be inactive, as Start() is called everytime the object is setActive
        yaw = transform.rotation;
        spaceship = FindAnyObjectByType<Spaceship>();
        originalCamAng = camTargetAng = cam.transform.rotation;
    }

    private void Update()
    {
        LinearMove();
        Rotate();
        gravity();
        switchToSpaceship();
    }

    void LinearMove()
    {
        Vector3 inputRight = Input.GetAxisRaw("Horizontal") * cam.transform.right;
        Vector3 inputForward = Input.GetAxisRaw("Vertical") * cam.transform.forward;
        
        // It must be noted that the direction of the player cubioud doesn't matter for any calculation but rather is the angle of the camera that dictates its movements
        
        Vector3 input = inputRight + inputForward;
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        targetVelocity = (input.normalized) * currentSpeed;
        
        // Smooth the velocity of the player to avoid jerky movements
        smoothVelocity = Vector3.SmoothDamp(smoothVelocity, targetVelocity, ref smoothVRef, smoothTime);
        
        // Avoid the player from moving too fast
        if (rb.linearVelocity.magnitude < maxVel)
        {
            rb.AddForce(smoothVelocity, ForceMode.VelocityChange);
        }
    }

    void Rotate()
    {
        float camYawInput = 0;
        camYawInput = Input.GetAxisRaw("Mouse X");
        camYawVal += camYawInput * angSpeed;
        pitchVal = Mathf.Clamp(pitchVal - Input.GetAxisRaw("Mouse Y") * angSpeed, pitchMinMax.x, pitchMinMax.y); // constraint for up/down rotation

        Quaternion camYaw = Quaternion.AngleAxis(camYawVal, transform.up); // rotate about the axis
        Quaternion pitch = Quaternion.AngleAxis(pitchVal, transform.right);

        camTargetAng = camYaw * pitch * rb.rotation;
        camSmoothedAng = Quaternion.Slerp(cam.transform.rotation, camTargetAng, Time.deltaTime * angSpeed); // smoothing applied, slerp splits the rotation into several frames over the time (Time.deltaTime * angSpeed)
        cam.transform.rotation = camSmoothedAng;
    }

    void gravity()
    {
        gravityUp = (rb.position - planet.transform.position).normalized;
        transform.up = gravityUp;
        //Debug.DrawLine(Vector3.zero, gravityUp*9999f, Color.red);
        //Debug.DrawLine(Vector3.zero, transform.up * 9999f, Color.blue);
        if (Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(gravityUp * 2f, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(-gravityUp * planet.GetComponent<CelestialBody>().mass, ForceMode.Acceleration);
        }
    }

    void switchToSpaceship()
    {
        if ((spaceship.rb.position - rb.position).magnitude < 10)
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                UniversalGravitation.trackBody = spaceship; // So floating origin system tracks the right object
                Quadtree.viewer = spaceship.transform; // regen and splitting tracks the right object
                spaceship.enabled = true;
                canvas.gameObject.SetActive(true); // This is the minimap
                mainCam.gameObject.SetActive(true); 
                if (OnSpaceship != null)
                {
                    OnSpaceship(); // to allow the boost of velocity so the spaceship could fly away from the planet
                }
                gameObject.SetActive(false); // player cam is parented to player so it also deactivates itself
            }
        }
    }
}
