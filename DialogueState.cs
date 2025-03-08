using System.Collections.Generic;
using UnityEngine;

public static class DialogueState
{
    private static HashSet<string> completedDialogues = new HashSet<string>();

    // Load completed dialogues from PlayerPrefs
    static DialogueState()
    {
        string savedDialogues = PlayerPrefs.GetString("CompletedDialogues", "");
        if (!string.IsNullOrEmpty(savedDialogues))
        {
            string[] dialogues = savedDialogues.Split(',');
            foreach (string dialogue in dialogues)
            {
                if (!string.IsNullOrEmpty(dialogue))
                {
                    completedDialogues.Add(dialogue);
                    Debug.Log($"Dialogue completed for {dialogue}");
                }
            }
        }
    }

    // Set dialogue as completed for an NPC
    public static void SetDialogueCompleted(string npcName)
    {
        completedDialogues.Add(npcName);
        Debug.Log($"Dialogue completed for {npcName}");
        SaveToPlayerPrefs();
    }

    // Check if dialogue is completed for an NPC
    public static bool HasCompletedDialogue(string npcName)
    {
        return completedDialogues.Contains(npcName);
    }

    // Save completed dialogues to PlayerPrefs
    private static void SaveToPlayerPrefs()
    {
        string savedDialogues = string.Join(",", completedDialogues);
        PlayerPrefs.SetString("CompletedDialogues", savedDialogues);
        PlayerPrefs.Save();
    }

    // Clear completed dialogues (for testing or resetting)
    public static void ClearCompletedDialogues()
    {
        completedDialogues.Clear();
        PlayerPrefs.DeleteKey("CompletedDialogues");
        PlayerPrefs.Save();
    }

    // Add this method to reset a specific dialogue state
    public static void ResetDialogueState(string npcName)
    {
        if (completedDialogues.Contains(npcName))
        {
            completedDialogues.Remove(npcName);
            Debug.Log($"Dialogue reset for {npcName}");
            SaveToPlayerPrefs();
        }
    }
}
