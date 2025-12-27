using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public enum ObjectiveTrigger
    {
        SessionStart,
        LevelCompleted
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
            Description = "Termine 3 niveaux dans une même session pour décrocher un succès.",
            Trigger = ObjectiveTrigger.LevelCompleted,
            Target = 3,
            ResetsDaily = true
        }
    };

    private readonly Dictionary<string, ObjectiveProgress> runtimeProgress = new Dictionary<string, ObjectiveProgress>();

    public static ObjectiveManager AttachTo(GameObject host)
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = host.AddComponent<ObjectiveManager>();
        Instance.Initialize();
        return Instance;
    }

    public IReadOnlyList<ObjectiveDefinition> Objectives => defaultObjectives.AsReadOnly();

    public ObjectiveProgress GetProgress(string objectiveId)
    {
        return runtimeProgress.ContainsKey(objectiveId) ? runtimeProgress[objectiveId] : null;
    }

    public void RegisterSessionStart()
    {
        UpdateObjectives(ObjectiveTrigger.SessionStart, 1);
    }

    public void RegisterLevelCompleted(int levelIndex)
    {
        UpdateObjectives(ObjectiveTrigger.LevelCompleted, 1);
    }

    private void Initialize()
    {
        LoadState();
        EnsureDefaultsExist();
        SaveState();
    }

    private void EnsureDefaultsExist()
    {
        DateTime utcNow = DateTime.UtcNow.Date;
        foreach (ObjectiveDefinition objective in defaultObjectives)
        {
            if (!runtimeProgress.ContainsKey(objective.Id))
            {
                runtimeProgress[objective.Id] = new ObjectiveProgress
                {
                    Id = objective.Id,
                    Progress = 0,
                    Completed = false,
                    LastRefreshTicks = utcNow.Ticks
                };
            }
            else if (objective.ResetsDaily)
            {
                MaybeResetDailyProgress(objective, runtimeProgress[objective.Id], utcNow);
            }
        }
    }

    private void MaybeResetDailyProgress(ObjectiveDefinition definition, ObjectiveProgress progress, DateTime today)
    {
        DateTime lastRefresh = new DateTime(progress.LastRefreshTicks, DateTimeKind.Utc).Date;
        if (lastRefresh != today)
        {
            progress.Progress = 0;
            progress.Completed = false;
            progress.LastRefreshTicks = today.Ticks;
        }
    }

    private void UpdateObjectives(ObjectiveTrigger trigger, int amount)
    {
        DateTime utcNow = DateTime.UtcNow.Date;

        foreach (ObjectiveDefinition objective in defaultObjectives)
        {
            if (objective.Trigger != trigger)
            {
                continue;
            }

            if (!runtimeProgress.ContainsKey(objective.Id))
            {
                runtimeProgress[objective.Id] = new ObjectiveProgress
                {
                    Id = objective.Id,
                    Progress = 0,
                    Completed = false,
                    LastRefreshTicks = utcNow.Ticks
                };
            }

            ObjectiveProgress progress = runtimeProgress[objective.Id];

            if (objective.ResetsDaily)
            {
                MaybeResetDailyProgress(objective, progress, utcNow);
            }

            if (progress.Completed)
            {
                continue;
            }

            progress.Progress = Mathf.Clamp(progress.Progress + amount, 0, objective.Target);
            progress.LastRefreshTicks = utcNow.Ticks;

            if (progress.Progress >= objective.Target)
            {
                progress.Completed = true;
            }

            OnObjectiveUpdated?.Invoke(objective, progress);
        }

        SaveState();
    }

    private void LoadState()
    {
        runtimeProgress.Clear();

        if (!PlayerPrefs.HasKey(StorageKey))
        {
            return;
        }

        string raw = PlayerPrefs.GetString(StorageKey);
        ObjectiveProgressList wrapper = JsonUtility.FromJson<ObjectiveProgressList>(raw);

        if (wrapper?.Items == null)
        {
            return;
        }

        foreach (ObjectiveProgress progress in wrapper.Items)
        {
            runtimeProgress[progress.Id] = progress;
        }
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
