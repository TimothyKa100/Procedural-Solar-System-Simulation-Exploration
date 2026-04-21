using UnityEngine;
using static Unity.Mathematics.math;

public class Minimap: MonoBehaviour
{
    public CelestialBody planet;
    private float scalingFactor = 0.001f;
    Spaceship spaceship;

    private void Start()
    {
        spaceship = FindAnyObjectByType<Spaceship>();
        transform.localScale = Vector3.one * 0.3f;
        transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    void Update()
    {
        transform.position = float3(planet.pos) * scalingFactor;
    }
}