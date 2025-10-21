using UnityEngine;

/// <summary>
/// Launches the player upward when they touch the pad, creating a trampoline effect.
/// </summary>
[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [Tooltip("Speed applied to the player when they hit the jump pad.")]
    [SerializeField]
    private float launchSpeed = 15f;

    [Tooltip("Tag used to identify the player object.")]
    [SerializeField]
    private string playerTag = "Player";

    [Tooltip("Replace the player's current velocity along the launch direction instead of adding to it.")]
    [SerializeField]
    private bool overrideVelocity = true;

    [Tooltip("Use the pad's up direction as the launch direction.")]
    [SerializeField]
    private bool alignWithPadUp = true;

    [Tooltip("Custom launch direction when not aligning with the pad's up vector.")]
    [SerializeField]
    private Vector3 customLaunchDirection = Vector3.up;

    private void OnCollisionEnter(Collision collision)
    {
        TryLaunch(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLaunch(other);
    }

    private void TryLaunch(Collider collider)
    {
        if (collider == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(playerTag) && !collider.CompareTag(playerTag))
        {
            return;
        }

        Rigidbody attachedRigidbody = collider.attachedRigidbody;
        if (attachedRigidbody == null)
        {
            return;
        }

        Launch(attachedRigidbody);
    }

    private void Launch(Rigidbody targetRigidbody)
    {
        Vector3 launchDirection = GetLaunchDirection();

        if (launchDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        launchDirection.Normalize();

        Vector3 velocity = targetRigidbody.linearVelocity;

        if (overrideVelocity)
        {
            float currentAlongDirection = Vector3.Dot(velocity, launchDirection);
            velocity -= launchDirection * currentAlongDirection;
        }

        velocity += launchDirection * launchSpeed;

        targetRigidbody.linearVelocity = velocity;
        targetRigidbody.velocity = velocity;
    }

    private Vector3 GetLaunchDirection()
    {
        if (alignWithPadUp)
        {
            return transform.up;
        }

        return customLaunchDirection;
    }

    private void Reset()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        if (collider is BoxCollider || collider is CapsuleCollider || collider is SphereCollider)
        {
            collider.isTrigger = false;
        }
    }
}
