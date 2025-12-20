using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallFakeTrail : MonoBehaviour
{
    public Transform trail;

    public float minSpeed = 0.5f;
    public float maxSpeed = 15f;

    public float minLength = 0.1f;
    public float maxLength = 1.2f;

    public float minAlpha = 0f;
    public float maxAlpha = 0.8f;

    public float offset = 0.2f; // distance derrière la balle

    Rigidbody rb;
    SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sr = trail.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;

        if (speed < minSpeed)
        {
            SetAlpha(0f);
            return;
        }

        Vector3 dir = velocity.normalized;

        // 1️⃣ Positionner le trail derrière la balle
        trail.position = transform.position - dir * offset;

        // 2️⃣ Rotation correcte en 2D
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        trail.rotation = Quaternion.Euler(0, 0, angle + 90f);

        // 3️⃣ Intensité selon la vitesse
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);

        // Longueur (axe Y du sprite)
        trail.localScale = new Vector3(
            trail.localScale.x,
            Mathf.Lerp(minLength, maxLength, t),
            1f
        );

        // Alpha
        SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, t));
    }

    void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}