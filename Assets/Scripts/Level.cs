using System;
using System.Collections;
using UnityEngine;

namespace DefaultNamespace
{
    public class Level : MonoBehaviour
    {
        public GameObject[] levelItems;
        public Transform[] stateOnTransforms;
        public Transform[] stateOffTransforms;
        
        public Transform[] itemToDestroy;
        [SerializeField]
        private float transitionDuration = 0.3f;

        [SerializeField]
        private AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        //Action si les itemDestroyed sont restore
        
        public Action onRestore = null ;
 
        private Coroutine transitionCoroutine;
        public bool isPaused = false;
        
        public int togglesUsedThisRun = 0;

        private void Start()
        {
            togglesUsedThisRun = 0;
        }
        
        public void SetTransformState(bool state)
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(AnimateTransition(state));

            if (state && itemToDestroy.Length > 0)
                destroyOnStateOn();
            
        }


        public void destroyOnStateOn()
        {
            for (int i = 0; i < itemToDestroy.Length; i++)
            {
                if(itemToDestroy[i] != null && itemToDestroy[i].gameObject.activeSelf)
                    itemToDestroy[i].gameObject.SetActive(false);

            }
            
        }

        public void RestoreDestroyedItems()
        {
            for (int i = 0; i < itemToDestroy.Length; i++)
            {
                if(itemToDestroy[i] != null)
                    itemToDestroy[i].gameObject.SetActive(true);
            }
            if (onRestore != null)
            {
                onRestore();
            }
        }
        private IEnumerator AnimateTransition(bool state)
        {

            Vector3[] startPositions = new Vector3[levelItems.Length];
            Quaternion[] startRotations = new Quaternion[levelItems.Length];
            Vector3[] targetPositions = new Vector3[levelItems.Length];
            Quaternion[] targetRotations = new Quaternion[levelItems.Length];

            for (int i = 0; i < levelItems.Length; i++)
            {
                startPositions[i] = levelItems[i].transform.localPosition;
                startRotations[i] = levelItems[i].transform.localRotation;
                targetPositions[i] = state ? stateOnTransforms[i].localPosition : stateOffTransforms[i].localPosition;
                targetRotations[i] = state ? stateOnTransforms[i].localRotation : stateOffTransforms[i].localRotation;
            }

            float elapsedTime = 0f;

            while (elapsedTime <= transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / transitionDuration; //Mathf.Clamp01(elapsedTime / transitionDuration);
                float curveValue = transitionCurve.Evaluate(normalizedTime); //Mathf.Clamp01(transitionCurve.Evaluate(normalizedTime));
                for (int i = 0; i < levelItems.Length; i++)
                {
                    if (levelItems[i] == null) continue;
                    //levelItems[i].transform.SetPositionAndRotation(Vector3.LerpUnclamped(startPositions[i], targetPositions[i],curveValue), Quaternion.SlerpUnclamped(startRotations[i], targetRotations[i],curveValue));
                    levelItems[i].transform.GetComponent<Rigidbody>().MovePosition(Vector3.LerpUnclamped(startPositions[i], targetPositions[i],curveValue));
                    levelItems[i].transform.GetComponent<Rigidbody>().MoveRotation(Quaternion.SlerpUnclamped(startRotations[i], targetRotations[i],curveValue));
                    
                }

                yield return new WaitForFixedUpdate();
            }

            // 🔒 Forcer la position finale à la fin
            ApplyImmediateState(state);
        }

        private void ApplyImmediateState(bool state)
        {
            for (int i = 0; i < levelItems.Length; i++)
            {
                levelItems[i].transform.localPosition = state ? stateOnTransforms[i].localPosition : stateOffTransforms[i].localPosition;
                levelItems[i].transform.localRotation = state ? stateOnTransforms[i].localRotation : stateOffTransforms[i].localRotation;
            }
        }

    }
}
