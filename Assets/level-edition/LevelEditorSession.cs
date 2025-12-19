using UnityEngine;

public static class LevelEditorSession
{
    public const string DefaultTemplateScenePath = "Scenes/Levels/Level 1";

    public static bool IsEditorActive { get; private set; }
    public static bool IsNewLevel { get; private set; }

    public static void StartNewLevel()
    {
        IsEditorActive = true;
        IsNewLevel = true;
    }

    public static void StartEditingExisting()
    {
        IsEditorActive = true;
        IsNewLevel = false;
    }

    public static void EndEditor()
    {
        IsEditorActive = false;
        IsNewLevel = false;
    }
}
