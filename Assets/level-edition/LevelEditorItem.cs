using UnityEngine;

public class LevelEditorItem : MonoBehaviour
{
    public Transform stateOffTransform;
    public Transform stateOnTransform;
    public bool destroyOnStateOn;
    public bool isAnimated;

    public void EnsureStateTransforms(Transform stateRoot)
    {
        if (stateOffTransform == null)
        {
            stateOffTransform = CreateStateTransform(stateRoot, "StateOff");
            stateOffTransform.SetPositionAndRotation(transform.position, transform.rotation);
        }

        if (stateOnTransform == null)
        {
            stateOnTransform = CreateStateTransform(stateRoot, "StateOn");
            stateOnTransform.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }

    private Transform CreateStateTransform(Transform stateRoot, string suffix)
    {
        GameObject stateObject = new GameObject($"{name}_{suffix}");
        if (stateRoot != null)
        {
            stateObject.transform.SetParent(stateRoot, true);
        }
        stateObject.transform.position = transform.position;
        stateObject.transform.rotation = transform.rotation;
        return stateObject.transform;
    }
}
