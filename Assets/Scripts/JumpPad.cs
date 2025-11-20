using UnityEngine;

/// <summary>
/// Launches the player upward when they touch the pad.
/// Optionally requires an input press to trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [Header("Launch Settings")]
    [Tooltip("Speed applied to the player when they hit the jump pad.")]
    [SerializeField] private float launchSpeed = 15f;

    [Tooltip("Tag used to identify the player object.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Replace the player's current velocity along the launch direction instead of adding to it.")]
    [SerializeField] private bool overrideVelocity = true;

    [Tooltip("Use the pad's up direction as the launch direction.")]
    [SerializeField] private bool alignWithPadUp = true;

    [Tooltip("Custom launch direction when not aligning with the pad's up vector.")]
    [SerializeField] private Vector3 customLaunchDirection = Vector3.up;

    [Header("Input Control (optional)")]
    [Tooltip("If enabled, the pad only launches when the input is pressed.")]
    [SerializeField] private bool requireInput = false;

    [Tooltip("Reference to the InputController (if required).")]
    [SerializeField] private InputController inputController;

    [Tooltip("Allow small timing window after input press (seconds).")]
    [SerializeField] private float inputBufferTime = 0.15f;

    private float lastPressedTime;

    private void Awake()
    {
        if (requireInput && inputController == null)
        {
            inputController = FindObjectOfType<InputController>();
            if (inputController == null)
                Debug.LogWarning("[JumpPad] No InputController found in scene.");
        }
    }

    private void Update()
    {
        if (!requireInput || inputController == null) return;

        // Met à jour le moment du dernier input pressé
        if (inputController.IsPressed())
        {
            lastPressedTime = Time.time;
        }
            
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryLaunch(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryLaunch(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryLaunch(other);
    }
    
    private void OnCollisionStay(Collision collision)
    {
        TryLaunch(collision.collider);
    }

    private void TryLaunch(Collider collider)
    {
        Debug.Log("trying to launch");

        if (collider == null) return;
        if (!string.IsNullOrEmpty(playerTag) && !collider.CompareTag(playerTag)) return;

        // Vérifie si une entrée est requise
        if (requireInput && inputController != null)
        {
            bool inputActive = inputController.IsPressed() || (Time.time - lastPressedTime <= inputBufferTime);
            if (!inputActive)
            {
               // Debug.Log("[JumpPad] Input required but not pressed — launch cancelled.");
                return;
            }
        }

        Rigidbody rb = collider.attachedRigidbody;
        if (rb == null) return;

        Launch(rb);
    }

    private void Launch(Rigidbody targetRigidbody)
    {
        Vector3 launchDirection = alignWithPadUp ? transform.up : customLaunchDirection;

        if (launchDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            Debug.LogWarning("[JumpPad] Launch direction is invalid.");
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

        Debug.Log($"[JumpPad] 🚀 Launching {targetRigidbody.name} | Velocity: {velocity:F2}");
    }

    private void Reset()
    {
        Collider collider = GetComponent<Collider>();
        if (collider is BoxCollider || collider is CapsuleCollider || collider is SphereCollider)
            collider.isTrigger = false;
    }
}
