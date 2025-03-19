using System;
using System.Collections.Generic;
using VirtueBanwa.Progress;

[Serializable]
public class GameProgressData
{
    public string Username;
    
    public TutorialData tutorial;
    
    public Dictionary<string, UnitData> units;
    
    public string currentUnit;
    
    public string currentLesson;
}

[Serializable]
public class TutorialData
{
    public string status;
    public string reward;
    public DateTime? date;
    public Dictionary<string, CheckpointData> checkpoints;
}

[Serializable]
public class CheckpointData
{
    public string status;
    public string reward;
    public DateTime date;
    public string message;
}

[Serializable]
public class UnitData
{
    public string status;
    public int completedLessons;
    public int unitScore;
    public Dictionary<string, LessonProgressData> lessons;
    
    // Add postTest property to fix DynamicTable references
    public PostTestData postTest;
}

[Serializable]
public class PostTestData
{
    public string status;
    public int score;
    public DateTime? completionDate;
    public string reward;
}

[Serializable]
public class LessonProgressData // Renamed to avoid conflicts
{
    public string status;
    public string reward;
    public int score;
    public DateTime? lastAttempt;
    public List<string> npcsTalkedTo;
    public Dictionary<string, NpcRewardData> rewards;
}

[Serializable]
public class NpcRewardData
{
    public string type;
    public string message;
    public int score;
}
