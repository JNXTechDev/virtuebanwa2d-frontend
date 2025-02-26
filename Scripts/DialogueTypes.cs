using UnityEngine;
using System;

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
    }

    [Serializable]
    public class Choice
    {
        public string text;
        public string response;
        public Reward reward;
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
}
