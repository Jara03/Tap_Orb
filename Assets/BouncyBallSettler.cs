using UnityEngine;

/// <summary>
/// Stops a highly bouncy rigidbody from endlessly jittering once it has settled on the ground.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BouncyBallSettler : MonoBehaviour
{
    [Tooltip("Absolute vertical velocity below which the ball is considered settled.")]
    [SerializeField]
    private float settleVelocityThreshold = 0.05f;

    [Tooltip("Minimum consecutive time (in seconds) that the ball must remain settled before being forced to sleep.")]
    [SerializeField]
    private float settleConfirmationTime = 0.15f;

    [Tooltip("Extra distance added to the collider half-height when checking if the ball is touching the ground.")]
    [SerializeField]
    private float groundCheckPadding = 0.05f;

    [Tooltip("Layers that are considered ground for the settling check.")]
    [SerializeField]
    private LayerMask groundLayers = Physics.DefaultRaycastLayers;

    private Rigidbody cachedRigidbody;
    private Collider cachedCollider;
    private float settledTimer;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        if (cachedRigidbody == null)
        {
            return;
        }

        if (cachedRigidbody.IsSleeping())
        {
            settledTimer = 0f;
            return;
        }

        if (HasSettled())
        {
            settledTimer += Time.fixedDeltaTime;

            if (settledTimer >= settleConfirmationTime)
            {
                Vector3 velocity = cachedRigidbody.velocity;
                velocity.y = 0f;
                cachedRigidbody.velocity = velocity;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.Sleep();
                settledTimer = 0f;
            }
        }
        else
        {
            settledTimer = 0f;
        }
    }

    private bool HasSettled()
    {
        if (Mathf.Abs(cachedRigidbody.velocity.y) > settleVelocityThreshold)
        {
            return false;
        }

        return IsGrounded();
    }

    private bool IsGrounded()
    {
        if (cachedCollider == null)
        {
            return Physics.Raycast(transform.position, Vector3.down, groundCheckPadding, groundLayers, QueryTriggerInteraction.Ignore);
        }

        Bounds bounds = cachedCollider.bounds;
        float rayDistance = bounds.extents.y + groundCheckPadding;
        Vector3 rayOrigin = bounds.center;

        return Physics.Raycast(rayOrigin, Vector3.down, rayDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }
}
