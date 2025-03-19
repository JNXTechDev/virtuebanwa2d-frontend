using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtueBanwa.Dialogue
{
    // Dialogue system types
    [Serializable]
    public class DialogueReward
    {
        public string type;  // OneStar, TwoStar, ThreeStar, etc.
        public string message;
        public string sprite;
        public int score;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text;
        public string response;
        public DialogueReward reward;
    }

    [Serializable]
    public class NPCDialogue
    {
        public string npcName;
        public string title;
        public string subtitle;
        public string initialNarration;
        public string initialDialogue;
        public List<DialogueChoice> choices;
        public string lessonLearned;
        public string instruction1;
        public string instruction2;
        public string lastDialogue;
    }

    [Serializable]
    public class DialogueData
    {
        public string dialogueId;
        public string npcName;
        public string title;
        public string content;
        public List<DialogueChoice> choices;
    }
}

// Progress data types in separate namespace to avoid conflicts
namespace VirtueBanwa.Progress
{
    [Serializable]
    public class GameProgressLessonData
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
}
