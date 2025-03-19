using System;
using System.Collections.Generic;
using VirtueBanwa.Progress; // Add reference to existing progress types

[Serializable]
public class GameProgress
{
    public string Username;
    public TutorialProgress tutorial;
    public Dictionary<string, UnitProgress> units;
}

[Serializable]
public class TutorialProgress
{
    public string status;
    public string reward;
    public DateTime date;
    public Dictionary<string, CheckpointData> checkpoints;
}

[Serializable]
public class UnitProgress
{
    public string status;
    public Dictionary<string, LessonProgress> lessons;
}

[Serializable]
public class LessonProgress
{
    public string status;
    public string reward;
    public List<string> npcsTalkedTo;
}
