using UnityEngine;
using Unity.Mathematics;

[ExecuteInEditMode]
public class CelestialBody : MonoBehaviour
{
    public double3 vel;
    public double3 pos;

    public float tiltAngle;
    public float rotationSpeed;

    public Vector3 initialLocalScale;

    public float mass;
    public float radius { get; set; } = 0.5f;

    public void Awake()
    {
        //Debug.Log("transfrom.position: " + transform.position);
        pos = new double3(transform.position.x, transform.position.y, transform.position.z);
        initialLocalScale = transform.localScale;
    }
}