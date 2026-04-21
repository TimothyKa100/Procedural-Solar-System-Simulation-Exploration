using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class Spaceship : rbBody
{
    KeyCode forward = KeyCode.W;
    KeyCode backward = KeyCode.S;
    KeyCode left = KeyCode.A;
    KeyCode right = KeyCode.D;

    KeyCode ascend = KeyCode.UpArrow;
    KeyCode descend = KeyCode.DownArrow;
    KeyCode rollCounter = KeyCode.LeftArrow;
    KeyCode rollClock = KeyCode.RightArrow;

    Vector3 thruster;
    float thrustStrength = 1f;
    
    float roller;

    float angSpeed = 1.0f;
    float rollSpeed = 10.0f;
    float smoothedAngSpeed = 1.0f;

    int timeTillPlayer = 60;

    Quaternion targetAng;
    Quaternion smoothedAng;

    int numCollisionTouches;

    public float yawInput;

    public Canvas canvas;

    public static event System.Action OnCollide;
    public static event System.Action OnExitCollide;
    public static event System.Action OnPlayer;
    // cross scripts communication (to UniversalGravitation and PlayerController)
    
    GameObject planet;
    
    Camera mainCam;

    bool isChangeSpeed = false;

    float scaleOnCollide;

    Transform collision;

    float[] planetPositions = new float[18];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = Vector3.zero;
        targetAng = transform.rotation;
        smoothedAng = transform.rotation;
        mass = rb.mass;
        mainCam = Camera.main;
        base.Awake();

        string[] planets = {"Neptune", "Saturn", "Mars", "Venus", "Sun", "Mercury",  "Earth",  "Jupiter", "Uranus"};
        for (int i = 0; i < 18; i+=2)
        {
            planetPositions[i]= GameObject.Find(planets[i/2]).GetComponent<CelestialBody>().transform.position.x-101;
            Debug.Log($"planet position 1 {planetPositions[i]}");
            planetPositions[i+1] = GameObject.Find(planets[i/2]).GetComponent<CelestialBody>().transform.position.x + 101;
            Debug.Log($"planet position 2 {planetPositions[i + 1]}");
        }
        rb.position = new Vector3(planetPositions[DataHolder.spaceshipPos], 0, 0);
        Debug.Log($"spaceship position {rb.position}");
    }

    //private void Start()
    //{
    //    rb.position = new Vector3(planetPositions[DataHolder.spaceshipPos], 0, 0);
    //}

    void Update()
    {
        Move();
        //Debug.Log($"velocity {rb.linearVelocity}");
        //Debug.Log(rb.linearVelocity);
    }

    void FixedUpdate()
    {
        Vector3 thrustDir = transform.TransformVector(thruster); // from local space to world space
        //Debug.Log($"thrustDir {thrustDir}");
        //Debug.Log($"velocity {rb.linearVelocity}");
        rb.AddForce(thrustDir * thrustStrength*UniversalGravitation.scaleForLanding, ForceMode.Acceleration);

        if (numCollisionTouches == 0)
        {
            smoothedAng = Quaternion.Normalize(smoothedAng);
            rb.MoveRotation(smoothedAng);
        }

        if (numCollisionTouches == 1)
        {
            timeTillPlayer--;
            if (timeTillPlayer <= 0)
            {
                switchToPlayer();
            }
            //Debug.Log($"timeTillPlayer {timeTsillPlayer}");
        }
        else
        {
            timeTillPlayer = 60;
        }
        // avoid situations where the spaceship might collide and exit more than once after impact
    }

    void Move()
    {
        thruster = new Vector3(
            Input.GetKey(right) ? 1 : Input.GetKey(left) ? -1 : 0,
            Input.GetKey(ascend) ? 1 : Input.GetKey(descend) ? -1 : 0,
            Input.GetKey(forward) ? 1 : Input.GetKey(backward) ? -1 : 0
        );

        if (numCollisionTouches == 0)
        {
            Rotate();
        }
    }

    void Rotate() 
    {
        roller = Input.GetKey(rollClock) ? 1 : Input.GetKey(rollCounter) ? -1 : 0;
        yawInput = Input.GetAxisRaw("Mouse X") * angSpeed;
        float pitchInput = Input.GetAxisRaw("Mouse Y") * angSpeed;
        float rollInput = roller * rollSpeed * Time.deltaTime;

        Quaternion yaw = Quaternion.AngleAxis(yawInput, transform.up);
        Quaternion pitch = Quaternion.AngleAxis(-pitchInput, transform.right);
        Quaternion roll = Quaternion.AngleAxis(-rollInput, transform.forward);

        // reason can't use *= is due to non-commutative nature of quaternions
        targetAng = yaw * pitch * roll * targetAng;
        
        smoothedAng = Quaternion.Slerp(transform.rotation, targetAng, Time.deltaTime * smoothedAngSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        //numCollisionTouches = Mathf.Min(1, numCollisionTouches + 1);
        //Debug.Log(numCollisionTouches);
        Debug.Log("Collision with " + collision.gameObject.name);
        //Debug.Log($"impact Velocity {rb.linearVelocity}");

        if (OnCollide != null)
        {
            OnCollide();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        vel = double3.zero;
        if (collision.gameObject.GetComponent<PlayerController>() is null)
        {
            transform.SetParent(collision.transform);
            this.collision = collision.transform;
            Quadtree quadtree = collision.gameObject.GetComponent<Quadtree>();
            if (quadtree is not null)
            {
                QuadtreeLock(quadtree);
            }
            //Debug.Log($"collision transform {collision.transform.parent.name}");
            planet = collision.transform.parent.gameObject;
            //Debug.Log($"planet {planet}");
        }   
        scaleOnCollide = UniversalGravitation.scaleForLanding;
    }

    private void OnCollisionExit(Collision collision)
    {
        //numCollisionTouches = Mathf.Max(0, numCollisionTouches - 1);
        //Debug.Log("Exit Collision with " + collision.gameObject.name);
        if (OnExitCollide != null)
        {
            OnExitCollide();
        }
        transform.SetParent(null);
        //Debug.Log(numCollisionTouches);
    }

    void switchToPlayer()
    {
        if (Input.GetKeyDown(KeyCode.B) && FindAnyObjectByType<PlayerController>() is null)
        {
            // rework direct call of function in player control
            PlayerController player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            player.gameObject.SetActive(true);

            Vector3 gravityUp = (player.transform.position - planet.transform.position).normalized;
            player.transform.position = transform.position - gravityUp*10;

            //Debug.Log($"player position {player.rb.position}, transform position {player.transform.position}");
            player.planet = planet;
            player.transform.SetParent(planet.transform);

            //Debug.Log($"player parent {player.transform}");
            UniversalGravitation.trackBody = player;
            Quadtree.viewer = player.transform;

            //Debug.Log($"player position {player.rb.position}");
            mainCam.gameObject.SetActive(false);
            enabled = false;
            canvas.gameObject.SetActive(false);
        }
    }

    void thrusterUpdate()
    {
        thrustStrength *= 10;
        if (!isChangeSpeed)
        {
            InvokeRepeating("DropSpeed", 0, 0.1f); // chose this over coroutine due to its more efficient processing (and it's good for purpose)
        }
    }

    void DropSpeed()
    {
        if (UniversalGravitation.scaleForLanding < scaleOnCollide)
        {
            thrustStrength /= 10;
            isChangeSpeed = true;
            CancelInvoke("DropSpeed");
        }   
    }

    void QuadtreeLock(Quadtree quadtree)
    {
        if (quadtree.parent is not null)
        {
            quadtree.genLock = true;
            QuadtreeLock(quadtree.parent);
        }
    }
}