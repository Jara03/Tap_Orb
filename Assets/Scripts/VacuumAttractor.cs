using System;
using DefaultNamespace;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class VacuumAttractor : MonoBehaviour
{
    [Header("Paramètres d’aspiration")]
    public float attractionForce = 20f;       // Intensité de la force d’aspiration
    public float suctionRadius = 5f;          // Rayon de la zone d’effet
    public float suctionStopDistance = 0.1f;  // Distance à laquelle on arrête d’aspirer (évite de coller l’objet dans le centre)

    [Header("Collider de la cage")]
    public SphereCollider cageCollider;
    public float cageShrinkSpeed = 10f;        // Vitesse de réduction du rayon
    public float targetCageRadius = 0.75f;       // Taille finale de la cage

    private bool shrinking = false;           // Flag pour indiquer si on est en train de réduire le rayon
    public Action OnEndLevel;

    private void Start()
    {
        // On s’assure que le collider sert de zone d’attraction
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = suctionRadius;

        // On désactive la cage au départ
        if (cageCollider != null)
        {
            cageCollider.enabled = false;
        }
        
    }
    private void OnTriggerStay(Collider other)
    {
        // On vérifie si l’objet dans la zone est le Player
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                Vector3 center = transform.position;
                Vector3 direction = (center - other.transform.position).normalized;
                float distance = Vector3.Distance(center, other.transform.position);

                // Tant qu'il n'est pas au centre, on applique une force d'attraction
                if (distance > suctionStopDistance)
                {
                    float force = attractionForce / Mathf.Max(distance, 0.1f);
                    rb.AddForce(direction * force, ForceMode.Acceleration);
                }

                // Lorsqu'il entre dans la zone interne (< 2.5f)
                if (distance < 2.5f)
                {
                    if (cageCollider != null && !cageCollider.enabled)
                    {
                        Debug.Log("Cage collider enabled");
                        cageCollider.enabled = true;
                        shrinking = true;
                    }

                    // 🔒 Stabilisation au centre pour éviter qu’il traverse la cage
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // On “attache” le joueur au centre de l’aspirateur
                    other.transform.position = Vector3.Lerp(
                        other.transform.position,
                        center,
                        Time.deltaTime * 5f // vitesse de recentrage
                    );

                    // Optionnel : empêche la physique de s’emballer
                    rb.linearDamping = 10f;
                    rb.angularDamping = 5f;
                    
                    if (distance < 0.5f)
                    {
                        Debug.Log("End Level");
                        OnEndLevel?.Invoke();
                    }
                }
                else
                {
                    // On remet la résistance normale quand il n’est plus proche du centre
                  
                    
                    rb.linearDamping = 0f;
                    rb.angularDamping = 0.05f;
                }
            }
        }
    }


    private void Update()
    {
        // Si la cage est activée et qu'on doit la rétrécir
        if (shrinking && cageCollider != null && cageCollider.enabled)
        {
            // Réduit progressivement le rayon
            cageCollider.radius = Mathf.MoveTowards(cageCollider.radius, targetCageRadius, cageShrinkSpeed * Time.deltaTime);

            // Si la cage est arrivée à sa taille finale, on arrête
            if (Mathf.Approximately(cageCollider.radius, targetCageRadius))
            {
                shrinking = false;
                Debug.Log("Cage shrink complete");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualisation de la zone d’aspiration dans l’éditeur
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, suctionRadius);
    }
}
