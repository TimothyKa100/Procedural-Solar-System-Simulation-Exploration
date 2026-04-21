using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Unity.Mathematics;
using System.Security.Cryptography;
using System.Collections.Generic;

public class UniversalGravitation : MonoBehaviour
{
    Stack<CelestialBody> celestialBodies;
    private float sunMass;
    int celestialBodyCount;

    private float timeStep = 8 * 60f;

    public const double G = 6.6743015e-11;
    public const double adjustedG = 1.190594655e-10; // as stated in design, calculated value for base unit change

    float distToLanding = 100f;
    bool landing = false;
    bool leaving = false;
    public static float scaleForLanding { get; set; } = 1f;
    public float reinstateDist;
    float distanceThreshold = 0.0001f;

    public GameObject sun;
    public Spaceship spaceship;
    PlayerController player;
    public static rbBody trackBody;
    public Transform viewer;

    Vector3 actualSpaceshipPos;

    public PhysicsMaterial noBounce;

    private void Awake()
    {
        celestialBodies = new Stack<CelestialBody>(FindObjectsByType<CelestialBody>(FindObjectsSortMode.None));
        celestialBodies = new Stack<CelestialBody>(celestialBodies.OrderBy(body => body is Spaceship ? 1 : 0)); // Spaceship to be last item ready to be popped
        celestialBodyCount = celestialBodies.Count;
        //Debug.Log($"celestialBodyCount{celestialBodyCount}");
        //foreach (CelestialBody body in celestialBodies)
        //{
        //    Debug.Log(body.name);
        //}
        
    }

    void Start()
    {
        Quadtree.noBounce = noBounce;
        trackBody = spaceship;

        if (sun is null)
        {
            //Debug.Log("sun is null");
            sun = GameObject.FindWithTag("Sun");
        }
        sunMass = sun.GetComponent<CelestialBody>().mass;
        foreach (CelestialBody planet in celestialBodies)
        {
            if (planet.CompareTag("Sun"))
                continue;
            if (math.all(planet.vel == double3.zero) && planet is not rbBody)
            {
                planet.vel = new double3(0, 0, CalculateInitialVel(planet));
            }
        }

        Time.fixedDeltaTime = 1 / 60f;

        Spaceship.OnCollide += removeBody;
        Spaceship.OnExitCollide += addBody;

        Quadtree.viewer = viewer;
    }

    double CalculateInitialVel(CelestialBody planet)
    {
        double r = math.length(planet.pos);
        double v = Math.Sqrt(G * sunMass / r) * planet.pos.x/r;
        return v;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                timeStep = Mathf.Max(1f, timeStep / 2);
            }
            else
            {
                timeStep *= 2;
            }
        }
    }

    private void FixedUpdate()
    {
        //Debug.Log($"update celestial body count {celestialBodyCount}");
        //foreach (CelestialBody body in celestialBodies)
        //{
        //    if (body.gameObject.activeSelf)
        //        Debug.Log(body.name);
        //}
        RK4();
        if (landing)
        {
            if (scaleForLanding < 2550)
            {
                if (scaleForLanding == 2048)
                {
                    scaleForLanding = 2550;
                    foreach (CelestialBody body in celestialBodies)
                    {
                        if (body is rbBody rigidBody)
                        {
                            rigidBody.rb.linearVelocity *= 1.245117f;
                        }
                    }
                }
                else
                {
                    float newScaleForLanding = Mathf.Min(scaleForLanding * 4f, 2048);
                    if (newScaleForLanding != scaleForLanding)
                    {
                        scaleForLanding = newScaleForLanding;
                        foreach (CelestialBody body in celestialBodies)
                        {
                            if (body is rbBody rigidBody)
                            {
                                rigidBody.rb.linearVelocity *= 4;
                            }
                        }
                    }
                }
            }

            //Debug.Log($"landing scale {scaleForLanding}");
            distToLanding = Mathf.Min(distToLanding * 2f, 1275);
            //Debug.Log($"distance to landing {distToLanding}");
        }
        if (leaving)
        {
            if (scaleForLanding > 0)
            {
                if (scaleForLanding == 2550)
                {
                    scaleForLanding = 2048;
                    foreach (CelestialBody body in celestialBodies)
                    {
                        if (body is rbBody rigidBody)
                        {
                            rigidBody.rb.linearVelocity /= 1.245117f;
                        }
                    }
                }
                else
                {
                    float newScaleForLanding = Mathf.Max(scaleForLanding / 4f, 1);
                    if (newScaleForLanding != scaleForLanding)
                    {
                        scaleForLanding = newScaleForLanding;
                        foreach (CelestialBody body in celestialBodies)
                        {
                            if (body is rbBody rigidBody)
                            {
                                rigidBody.rb.linearVelocity /= 4f;
                            }
                        }
                    }
                }
            }
            //Debug.Log($"landing scale {scaleForLanding}");
            //Debug.Log($"leaving");
            distToLanding = Mathf.Max(100, distToLanding / 2f);
        }

        ChangeScale();
        //Debug.Log("landing 3" + landing);
        if (trackBody is not null)
        UpdateFloatingOrigin();
        //Debug.Log("scale" + scaleForLanding);
        //Debug.Log("fixed update");
    }

    //legacy method
    //public static Vector3 CalcAcc(Transform body)
    //{
    //    Vector3 acceleration = Vector3.zero;
    //    foreach (CelestialBody celBody in Instance.celestialBodies)
    //    {
    //        Vector3 r = celBody.transform.position - body.position;
    //        Debug.Log(r);
    //        float rSqauredMagnitude = r.sqrMagnitude;

    //        if (rSqauredMagnitude < 0.0001f) continue;

    //        acceleration += G * celBody.mass * r.normalized / rSqauredMagnitude;
    //        Debug.Log(acceleration);
    //    }
    //    return acceleration;
    //}

    double3 CalcAcc(double3[] positions, int bodyIndex, bool firstIteration = false)
    {
        double3 acceleration = double3.zero;
        double shortestDistance = double.MaxValue;

        bool isSpaceship = celestialBodies.ElementAt(bodyIndex) is Spaceship && firstIteration;
        for (int i = 0; i < celestialBodyCount; i++)
        {
            if (i == bodyIndex) continue;

            double3 r = positions[i] - positions[bodyIndex];
            double rSquaredMagnitude = math.lengthsq(r);
            double rMag = math.length(r);
            if (rMag < shortestDistance) shortestDistance = rMag;

            if (isSpaceship && shortestDistance == rMag)
            {
                //Debug.Log("rMag: " + rMag);
                if (rMag * scaleForLanding < distToLanding)
                {
                    landing = true;
                    //Debug.Log("landing true 1");
                }
                else landing = false;
                // only true if the shortest distance is more than that

                if (rMag * scaleForLanding > (2 * (distToLanding + 1)))
                {
                    //Debug.Log("landing false");
                    leaving = true;
                }
                else leaving = false;
            }

            if (rSquaredMagnitude < 0.0001f) continue; // Avoid division by zero

            acceleration += G * celestialBodies.ElementAt(i).mass * math.normalize(r) / rSquaredMagnitude;
        }
        return acceleration;
    }

    // New Method
    void RK4()
    {
        double3[] k1v = new double3[celestialBodyCount];
        double3[] k1r = new double3[celestialBodyCount];
        double3[] k2v = new double3[celestialBodyCount];
        double3[] k2r = new double3[celestialBodyCount];
        double3[] k3v = new double3[celestialBodyCount];
        double3[] k3r = new double3[celestialBodyCount];
        double3[] k4v = new double3[celestialBodyCount];
        double3[] k4r = new double3[celestialBodyCount];

        double3[] currentPositions = new double3[celestialBodyCount];
        double3[] currentVelocities = new double3[celestialBodyCount];

        double3[] tempPositions = new double3[celestialBodyCount];
        double3[] tempVelocities = new double3[celestialBodyCount];

        for (int i = 0; i < celestialBodyCount; i++)
        {
            currentPositions[i] = celestialBodies.ElementAt(i).pos;
            currentVelocities[i] = celestialBodies.ElementAt(i).vel;
        }

        for (int i = 0; i < celestialBodyCount; i++)
        {
            if (celestialBodies.ElementAt(i).CompareTag("Sun"))
                continue;
            k1v[i] = CalcAcc(currentPositions, i, true) * timeStep;
            k1r[i] = currentVelocities[i] * timeStep;
        }

        tempPositions = tempOffset(currentPositions, k1r, 0.5f);
        tempVelocities = tempOffset(currentVelocities, k1v, 0.5f);

        for (int i = 0; i < celestialBodyCount; i++)
        {
            if (celestialBodies.ElementAt(i).CompareTag("Sun"))
                continue;
            k2v[i] = CalcAcc(tempPositions, i) * timeStep;
            k2r[i] = tempVelocities[i] * timeStep;
        }

        tempPositions = tempOffset(currentPositions, k2r, 0.5f);
        tempVelocities = tempOffset(currentVelocities, k2v, 0.5f);

        for (int i = 0; i < celestialBodyCount; i++)
        {
            if (celestialBodies.ElementAt(i).CompareTag("Sun"))
                continue;
            k3v[i] = CalcAcc(tempPositions, i) * timeStep;
            k3r[i] = tempVelocities[i] * timeStep;
        }

        tempPositions = tempOffset(currentPositions, k3r, 1f);
        tempVelocities = tempOffset(currentVelocities, k3v, 1f);

        for (int i = 0; i < celestialBodyCount; i++)
        {
            if (celestialBodies.ElementAt(i).CompareTag("Sun"))
                continue;
            k4v[i] = CalcAcc(tempPositions, i) * timeStep;
            k4r[i] = tempVelocities[i] * timeStep;
        }

        for (int i = 0; i < celestialBodyCount; i++)
        {
            double3 deltaVel = (k1v[i] + 2f * k2v[i] + 2f * k3v[i] + k4v[i]) / 6f;
            celestialBodies.ElementAt(i).vel += deltaVel;
            double3 deltaPos = (k1r[i] + 2f * k2r[i] + 2f * k3r[i] + k4r[i]) / 6f;

            // since spaceship is the last element, we can do the following without worrying it will intersect with other planets
            if (celestialBodies.ElementAt(i) is Spaceship spaceship)
            {
                //Debug.Log("vel " + rbBody.rb.linearVelocity);
                //Debug.Log("deltaVel: " + deltaVel);
                //Debug.Log("original position " + rbBody.rb.position + "scale" + scaleForLanding);
                //Debug.Log("Update delta vel: " + math.float3(deltaVel * scaleForLanding * timeStep / Time.fixedDeltaTime));
                spaceship.rb.AddForce(math.float3(deltaVel * scaleForLanding * timeStep / Time.fixedDeltaTime), ForceMode.VelocityChange);
                //Debug.Log("spaceship position "+ rbBody.rb.position + "scale" + scaleForLanding);
                celestialBodies.ElementAt(i).pos = math.double3(spaceship.rb.position) / scaleForLanding;
            }
            else
            {
                celestialBodies.ElementAt(i).pos += deltaPos;
                celestialBodies.ElementAt(i).transform.position = math.float3(celestialBodies.ElementAt(i).pos * scaleForLanding);
                //CheckForCrash(celestialBodies.ElementAt(i));
            }
        }
    }

    void ChangeScale()
    {
        foreach (CelestialBody body in celestialBodies)
        {
            if (body is not rbBody)
            {
                body.transform.localScale = body.initialLocalScale * scaleForLanding;
            }
        }
    }

    double3[] tempOffset(double3[] currentState, double3[] calcPhase, float scale)
    {
        double3[] temp = new double3[celestialBodyCount];
        for (int i = 0; i < celestialBodyCount; i++)
        {
            temp[i] = currentState[i] + calcPhase[i] * scale;
        }
        return temp;
    }

    //I have just decided to implement calculation direction from this script so there's no point of using this method anymore!
    //public static CelestialBody[] Bodies => Instance.celestialBodies;

    //No longer needed as I am calculating acceleration directly from here
    //static UniversalGravitation Instance
    //{
    //    get
    //    {
    //        if (instance == null)
    //        {
    //            instance = FindAnyObjectByType<UniversalGravitation>();
    //        }
    //        return instance;
    //    }
    //}


    //The main issue is that the collision detection could register more than once occassionally? -- this is to avoid accidental pop/push of other celestial bodies
    void removeBody()
    {
        if (!celestialBodies.Any(body => body is Spaceship))
        {
            return;
        }
        else
        {
            celestialBodies.Pop();
            celestialBodyCount--;
        }
        Debug.Log(celestialBodies.Count);
        foreach (CelestialBody body in celestialBodies)
        {
            Debug.Log(body.name);
        }
    }
    // this function reworked for stack implementation

    void addBody()
    {
        if (celestialBodies.Any(body => body is Spaceship))
        {
            return;
        }
        else
        {
            celestialBodies.Push(FindAnyObjectByType<Spaceship>());
            celestialBodyCount++;
        }
        //Debug.Log(celestialBodies.Count);
        //foreach (CelestialBody body in celestialBodies)
        //{
        //    Debug.Log(body.name);
        //}
        //Debug.Log(celestialBodies.Count);
    }

    private void UpdateFloatingOrigin()
    {
        float3 originOffset = trackBody.rb.position;
        //Debug.Log("origin: " + originOffset + "scale" + scaleForLanding);
        //Debug.Log("origin: " + originOffset + "scale" + scaleForLanding);
        double dstFromOrigin = math.length(originOffset);
        if (trackBody is Spaceship)
        {
            actualSpaceshipPos += (Vector3)originOffset;
        }

        if (dstFromOrigin > distanceThreshold)
        {
            //Debug.Log("distance from origin: " + dstFromOrigin);
            foreach (CelestialBody body in celestialBodies)
            {
                body.pos -= originOffset / scaleForLanding;
                //Debug.Log("body pos: " + body.pos + "name: " + body.name);
                //Debug.Log("body pos: " + body.pos);
                if (body is rbBody rbBod)
                {
                    rbBod.rb.position = math.float3(rbBod.pos * scaleForLanding);
                }
                else
                {
                    body.transform.position = math.float3(body.pos * scaleForLanding);
                }
            }
            if (trackBody is PlayerController)
            {
                trackBody.rb.position -= (Vector3)originOffset;
            }
        }
    }

    void reActivateShip()
    {
        if (trackBody is not Spaceship)
        {
            if ((trackBody.rb.position - spaceship.rb.position).magnitude < reinstateDist)
            {
                trackBody.gameObject.SetActive(false);
                trackBody = spaceship;
            }
        }
    }
}