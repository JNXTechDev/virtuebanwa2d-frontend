using System;
using System.Collections.Generic;
using UnityEngine;
using VirtueBanwa.Dialogue;

// This is just a utility class to work with dialogue data
public class DialogueDataUtility 
{
    // Method to convert between DialogueData and NPCDialogue
    public static NPCDialogue ConvertToNPCDialogue(DialogueData data)
    {
        if (data == null) return null;
        
        return new NPCDialogue
        {
            npcName = data.npcName,
            title = data.title,
            initialDialogue = data.content,
            choices = data.choices,
            subtitle = "",
            initialNarration = "",
            lessonLearned = ""
        };
    }
    
    // Method to create a DialogueData object
    public static DialogueData CreateDialogueData(string npcName, string title, string content, List<DialogueChoice> choices = null)
    {
        return new DialogueData
        {
            dialogueId = $"{npcName}_{Guid.NewGuid().ToString().Substring(0, 8)}",
            npcName = npcName,
            title = title,
            content = content,
            choices = choices ?? new List<DialogueChoice>()
        };
    }
}
