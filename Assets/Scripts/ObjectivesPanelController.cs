using System.Collections.Generic;
using UnityEngine;

public class ObjectivesPanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform contentParent;       // le Content du ScrollView
    [SerializeField] private ObjectiveRowView rowPrefab;    // ton prefab de ligne

    private readonly Dictionary<string, ObjectiveRowView> rowsById = new();

    private void OnEnable()
    {
        // garantit que le manager existe (au cas où le bootstrap n'a pas encore tourné)
        ObjectiveManager.EnsureExists();

        BuildList();
        
        // (Bloc 5) On pourra s'abonner ici pour live update
        ObjectiveManager.Instance.OnObjectiveUpdated += HandleObjectiveUpdated;
    }

    private void OnDisable()
    {
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnObjectiveUpdated -= HandleObjectiveUpdated;
    }

    private void BuildList()
    {
        // 1) Clear
        rowsById.Clear();
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        // 2) Rebuild
        foreach (var def in ObjectiveManager.Instance.Objectives)
        {
            var prog = ObjectiveManager.Instance.GetProgress(def.Id);

            var row = Instantiate(rowPrefab, contentParent);
            row.Bind(def, prog);

            rowsById[def.Id] = row;
        }
    }

    private void HandleObjectiveUpdated(ObjectiveManager.ObjectiveDefinition def, ObjectiveManager.ObjectiveProgress prog)
    {
        // Si le panel est ouvert, on met à jour la ligne concernée
        if (rowsById.TryGetValue(def.Id, out var row))
        {
            row.Bind(def, prog);
        }
        // Si jamais la ligne n'existe pas (rare), on rebuild tout
        else
        {
            BuildList();
        }
    }
}