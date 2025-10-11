using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedGameItem : MonoBehaviour
{ 
    public Transform stateOnTransform;
    public Transform stateOffTransform;
    public float animationSpeed = 1f;
    bool isPlaying = true;

    private bool currentState = false;
    private Coroutine animationRoutine;

    void Start()
    {
        // Démarre une coroutine qui alterne entre les deux états
        animationRoutine = StartCoroutine(AnimateStateRoutine());
    }

    IEnumerator AnimateStateRoutine()
    {
        while (true)
        {
            if (isPlaying)
            {
                // Inverse l’état actuel
                currentState = !currentState;

                // Lance la transition vers l’état opposé
                yield return StartCoroutine(TransitionToState(currentState));
            }

            // Attend avant de relancer selon la vitesse d’animation
            yield return new WaitForSeconds(animationSpeed);
        }
    }

    IEnumerator TransitionToState(bool state)
    {
        // On capture les positions/rotations en espace global AVANT de bouger quoi que ce soit
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // On lit les positions/rotations globales des transforms de référence (et non leurs locales)
        Vector3 targetPos = state ? stateOnTransform.position : stateOffTransform.position;
        Quaternion targetRot = state ? stateOnTransform.rotation : stateOffTransform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // Fin de la transition — on s’assure d’être exactement sur la cible
        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    

    public void PlayAnimation(bool play)
    {
        isPlaying = play;

        if (!isPlaying && animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
        else if (isPlaying && animationRoutine == null)
        {
            animationRoutine = StartCoroutine(AnimateStateRoutine());
        }
    }
    
}
