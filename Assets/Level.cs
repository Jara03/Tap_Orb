using System.Collections;
using UnityEngine;

namespace DefaultNamespace
{
    public class Level : MonoBehaviour
    {
        public GameObject[] levelItems;
        public Transform[] stateOnTransforms;
        public Transform[] stateOffTransforms;

        [SerializeField]
        private float transitionDuration = 0.3f;

        [SerializeField]
        private AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private Coroutine transitionCoroutine;
        
        public void SetTransformState(bool state)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            transitionCoroutine = StartCoroutine(AnimateTransition(state));
        }

        private IEnumerator AnimateTransition(bool state)
        {
            if (transitionDuration <= 0f)
            {
                ApplyImmediateState(state);
                yield break;
            }

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

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / transitionDuration);
                float curveValue = transitionCurve.Evaluate(normalizedTime);

                for (int i = 0; i < levelItems.Length; i++)
                {
                    levelItems[i].transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], curveValue);
                    levelItems[i].transform.localRotation = Quaternion.Lerp(startRotations[i], targetRotations[i], curveValue);
                }

                yield return null;
            }

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
