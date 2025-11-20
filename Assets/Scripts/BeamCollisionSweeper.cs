using UnityEngine;

public class BeamCollisionSweeper : MonoBehaviour
{
    public Rigidbody rb;              // Le rigidbody kinematic de la poutre
    public float hitRadius = 0.5f;    // Rayon du balayage (augmente si nécessaire)
    public LayerMask ballMask;        // La couche de ta boule (pour filtrer)

    private Vector3 previousPosition;

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        previousPosition = rb.position;
    }

    private void FixedUpdate()
    {
        Vector3 currentPosition = rb.position;
        Vector3 movement = currentPosition - previousPosition;
        float distance = movement.magnitude;

        if (distance > 0f)
        {
            // 🔥 Sweep principal (le plus fiable)
            if (rb.SweepTest(movement.normalized, out RaycastHit hit, distance))
            {
                HandleImpact(hit);
            }
            else
            {
                // 🔥 Option de secours ultra-stable : raycast épais
                if (Physics.SphereCast(previousPosition, hitRadius, movement.normalized, out hit, distance, ballMask))
                {
                    HandleImpact(hit);
                }
            }
        }

        previousPosition = currentPosition;
    }

    private void HandleImpact(RaycastHit hit)
    {
        // Ici tu utilises TON système existant pour taper la balle
        var ballRb = hit.collider.attachedRigidbody;

        if (ballRb != null)
        {
            // Exemple d'effet
            Vector3 dir = (hit.point - transform.position).normalized;
            ballRb.AddForce(dir * 20f, ForceMode.Impulse);
        }

        // Debug visible dans la scène
        Debug.Log("BALL HIT by sweep: " + hit.collider.name);
    }
}