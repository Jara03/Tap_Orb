using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class MineItem : MonoBehaviour
{
    [Header("Détection")]
    [Tooltip("Rayon auquel la mine détecte le joueur et déclenche l’explosion.")]
    [SerializeField]
    private float triggerRadius = 2f;

    [Header("Déflagration")]
    [Tooltip("Force appliquée au joueur lors de l’explosion.")]
    [SerializeField]
    private float explosionForce = 15f;

    [Tooltip("Rayon de la déflagration utilisé pour appliquer la force.")]
    [SerializeField]
    private float explosionRadius = 3f;

    [Tooltip("Facteur vertical appliqué à la force d’explosion.")]
    [SerializeField]
    private float upwardsModifier = 0.5f;

    [Tooltip("Effet visuel optionnel instancié lors de l’explosion.")]
    [SerializeField]
    private GameObject explosionEffectPrefab;

    [Tooltip("Transform depuis lequel l’effet visuel est instancié (facultatif).")]
    [SerializeField]
    private Transform effectSpawnPoint;

    [Tooltip("Durée avant destruction automatique de l’effet visuel.")]
    [SerializeField]
    private float effectLifetime = 2f;

    private bool hasExploded;
    private SphereCollider detectionCollider;

    private void Awake()
    {
        detectionCollider = GetComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = triggerRadius;
    }

    private void OnValidate()
    {
        if (triggerRadius < 0f)
        {
            triggerRadius = 0f;
        }

        if (explosionRadius < 0f)
        {
            explosionRadius = 0f;
        }

        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<SphereCollider>();
        }

        if (detectionCollider != null)
        {
            detectionCollider.isTrigger = true;
            detectionCollider.radius = triggerRadius;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody playerRigidbody = other.attachedRigidbody;
        if (playerRigidbody == null)
        {
            return;
        }

        Explode(playerRigidbody);
    }

    private void Explode(Rigidbody playerRigidbody)
    {
        hasExploded = true;

        Vector3 explosionPosition = transform.position;
        if (effectSpawnPoint != null)
        {
            explosionPosition = effectSpawnPoint.position;
        }

        SpawnEffect(explosionPosition);
        ApplyExplosionForce(playerRigidbody, explosionPosition);

        if (detectionCollider != null)
        {
            detectionCollider.enabled = false;
        }

        // On désactive le visuel de la mine après l’explosion.
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
    }

    private void SpawnEffect(Vector3 explosionPosition)
    {
        if (explosionEffectPrefab == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(
            explosionEffectPrefab,
            explosionPosition,
            Quaternion.identity
        );

        if (effectLifetime > 0f)
        {
            Destroy(effectInstance, effectLifetime);
        }
    }

    private void ApplyExplosionForce(Rigidbody playerRigidbody, Vector3 explosionPosition)
    {
        playerRigidbody.AddExplosionForce(
            explosionForce,
            explosionPosition,
            explosionRadius > 0f ? explosionRadius : triggerRadius,
            upwardsModifier,
            ForceMode.Impulse
        );
    }
}
