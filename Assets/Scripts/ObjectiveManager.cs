using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)] // optionnel mais pratique: init tôt
public class ObjectiveManager : MonoBehaviour
{
    public enum ObjectiveTrigger
    {
        SessionStart,
        LevelCompleted,
        ToggleUsed,
        LevelCompletedClean, // complétion “propre”

    }

    [Serializable]
    public class ObjectiveDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public ObjectiveTrigger Trigger;
        public int Target;
        public bool ResetsDaily;
        public bool ResetsOnSessionStart;

    }

    [Serializable]
    public class ObjectiveProgress
    {
        public string Id;
        public int Progress;
        public bool Completed;
        public long LastRefreshTicks;
    }

    [Serializable]
    private class ObjectiveProgressList
    {
        public List<ObjectiveProgress> Items = new List<ObjectiveProgress>();
    }

    private const string StorageKey = "ObjectivesState_v1";

    public static ObjectiveManager Instance { get; private set; }

    public event Action<ObjectiveDefinition, ObjectiveProgress> OnObjectiveUpdated;

    private bool initialized = false;

    private readonly List<ObjectiveDefinition> defaultObjectives = new List<ObjectiveDefinition>
    {
        new ObjectiveDefinition
        {
            Id = "session_welcome",
            Title = "Reviens aujourd'hui",
            Description = "Lance une partie chaque jour pour garder la dynamique.",
            Trigger = ObjectiveTrigger.SessionStart,
            Target = 1,
            ResetsDaily = true
        },
        new ObjectiveDefinition
        {
            Id = "first_clear",
            Title = "Premier succès",
            Description = "Termine n'importe quel niveau et débloque ton premier badge.",
            Trigger = ObjectiveTrigger.LevelCompleted,
            Target = 1,
            ResetsDaily = false
        },
        new ObjectiveDefinition
        {
            Id = "triple_clear",
            Title = "Enchaîne les victoires",
            Description = "Termine 3 niveaux aujourd'hui pour décrocher un succès.",
            Trigger = ObjectiveTrigger.LevelCompleted,
            Target = 3,
            ResetsDaily = true
        },
        // Nouveau 4
        new ObjectiveDefinition
        {
            Id = "toggle_spree",
            Title = "Toggle mania",
            Description = "Active 25 toggles aujourd'hui.",
            Trigger = ObjectiveTrigger.ToggleUsed,
            Target = 25,
            ResetsDaily = true
        },
        new ObjectiveDefinition
        {
            Id = "clean_run",
            Title = "Maîtrise",
            Description = "Termine un niveau en utilisant 3 toggles ou moins.",
            Trigger = ObjectiveTrigger.LevelCompletedClean,
            Target = 1,
            ResetsDaily = true
        }
    };

    private readonly Dictionary<string, ObjectiveProgress> runtimeProgress = new Dictionary<string, ObjectiveProgress>();

    /// <summary>
    /// Garantit qu'il existe une instance (crée un GO si nécessaire).
    /// À appeler depuis n'importe où (UI, bootstrap, etc.)
    /// </summary>
    public static ObjectiveManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        // Si un ObjectiveManager est déjà présent dans la scène (mais Instance pas encore set)
        var existing = FindFirstObjectByType<ObjectiveManager>();
        if (existing != null)
        {
            Instance = existing;
            Instance.InitializeIfNeeded();
            return Instance;
        }

        // Sinon on en crée un
        var go = new GameObject("[ObjectiveManager]");
        Instance = go.AddComponent<ObjectiveManager>();
        DontDestroyOnLoad(go);
        Instance.InitializeIfNeeded();
        return Instance;
    }

    /// <summary>
    /// Option: attacher le manager à un host spécifique (ton Bootstrap).
    /// </summary>
    public static ObjectiveManager AttachTo(GameObject host)
    {
        if (Instance != null)
            return Instance;

        Instance = host.GetComponent<ObjectiveManager>();
        if (Instance == null)
            Instance = host.AddComponent<ObjectiveManager>();

        DontDestroyOnLoad(host);
        Instance.InitializeIfNeeded();
        return Instance;
    }

    private void Awake()
    {
        // Singleton anti-doublon (si tu charges plusieurs scènes avec un bootstrap par erreur)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    public IReadOnlyList<ObjectiveDefinition> Objectives => defaultObjectives.AsReadOnly();

    public ObjectiveProgress GetProgress(string objectiveId)
    {
        return runtimeProgress.TryGetValue(objectiveId, out var p) ? p : null;
    }

    public void RegisterSessionStart()
    {
        InitializeIfNeeded();
        UpdateObjectives(ObjectiveTrigger.SessionStart, 1);
    }

    public void RegisterLevelCompleted(int levelIndex)
    {
        InitializeIfNeeded();
        UpdateObjectives(ObjectiveTrigger.LevelCompleted, 1);
    }

    public void RegisterLevelCompletedClean(int levelIndex)
    {
        InitializeIfNeeded();
        UpdateObjectives(ObjectiveTrigger.LevelCompletedClean, 1);
    }

    public void RegisterToggleUsed()
    {
        InitializeIfNeeded();
        UpdateObjectives(ObjectiveTrigger.ToggleUsed, 1);
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        initialized = true;

        LoadState();
        EnsureDefaultsExist();
        SaveState();
    }

    private void EnsureDefaultsExist()
    {
        DateTime utcToday = DateTime.UtcNow.Date;
        foreach (ObjectiveDefinition objective in defaultObjectives)
        {
            if (!runtimeProgress.ContainsKey(objective.Id))
            {
                runtimeProgress[objective.Id] = new ObjectiveProgress
                {
                    Id = objective.Id,
                    Progress = 0,
                    Completed = false,
                    LastRefreshTicks = utcToday.Ticks
                };
            }
            else if (objective.ResetsDaily)
            {
                MaybeResetDailyProgress(runtimeProgress[objective.Id], utcToday);
            }
        }
    }

    private void MaybeResetDailyProgress(ObjectiveProgress progress, DateTime utcToday)
    {
        DateTime lastRefresh = new DateTime(progress.LastRefreshTicks, DateTimeKind.Utc).Date;
        if (lastRefresh != utcToday)
        {
            progress.Progress = 0;
            progress.Completed = false;
            progress.LastRefreshTicks = utcToday.Ticks;
        }
    }

    private void UpdateObjectives(ObjectiveTrigger trigger, int amount)
    {
        DateTime utcToday = DateTime.UtcNow.Date;

        foreach (ObjectiveDefinition objective in defaultObjectives)
        {
            if (objective.Trigger != trigger)
                continue;

            if (!runtimeProgress.ContainsKey(objective.Id))
            {
                runtimeProgress[objective.Id] = new ObjectiveProgress
                {
                    Id = objective.Id,
                    Progress = 0,
                    Completed = false,
                    LastRefreshTicks = utcToday.Ticks
                };
            }

            ObjectiveProgress progress = runtimeProgress[objective.Id];

            if (objective.ResetsDaily)
                MaybeResetDailyProgress(progress, utcToday);

            if (progress.Completed)
                continue;

            progress.Progress = Mathf.Clamp(progress.Progress + amount, 0, objective.Target);
            progress.LastRefreshTicks = utcToday.Ticks;

            if (progress.Progress >= objective.Target)
                progress.Completed = true;

            OnObjectiveUpdated?.Invoke(objective, progress);
        }

        SaveState();
    }

    private void LoadState()
    {
        runtimeProgress.Clear();

        if (!PlayerPrefs.HasKey(StorageKey))
            return;

        string raw = PlayerPrefs.GetString(StorageKey);
        ObjectiveProgressList wrapper = JsonUtility.FromJson<ObjectiveProgressList>(raw);

        if (wrapper?.Items == null)
            return;

        foreach (ObjectiveProgress progress in wrapper.Items)
            runtimeProgress[progress.Id] = progress;
    }

    private void SaveState()
    {
        ObjectiveProgressList wrapper = new ObjectiveProgressList
        {
            Items = new List<ObjectiveProgress>(runtimeProgress.Values)
        };

        string raw = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(StorageKey, raw);
        PlayerPrefs.Save();
    }
}
