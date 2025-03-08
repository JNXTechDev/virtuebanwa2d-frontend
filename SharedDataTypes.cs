using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProgressDataWrapper
{
    public GameProgressData data;
}

[System.Serializable]
public class GameProgressData
{
    public string Username;
    public TutorialProgress tutorial;
    public Dictionary<string, UnitProgress> units;
    public string currentUnit;
    public string currentLesson;
    // Add these for backward compatibility with existing code
    public string unit;
    public string lesson;
    public string reward;
    public string message;

    public static GameProgressData FromDictionary(Dictionary<string, object> dict)
    {
        return JsonUtility.FromJson<GameProgressData>(
            JsonUtility.ToJson(new Dictionary<string, object> { ["data"] = dict })
        );
    }
}

[System.Serializable]
public class UnitProgress
{
    public string status = "Not Started"; // Not Started, In Progress, Completed
    public Dictionary<string, LessonProgress> lessons;
    public PostTestProgress postTest;
    public int completedLessons = 0;
    public float unitScore = 0f;
}

[System.Serializable]
public class LessonProgress
{
    public string status = "Locked"; // Locked, Available, In Progress, Completed
    public string reward;
    public float score = 0f;
    public DateTime lastAttempt;
    public string date; // Added for API compatibility
    public List<string> npcsTalkedTo = new List<string>();
}

[System.Serializable]
public class PostTestProgress
{
    public string status = "Locked"; // Locked, Available, Completed
    public float score = 0f;
    public DateTime? completionDate;
    public string reward;
}

[System.Serializable]
public class TutorialProgress
{
    public string status;
    public string reward;
    public DateTime date;
}

[System.Serializable]
public class UserProgressData
{
    public List<GameProgress> progress;
}

[System.Serializable]
public class GameProgress
{
    public string Username;
    public TutorialProgress tutorial;
    public Dictionary<string, UnitProgress> units;
    public Dictionary<string, LessonProgress> lessons;
}
