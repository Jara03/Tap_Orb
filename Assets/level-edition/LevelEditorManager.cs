using System;
using System.Collections.Generic;
using System.IO;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class LevelEditorManager : MonoBehaviour
{
    private const string StateRootName = "LevelEditorStates";

    private Level level;
    public Camera editorCamera;
    private LevelEditorItem selectedItem;
    private LevelEditorGizmo gizmo;

    private readonly List<GameObject> availablePrefabs = new List<GameObject>();
    private Vector2 paletteScroll;
    private GameObject pendingPrefab;

    private string levelNumberInput = "";
    private string statusMessage = "";
    private float statusMessageTimer;

    private LevelEditorGizmo.GizmoMode gizmoMode = LevelEditorGizmo.GizmoMode.Translate;
    private bool editStateOn;
    private bool previewStateOn;

    private bool isDraggingItem;

    private Transform stateRoot;

    private void Start()
    {
        LevelEditorSession.StartNewLevel();
        
        if (!LevelEditorSession.IsEditorActive)
        {
            Destroy(gameObject);
            return;
        }

        level = FindFirstObjectByType<Level>();
        editorCamera = Camera.main;
        if (editorCamera == null)
        {
            editorCamera = FindFirstObjectByType<Camera>();
        }

        EnsureStateRoot();
        LoadPrefabs();
        PrepareSceneForEditing();
        RefreshItemsFromScene();
        CreateGizmo();

        if (level != null)
        {
            level.isPaused = true;
        }

        InputController inputController = FindFirstObjectByType<InputController>();
        if (inputController != null)
        {
            inputController.enabled = false;
        }
    }

    private void Update()
    {
        if (statusMessageTimer > 0f)
        {
            statusMessageTimer -= Time.unscaledDeltaTime;
            if (statusMessageTimer <= 0f)
            {
                statusMessage = "";
            }
        }

        if (editorCamera == null || gizmo == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (gizmo.IsDragging) return;

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectItem();
        }

        if (Input.GetMouseButton(0) && isDraggingItem && selectedItem != null)
        {
            DragSelectedItem();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDraggingItem = false;
        }
    }

    private void OnGUI()
    {
        if (!LevelEditorSession.IsEditorActive) return;

        DrawEditorControls();
        DrawPalette();
    }

    private void DrawEditorControls()
    {
        GUILayout.BeginArea(new Rect(20, 80, 320, 520), GUI.skin.box);
        GUILayout.Label("Level Editor");

        GUILayout.Space(5);
        GUILayout.Label("Edit State:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(!editStateOn, "State Off", "Button"))
        {
            SetEditState(false);
        }
        if (GUILayout.Toggle(editStateOn, "State On", "Button"))
        {
            SetEditState(true);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label("Gizmo Mode:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(gizmoMode == LevelEditorGizmo.GizmoMode.Translate, "Move", "Button"))
        {
            SetGizmoMode(LevelEditorGizmo.GizmoMode.Translate);
        }
        if (GUILayout.Toggle(gizmoMode == LevelEditorGizmo.GizmoMode.Rotate, "Rotate", "Button"))
        {
            SetGizmoMode(LevelEditorGizmo.GizmoMode.Rotate);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        if (GUILayout.Button(previewStateOn ? "Preview: State On" : "Preview: State Off"))
        {
            previewStateOn = !previewStateOn;
            PreviewState(previewStateOn);
        }

        GUILayout.Space(10);
        DrawSelectedItemControls();

        GUILayout.Space(10);
        GUILayout.Label("Save Level");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Level #", GUILayout.Width(60));
        levelNumberInput = GUILayout.TextField(levelNumberInput, GUILayout.Width(80));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Save Level"))
        {
            SaveLevel();
        }

        if (GUILayout.Button("Exit Editor"))
        {
            ExitEditor();
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(10);
            GUILayout.Label(statusMessage, GUI.skin.box);
        }

        GUILayout.EndArea();
    }

    private void DrawSelectedItemControls()
    {
        if (selectedItem == null)
        {
            GUILayout.Label("No item selected.");
            return;
        }

        GUILayout.Label($"Selected: {selectedItem.name}");

        bool destroyOnState = GUILayout.Toggle(selectedItem.destroyOnStateOn, "Destroy On State On");
        if (destroyOnState != selectedItem.destroyOnStateOn)
        {
            selectedItem.destroyOnStateOn = destroyOnState;
            RefreshLevelData();
        }

        bool isAnimated = GUILayout.Toggle(selectedItem.isAnimated, "Animated");
        if (isAnimated != selectedItem.isAnimated)
        {
            selectedItem.isAnimated = isAnimated;
            RefreshLevelData();
        }

        if (GUILayout.Button("Delete Item"))
        {
            DeleteSelectedItem();
        }
    }

    private void DrawPalette()
    {
        GUILayout.BeginArea(new Rect(20, Screen.height - 140, Screen.width - 40, 120), GUI.skin.box);
        GUILayout.Label("Prefabs");
        paletteScroll = GUILayout.BeginScrollView(paletteScroll, GUILayout.Height(70));
        GUILayout.BeginHorizontal();
        foreach (GameObject prefab in availablePrefabs)
        {
            if (prefab == null) continue;
            if (GUILayout.Button(prefab.name, GUILayout.Width(160), GUILayout.Height(40)))
            {
                SelectPrefabToPlace(prefab);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void EnsureStateRoot()
    {
        GameObject root = GameObject.Find(StateRootName);
        if (root == null)
        {
            root = new GameObject(StateRootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
        }
        stateRoot = root.transform;
    }

    private void LoadPrefabs()
    {
        availablePrefabs.Clear();
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/editable_prefab" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                availablePrefabs.Add(prefab);
            }
        }
#endif
    }

    private void PrepareSceneForEditing()
    {
        if (!LevelEditorSession.IsNewLevel) return;

        if (level == null) return;

        if (level.levelItems != null)
        {
            for (int i = 0; i < level.levelItems.Length; i++)
            {
                if (level.levelItems[i] != null)
                {
                    Destroy(level.levelItems[i]);
                }
            }
        }

        foreach (Transform child in stateRoot)
        {
            Destroy(child.gameObject);
        }

        level.levelItems = Array.Empty<GameObject>();
        level.stateOffTransforms = Array.Empty<Transform>();
        level.stateOnTransforms = Array.Empty<Transform>();
        level.itemToDestroy = Array.Empty<Transform>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
        }

        VacuumAttractor goal = FindFirstObjectByType<VacuumAttractor>();
        if (goal != null)
        {
            Destroy(goal.gameObject);
        }

        ShowStatusMessage("New level: start placing prefabs.");
    }

    private void RefreshItemsFromScene()
    {
        if (level != null && level.levelItems != null)
        {
            for (int i = 0; i < level.levelItems.Length; i++)
            {
                GameObject itemObject = level.levelItems[i];
                if (itemObject == null) continue;
                LevelEditorItem editorItem = itemObject.GetComponent<LevelEditorItem>();
                if (editorItem == null)
                {
                    editorItem = itemObject.AddComponent<LevelEditorItem>();
                }

                if (level.stateOffTransforms != null && level.stateOffTransforms.Length > i)
                {
                    editorItem.stateOffTransform = level.stateOffTransforms[i];
                }
                if (level.stateOnTransforms != null && level.stateOnTransforms.Length > i)
                {
                    editorItem.stateOnTransform = level.stateOnTransforms[i];
                }

                editorItem.destroyOnStateOn = level.itemToDestroy != null && Array.Exists(level.itemToDestroy, t => t == itemObject.transform);
                editorItem.isAnimated = itemObject.GetComponent<AnimatedGameItem>() != null;
                editorItem.EnsureStateTransforms(stateRoot);
            }
        }

        foreach (LevelEditorItem editorItem in FindObjectsOfType<LevelEditorItem>())
        {
            editorItem.EnsureStateTransforms(stateRoot);
        }
    }

    private void CreateGizmo()
    {
        GameObject gizmoObject = new GameObject("LevelEditorGizmo");
        gizmo = gizmoObject.AddComponent<LevelEditorGizmo>();
        gizmo.sceneCamera = editorCamera;
        gizmo.mode = gizmoMode;
        gizmo.onTargetModified = HandleGizmoModified;
        gizmoObject.SetActive(false);
    }

    private void SetEditState(bool stateOn)
    {
        editStateOn = stateOn;
        if (selectedItem != null)
        {
            Transform stateTransform = editStateOn ? selectedItem.stateOnTransform : selectedItem.stateOffTransform;
            gizmo.SetTarget(stateTransform);
            ApplyStateTransformToItem();
        }
    }

    private void SetGizmoMode(LevelEditorGizmo.GizmoMode mode)
    {
        gizmoMode = mode;
        if (gizmo != null)
        {
            gizmo.mode = gizmoMode;
        }
    }

    private void TrySelectItem()
    {
        Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            LevelEditorItem item = hit.collider.GetComponentInParent<LevelEditorItem>();
            if (item != null)
            {
                SelectItem(item);
                isDraggingItem = true;
                return;
            }
        }

        if (pendingPrefab != null)
        {
            PlacePendingPrefab();
        }
    }

    private void DragSelectedItem()
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 point = ray.GetPoint(enter);
            Transform target = editStateOn ? selectedItem.stateOnTransform : selectedItem.stateOffTransform;
            target.position = point;
            ApplyStateTransformToItem();
        }
    }

    private void SelectPrefabToPlace(GameObject prefab)
    {
        pendingPrefab = prefab;
        ShowStatusMessage($"Place {prefab.name}: click in the scene.");
    }

    private void PlacePendingPrefab()
    {
       // Debug.Log("placing prefab");
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
        Debug.Log("placing prefab : " + pendingPrefab);
        Debug.Log("mouse position : " + Input.mousePosition);

        if (plane.Raycast(ray, out float enter)) return;

        Vector3 spawnPosition = ray.GetPoint(enter);
        spawnPosition.z = 0;
        GameObject instance = Instantiate(pendingPrefab, spawnPosition, Quaternion.identity);
        LevelEditorItem item = instance.GetComponent<LevelEditorItem>();
        if (item == null)
        {
            item = instance.AddComponent<LevelEditorItem>();
        }
        item.EnsureStateTransforms(stateRoot);
        SelectItem(item);
        RefreshLevelData();
        pendingPrefab = null;
    }

    private void SelectItem(LevelEditorItem item)
    {
        selectedItem = item;
        Transform stateTransform = editStateOn ? item.stateOnTransform : item.stateOffTransform;
        gizmo.SetTarget(stateTransform);
        ApplyStateTransformToItem();
    }

    private void ApplyStateTransformToItem()
    {
        if (selectedItem == null) return;
        Transform stateTransform = editStateOn ? selectedItem.stateOnTransform : selectedItem.stateOffTransform;
        if (stateTransform == null) return;

        selectedItem.transform.SetPositionAndRotation(stateTransform.position, stateTransform.rotation);
        RefreshLevelData();
    }

    private void HandleGizmoModified()
    {
        ApplyStateTransformToItem();
    }

    private void PreviewState(bool stateOn)
    {
        RefreshLevelData();
        if (level != null)
        {
            level.SetTransformState(stateOn);
        }
    }

    private void RefreshLevelData()
    {
        if (level == null) return;

        List<GameObject> items = new List<GameObject>();
        List<Transform> stateOff = new List<Transform>();
        List<Transform> stateOn = new List<Transform>();
        List<Transform> toDestroy = new List<Transform>();

        foreach (LevelEditorItem editorItem in FindObjectsOfType<LevelEditorItem>())
        {
            if (editorItem == null) continue;
            if (editorItem.stateOffTransform == null || editorItem.stateOnTransform == null) continue;

            if (editorItem.destroyOnStateOn)
            {
                toDestroy.Add(editorItem.transform);
            }

            if (editorItem.isAnimated)
            {
                EnsureAnimatedComponent(editorItem);
                continue;
            }

            RemoveAnimatedComponent(editorItem);
            items.Add(editorItem.gameObject);
            stateOff.Add(editorItem.stateOffTransform);
            stateOn.Add(editorItem.stateOnTransform);
        }

        level.levelItems = items.ToArray();
        level.stateOffTransforms = stateOff.ToArray();
        level.stateOnTransforms = stateOn.ToArray();
        level.itemToDestroy = toDestroy.ToArray();
    }

    private void EnsureAnimatedComponent(LevelEditorItem editorItem)
    {
        AnimatedGameItem animated = editorItem.GetComponent<AnimatedGameItem>();
        if (animated == null)
        {
            animated = editorItem.gameObject.AddComponent<AnimatedGameItem>();
        }
        animated.stateOffTransform = editorItem.stateOffTransform;
        animated.stateOnTransform = editorItem.stateOnTransform;
    }

    private void RemoveAnimatedComponent(LevelEditorItem editorItem)
    {
        AnimatedGameItem animated = editorItem.GetComponent<AnimatedGameItem>();
        if (animated != null)
        {
            Destroy(animated);
        }
    }

    private void DeleteSelectedItem()
    {
        if (selectedItem == null) return;
        if (selectedItem.stateOffTransform != null)
        {
            Destroy(selectedItem.stateOffTransform.gameObject);
        }
        if (selectedItem.stateOnTransform != null)
        {
            Destroy(selectedItem.stateOnTransform.gameObject);
        }
        Destroy(selectedItem.gameObject);
        selectedItem = null;
        gizmo.SetTarget(null);
        RefreshLevelData();
    }

    private void SaveLevel()
    {
        RefreshLevelData();

        if (!HasPlayer())
        {
            ShowStatusMessage("Missing player (tag: Player).");
            return;
        }

        if (!HasGoal())
        {
            ShowStatusMessage("Missing goal item (arrivee).");
            return;
        }

        if (!int.TryParse(levelNumberInput, out int levelNumber) || levelNumber <= 0)
        {
            ShowStatusMessage("Enter a valid level number.");
            return;
        }

#if UNITY_EDITOR
        string directory = "Assets/Scenes/Levels";
        Directory.CreateDirectory(directory);
        string scenePath = $"{directory}/Level {levelNumber}.unity";
        bool saved = EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
        if (saved)
        {
            AssetDatabase.Refresh();
            UpdateLevelCount(levelNumber);
            ShowStatusMessage($"Saved Level {levelNumber}.");
        }
        else
        {
            ShowStatusMessage("Failed to save scene.");
        }
#else
        ShowStatusMessage("Scene saving is only available in the editor.");
#endif
    }

    private void UpdateLevelCount(int levelNumber)
    {
        int newCount = Mathf.Max(LevelManager.lvlCount, levelNumber);
        LevelManager.SetLevelCount(newCount);
    }

    private bool HasPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null;
    }

    private bool HasGoal()
    {
        if (FindFirstObjectByType<VacuumAttractor>() != null)
        {
            return true;
        }

        foreach (LevelEditorItem editorItem in FindObjectsOfType<LevelEditorItem>())
        {
            if (editorItem != null && editorItem.name.ToLowerInvariant().Contains("arrivee"))
            {
                return true;
            }
        }

        return false;
    }

    private void ExitEditor()
    {
        LevelEditorSession.EndEditor();
        LevelManager.goBackHome();
    }

    private void ShowStatusMessage(string message)
    {
        statusMessage = message;
        statusMessageTimer = 5f;
    }
}
