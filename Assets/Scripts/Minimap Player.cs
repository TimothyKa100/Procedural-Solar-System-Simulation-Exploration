using UnityEngine;

public class MinimapPlayer : MonoBehaviour
{
    Spaceship spaceship;
    Quaternion initAngle = Quaternion.Euler(-90, 0, 0);

    private void Start()
    {
        spaceship = FindAnyObjectByType<Spaceship>();
        transform.localScale = Vector3.one * 0.2f;
        transform.rotation = initAngle;
        transform.position = Vector3.zero;
    }

    void Update()
    {
        transform.eulerAngles += Quaternion.AngleAxis(spaceship.yawInput, Vector3.up).eulerAngles; // rotate about the axis by the input (same as the spaceship - but I didn't smooth here cause the complexity tradeoff is not worth it)
    }
}