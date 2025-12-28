using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveRowView : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject checkObject; // Image ou TMP "✓"

    private string objectiveId;

    public string ObjectiveId => objectiveId;

    public void Bind(ObjectiveManager.ObjectiveDefinition def, ObjectiveManager.ObjectiveProgress prog)
    {
        objectiveId = def.Id;

        titleText.text = def.Title;
        descText.text = def.Description;

        int current = prog != null ? prog.Progress : 0;
        bool completed = prog != null && prog.Completed;

        progressText.text = $"{current}/{def.Target}";
        if (checkObject != null) checkObject.SetActive(completed);
    }
}