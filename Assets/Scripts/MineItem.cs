using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class MineItem : MonoBehaviour
{
    [Header("Détection")]
    [Tooltip("Rayon auquel la mine détecte le joueur et déclenche l’explosion.")]
    [SerializeField]
    private float triggerRadius = 2f;

    [Header("Déflagration")]
    [Tooltip("Force appliquée lors de l’explosion.")]
    [SerializeField]
    private float explosionForce = 15f;

    [Tooltip("Rayon de la déflagration.")]
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

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Mine exploded");
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        Vector3 explosionPosition = effectSpawnPoint ? effectSpawnPoint.position : transform.position;

        SpawnEffect(explosionPosition);

        // Appliquer la force à tous les rigidbodies proches
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);
        foreach (Collider nearby in colliders)
        {
            Rigidbody rb = nearby.attachedRigidbody;
            if (rb != null)
            {
                Debug.Log("Applying force to " + nearby.name);
                rb.AddExplosionForce(
                    explosionForce,
                    explosionPosition,
                    explosionRadius,
                    upwardsModifier,
                    ForceMode.Impulse
                );
            }
        }

        // Désactiver la détection après explosion
        if (detectionCollider != null)
            detectionCollider.enabled = false;

        // Désactiver les rendus visuels
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        // (Optionnel) détruire la mine après un délai
        Destroy(gameObject, 1f);
    }

    private void SpawnEffect(Vector3 explosionPosition)
    {
        if (explosionEffectPrefab == null) return;

        GameObject effectInstance = Instantiate(
            explosionEffectPrefab,
            explosionPosition,
            Quaternion.identity
        );

        if (effectLifetime > 0f)
            Destroy(effectInstance, effectLifetime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
