using UnityEngine;

public class ObjectivesPanel : MonoBehaviour
{

    public Transform ObjectivePanel;
    private void OnEnable()
    {
        ObjectiveManager.Instance.OnObjectiveUpdated += HandleUpdate;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnObjectiveUpdated -= HandleUpdate;
    }

    private void RefreshAll()
    {
        foreach (var def in ObjectiveManager.Instance.Objectives)
        {
            var prog = ObjectiveManager.Instance.GetProgress(def.Id);
            // Update UI row: title, desc, prog.Progress/def.Target, completed
        }
    }

    private void HandleUpdate(ObjectiveManager.ObjectiveDefinition def, ObjectiveManager.ObjectiveProgress prog)
    {
        // Update only that row (or call RefreshAll)
    }

    public void ToggleObjectivePanel()
    {
        Debug.Log("ToggleObjectivePanel");
        ObjectivePanel.gameObject.SetActive(!ObjectivePanel.gameObject.activeSelf);
    }
}