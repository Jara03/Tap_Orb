using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float launchSpeed = 15f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool overrideVelocity = true;
    [SerializeField] private bool alignWithPadUp = true;
    [SerializeField] private Vector3 customLaunchDirection = Vector3.up;

    [Header("Input Control")]
    [SerializeField] private bool requireInput = false;
    [SerializeField] private InputController inputController;
    [SerializeField] private float inputBufferTime = 0.15f;

    private float lastPressedTime;
    private bool hasLaunchedThisContact;

    private void Update()
    {
        if (!requireInput || inputController == null) return;

        if (inputController.IsPressed())
            lastPressedTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryLaunch(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLaunch(other);
    }

    private void OnCollisionExit(Collision collision)
    {
        hasLaunchedThisContact = false;
    }

    private void OnTriggerExit(Collider other)
    {
        hasLaunchedThisContact = false;
    }

    private void TryLaunch(Collider collider)
    {
        if (hasLaunchedThisContact) return;
        if (!collider.CompareTag(playerTag)) return;

        if (requireInput)
        {
            bool inputValid = Time.time - lastPressedTime <= inputBufferTime;
            if (!inputValid) return;

            // Consomme l’input
            lastPressedTime = -999f;
        }

        Rigidbody rb = collider.attachedRigidbody;
        if (rb == null) return;

        Launch(rb);
        hasLaunchedThisContact = true;
    }

    private void Launch(Rigidbody rb)
    {
        Vector3 direction = alignWithPadUp ? transform.up : customLaunchDirection;
        direction.Normalize();

        Vector3 velocity = rb.linearVelocity;

        if (overrideVelocity)
        {
            float dot = Vector3.Dot(velocity, direction);
            velocity -= direction * dot;
        }

        velocity += direction * launchSpeed;
        rb.linearVelocity = velocity;

        Debug.Log($"[JumpPad] Launch OK | Velocity: {rb.linearVelocity}");
    }
}
