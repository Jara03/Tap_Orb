using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float launchSpeed = 15f;
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("Si true, la composante dans la direction du pad est fixée à launchSpeed (plus stable).")]
    [SerializeField] private bool overrideAlongDirection = true;

    [Tooltip("Si true, on conserve la vitesse latérale (sur le plan perpendiculaire au launch).")]
    [SerializeField] private bool keepLateralVelocity = true;

    [SerializeField] private bool alignWithPadUp = true;
    [SerializeField] private Vector3 customLaunchDirection = Vector3.up;

    [Header("Input Control")]
    [SerializeField] private bool requireInput = false;
    [SerializeField] private InputController inputController;

    [Tooltip("Buffer : si tu as tap dans cette fenêtre AVANT le contact, ça lance.")]
    [SerializeField] private float pressBufferTime = 0.15f;

    [Tooltip("Grace : si tu tapes dans cette fenêtre APRÈS le contact, ça lance aussi.")]
    [SerializeField] private float contactGraceTime = 0.20f;

    // ---- State ----
    private float lastPressTime = -999f;

    private bool isPlayerInContact;
    private Rigidbody playerRb;
    private bool hasLaunchedThisContact;

    // Pour détecter un "tap" propre (front montant)
    private bool previousPressedState;

    private void Update()
    {
        if (!requireInput) return;

        // Si tu n'as pas d'InputController, tu peux fallback ici si besoin
        bool pressedNow = (inputController != null) && inputController.IsPressed();

        // Détecte l'instant exact où le tap arrive (pressedNow = true alors qu'avant c'était false)
        if (pressedNow && !previousPressedState)
        {
            lastPressTime = Time.unscaledTime; // unscaled => plus stable si tu joues avec Time.timeScale
        }

        previousPressedState = pressedNow;
    }

    private void FixedUpdate()
    {
        if (!requireInput) return;
        if (!isPlayerInContact) return;
        if (hasLaunchedThisContact) return;
        if (playerRb == null) return;

        // Si le tap arrive juste APRES l'atterrissage (grace time), on autorise aussi
        bool pressBuffered = Time.unscaledTime - lastPressTime <= pressBufferTime;
        bool stillInGraceWindow = Time.unscaledTime - contactEnterTime <= contactGraceTime;

        // Deux cas valides :
        // 1) Tu as tap juste AVANT de toucher (pressBuffered)
        // 2) Tu as tap juste APRES avoir touché (stillInGraceWindow)
        //    -> dans ce cas, on exige un tap récent aussi
        bool canLaunch = pressBuffered || (stillInGraceWindow && pressBuffered);

        if (!canLaunch) return;

        Launch(playerRb);
        hasLaunchedThisContact = true;

        // Consomme l'input pour éviter les double triggers
        lastPressTime = -999f;
    }

    private float contactEnterTime = -999f;

    private void OnCollisionEnter(Collision collision) => TryRegisterContact(collision.collider);
    private void OnTriggerEnter(Collider other) => TryRegisterContact(other);

    private void OnCollisionExit(Collision collision) => ClearContactIfSame(collision.collider);
    private void OnTriggerExit(Collider other) => ClearContactIfSame(other);

    private void TryRegisterContact(Collider collider)
    {
        if (!collider.CompareTag(playerTag)) return;

        Rigidbody rb = collider.attachedRigidbody;
        if (rb == null) return;

        // Si on touche un nouveau rb, on refresh
        playerRb = rb;
        isPlayerInContact = true;
        contactEnterTime = Time.unscaledTime;

        // Si input non requis => launch immédiat
        if (!requireInput && !hasLaunchedThisContact)
        {
            Launch(playerRb);
            hasLaunchedThisContact = true;
        }
        else
        {
            // Si input requis :
            // si tu as tap dans la fenêtre AVANT contact => launch immédiat aussi
            bool pressBuffered = Time.unscaledTime - lastPressTime <= pressBufferTime;
            if (pressBuffered && !hasLaunchedThisContact)
            {
                Launch(playerRb);
                hasLaunchedThisContact = true;
                lastPressTime = -999f;
            }
        }
    }

    private void ClearContactIfSame(Collider collider)
    {
        if (playerRb == null) return;
        if (collider.attachedRigidbody != playerRb) return;

        isPlayerInContact = false;
        playerRb = null;
        hasLaunchedThisContact = false;
        contactEnterTime = -999f;
    }

    private void Launch(Rigidbody rb)
    {
        Vector3 direction = alignWithPadUp ? transform.up : customLaunchDirection;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.up;
        direction.Normalize();

        // Unity classic : rb.velocity
        // Unity 6 : rb.linearVelocity existe aussi chez certains
        Vector3 v = rb.linearVelocity;

        Vector3 lateral = keepLateralVelocity ? Vector3.ProjectOnPlane(v, direction) : Vector3.zero;

        if (overrideAlongDirection)
        {
            // propulsion stable : composante dans la direction = launchSpeed
            v = lateral + direction * launchSpeed;
        }
        else
        {
            // ajoute une impulsion (plus "arcade", mais dépend plus de la vitesse actuelle)
            v = lateral + (Vector3.Project(v, direction)) + direction * launchSpeed;
        }

        rb.linearVelocity = v;
    }
}
