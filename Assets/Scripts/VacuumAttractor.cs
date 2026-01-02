using System;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class VacuumAttractor : MonoBehaviour
{
    [Header("Aspiration")]
    public float attractionSpeed = 12f;
    public float suctionRadius = 5f;
    public float stopDistance = 0.05f;
    public Transform centerGoal;

    public Action OnEndLevel;

    private Rigidbody playerRb;
    private bool isAttracting;
    private bool endTriggered;

    void Start()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = suctionRadius;

    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerRb = other.attachedRigidbody;
        if (playerRb == null) return;

        playerRb.useGravity = false;
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;

        isAttracting = true;
    }
    void FixedUpdate()
    {
        
        if (!isAttracting || playerRb == null) return;

        Vector3 newPos = Vector3.MoveTowards(
            playerRb.position,
            centerGoal.position,
            attractionSpeed * Time.fixedDeltaTime
        );

        playerRb.MovePosition(newPos);

        float dist = Vector3.Distance(newPos, centerGoal.position);

        // ✅ Fin de niveau UNE SEULE FOIS
        if (!endTriggered && dist <= stopDistance)
        {
            Debug.Log(dist);
            playerRb.gameObject.SetActive(false);
            endTriggered = true;
            Debug.Log("End level : " + endTriggered);
            OnEndLevel?.Invoke();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, suctionRadius);
    }
}
