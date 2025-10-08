using UnityEngine;

namespace DefaultNamespace
{
    public class Level : MonoBehaviour
    {
        public GameObject[] levelItems;
        public  Transform[] stateOnTransforms;
        public  Transform[] stateOffTransforms;
        
        public void SetTransformState(bool state)
        {
            for (int i = 0; i < levelItems.Length; i++)
            {
                //tout les levelItems se déplacent et tournent dans l'état On ou Off 
                levelItems[i].transform.position = state ? stateOnTransforms[i].localPosition : stateOffTransforms[i].localPosition;
                levelItems[i].transform.rotation = state ? stateOnTransforms[i].localRotation : stateOffTransforms[i].localRotation;
            }
        }

    }
}