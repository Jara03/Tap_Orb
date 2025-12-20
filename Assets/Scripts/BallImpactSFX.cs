using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class BallImpactSFX : MonoBehaviour
{
    public float minImpact = 10f;
    public float maxImpact = 20f;

    public float minVolume = 0.05f;
    public float maxVolume = 0.8f;

    public float minPitch = 0.8f;
    public float maxPitch = 1.5f;
    
    float lastImpactTime;
    public float impactCooldown = 0.05f;


    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        audioSource.ignoreListenerPause = true;

        audioSource.PlayOneShot(audioSource.clip);

        Debug.Log(audioSource.clip);
        

    }

    void OnCollisionEnter(Collision collision)
    {
        
        if (Time.time - lastImpactTime < impactCooldown)
            return;

        lastImpactTime = Time.time;

        float impactForce = collision.relativeVelocity.magnitude;

        // Ignore micro-collisions
        if (impactForce < minImpact)
            return;

        float t = Mathf.InverseLerp(minImpact, maxImpact, impactForce);

        audioSource.volume = Mathf.Lerp(minVolume, maxVolume, t);
        audioSource.pitch = Mathf.Lerp(minPitch,  maxPitch,  t);
        audioSource.pitch *= Random.Range(0.95f, 1.05f);

        audioSource.PlayOneShot(audioSource.clip);
        Debug.Log("PlayedSound");
    }
}