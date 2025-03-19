using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtueBanwa.Dialogue;

public class QuestManager : MonoBehaviour
{
    // The current quest sequence
    private string questName;
    private string[] questTargets;
    private Dictionary<string, bool> completedTargets = new Dictionary<string, bool>();
    private int currentTargetIndex = 0;
    private Action onQuestCompleted;
    
    // Pending reward information (NPC name, reward)
    private Tuple<string, DialogueReward> pendingReward;
    
    public void Reset()
    {
        questName = null;
        questTargets = null;
        completedTargets.Clear();
        currentTargetIndex = 0;
        onQuestCompleted = null;
        pendingReward = null;
    }
    
    public void CreateQuestSequence(string name, string[] targets, Action completionCallback)
    {
        questName = name;
        questTargets = targets;
        completedTargets.Clear();
        currentTargetIndex = 0;
        onQuestCompleted = completionCallback;
        
        // Initialize completion status for all targets
        foreach (string target in questTargets)
        {
            completedTargets[target] = false;
        }
    }
    
    public string GetNextQuestTarget()
    {
        if (questTargets == null || questTargets.Length == 0 || currentTargetIndex >= questTargets.Length)
        {
            return null;
        }
        
        return questTargets[currentTargetIndex];
    }
    
    public bool IsNpcInCurrentQuest(string npcName)
    {
        if (questTargets == null)
        {
            return false;
        }
        
        return Array.IndexOf(questTargets, npcName) >= 0;
    }
    
    public void MarkNPCCompleted(string npcName)
    {
        if (completedTargets.ContainsKey(npcName))
        {
            completedTargets[npcName] = true;
            
            // Move to the next target if this is the current target
            if (questTargets[currentTargetIndex] == npcName)
            {
                currentTargetIndex++;
                
                // Check if all targets are completed
                if (currentTargetIndex >= questTargets.Length)
                {
                    // Quest is completed!
                    if (onQuestCompleted != null)
                    {
                        onQuestCompleted.Invoke();
                    }
                }
            }
        }
    }
    
    public void SetPendingReward(string npcName, DialogueReward reward)
    {
        pendingReward = new Tuple<string, DialogueReward>(npcName, reward);
    }
    
    public Tuple<string, DialogueReward> GetPendingReward()
    {
        return pendingReward;
    }
    
    public void ClearPendingReward()
    {
        pendingReward = null;
    }

    public void SetCurrentTarget(string npcName)
    {
        // Find the index of the NPC in our targets array
        if (questTargets != null)
        {
            int index = Array.IndexOf(questTargets, npcName);
            if (index >= 0)
            {
                currentTargetIndex = index;
            }
            else
            {
                Debug.LogWarning($"NPC {npcName} not found in quest targets");
            }
        }
    }

    // Add this helper method to get all quest targets
    public string[] GetAllQuestTargets()
    {
        return questTargets;
    }
}
