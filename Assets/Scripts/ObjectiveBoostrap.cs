using UnityEngine;

public class ObjectiveBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // crée le manager si absent
        ObjectiveManager.EnsureExists();
    }

    private void Start()
    {
        // à toi de décider: Start du menu = start de session
        ObjectiveManager.Instance.RegisterSessionStart();
    }
}