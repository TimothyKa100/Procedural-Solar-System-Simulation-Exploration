using UnityEngine;

public class rbBody: CelestialBody
{
    public Rigidbody rb;
    private void Awake()
    {
        mass = 0.000001f;
        base.Awake();
    }
}