using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LevelTransitionController : MonoBehaviour
{
    [Header("🔗 Items & States")]
    [Tooltip("Objets à déplacer/rotater pendant la transition.")]
    public Transform[] levelItems;

    [Tooltip("Transformées cibles pour l'état ON.")]
    public Transform[] stateOnTransforms;

    [Tooltip("Transformées cibles pour l'état OFF.")]
    public Transform[] stateOffTransforms;

    [Header("💥 Destruction d'objets à l'état ON")]
    public Transform[] itemToDestroy;

    [Header("⚙️ Transition Settings")]
    [Range(0.05f, 2f)] public float transitionDuration = 0.3f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("🧪 Debug / Preview")]
    [Range(0f, 1f)] public float previewValue = 0f;
    public bool previewStateOn = false;
    public bool livePreview = false;

    public Action onRestore;

    private bool isAnimating = false;
    private float currentTime = 0f;
    private bool currentTargetState = false; // false = off, true = on
    
    private void FixedUpdate()
    {

        if (!isAnimating) return;

        currentTime += Time.deltaTime;
        float t = Mathf.Clamp01(currentTime / transitionDuration);
        float curveValue = transitionCurve.Evaluate(t);

        for (int i = 0; i < levelItems.Length; i++)
        {
            if (levelItems[i] == null) continue;
            levelItems[i].transform.localPosition = Vector3.LerpUnclamped(stateOffTransforms[i].localPosition, stateOnTransforms[i].localPosition, curveValue);
            levelItems[i].transform.localRotation = Quaternion.SlerpUnclamped(stateOffTransforms[i].localRotation, stateOnTransforms[i].localRotation, curveValue);
        }

        if (t >= 1f)
        {
          //  FinishTransition();
        }
    }

    public void SetState(bool state)
    {
        if (state == currentTargetState && !isAnimating)
            return;

        currentTargetState = state;
        isAnimating = true;
        currentTime = 0f;

        if (state && itemToDestroy.Length > 0)
            DestroyItemsOnStateOn();
    }

    private void FinishTransition()
    {
        isAnimating = false;

        for (int i = 0; i < levelItems.Length; i++)
        {
            if (!levelItems[i]) continue;
            levelItems[i].localPosition = currentTargetState ? stateOnTransforms[i].localPosition : stateOffTransforms[i].localPosition;
            levelItems[i].localRotation = currentTargetState ? stateOnTransforms[i].localRotation : stateOffTransforms[i].localRotation;
        }
    }

    private void DestroyItemsOnStateOn()
    {
        foreach (var item in itemToDestroy)
        {
            if (item && item.gameObject.activeSelf)
                item.gameObject.SetActive(false);
        }
    }

    public void RestoreDestroyedItems()
    {
        foreach (var item in itemToDestroy)
        {
            if (item)
                item.gameObject.SetActive(true);
        }
        onRestore?.Invoke();
    }

}
