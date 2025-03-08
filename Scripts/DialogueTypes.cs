using UnityEngine;
using System;
using System.Collections.Generic;

namespace VirtueBanwa
{
    [Serializable]
    public class DialogueData
    {
        public string npcName;
        public string initialDialogue;
        public Choice[] choices;
        public string title;
        public string subtitle;
        public string setting;
        public string initialNarration;
        public string npcResponse;
        public string narrationPrompt;
        public string lessonLearned;
        public string id;
        public string sceneToLoad; // Add this field

        // Change this method to be virtual so it can be overridden
        public virtual void ClearData()
        {
            npcName = "";
            initialDialogue = "";
            lessonLearned = "";
            choices = new Choice[0];
        }
    }

    [Serializable]
    public class Choice
    {
        public string text;
        public string response;
        public Reward reward;
        public string sceneToLoad; // Add this field for scene transitions
    }

    [Serializable]
    public class Reward
    {
        public string type;
        public string message;
        public string sprite;
        public int score;  // Add score field
    }

    [Serializable]
    public class LessonData
    {
        public string title;
        public string subtitle;
        public string initialNarration;
        public DialogueData dialogue;
        public Choice[] choices;
    }

    [Serializable]
    public class RewardChoice
    {
        public string choiceName;
        public string congratsMessage;
        public UnityEngine.Sprite rewardSprite;
        public string rewardText;
    }

    [Serializable]
    public class LessonProgress
    {
        public string status;
        public string reward;
        public int score;
        public DateTime lastAttempt;
        public List<string> npcsTalkedTo; // Add this field
    }
}
