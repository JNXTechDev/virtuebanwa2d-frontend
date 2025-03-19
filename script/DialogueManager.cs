using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtueBanwa.Dialogue;

namespace VirtueBanwa.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        // Dictionary to store all NPC dialogues
        private Dictionary<string, NPCDialogue> dialogues = new Dictionary<string, NPCDialogue>();
        
        // Dictionary to track completed dialogues
        private Dictionary<string, bool> completedDialogues = new Dictionary<string, bool>();
        
        public void Reset()
        {
            dialogues.Clear();
            completedDialogues.Clear();
        }
        
        public void AddDialogueForNPC(string npcName, NPCDialogue dialogue)
        {
            dialogues[npcName] = dialogue;
            completedDialogues[npcName] = false;
        }
        
        public NPCDialogue GetDialogueForNPC(string npcName)
        {
            Debug.Log($"DialogueManager: Getting dialogue for NPC: {npcName}");
            
            if (dialogues.TryGetValue(npcName, out NPCDialogue dialogue))
            {
                // Create a deep copy of the choices list
                List<DialogueChoice> choicesCopy = new List<DialogueChoice>();
                if (dialogue.choices != null)
                {
                    foreach (var choice in dialogue.choices)
                    {
                        DialogueChoice choiceCopy = new DialogueChoice
                        {
                            text = choice.text,
                            response = choice.response,
                            reward = choice.reward != null ? new DialogueReward
                            {
                                type = choice.reward.type,
                                message = choice.reward.message,
                                sprite = choice.reward.sprite,
                                score = choice.reward.score
                            } : null
                        };
                        choicesCopy.Add(choiceCopy);
                    }
                }

                // Return a deep copy of the dialogue with non-null strings
                NPCDialogue dialogueCopy = new NPCDialogue
                {
                    npcName = npcName,
                    title = dialogue.title ?? "",
                    subtitle = dialogue.subtitle ?? "",
                    initialNarration = dialogue.initialNarration ?? "",
                    initialDialogue = dialogue.initialDialogue ?? "",
                    instruction1 = dialogue.instruction1 ?? "",
                    instruction2 = dialogue.instruction2 ?? "",
                    lastDialogue = dialogue.lastDialogue ?? "",
                    lessonLearned = dialogue.lessonLearned ?? "",
                    choices = choicesCopy
                };

                Debug.Log($"DialogueManager: Dialogue sequence for {npcName}: " +
                         $"\nNarration: {dialogueCopy.initialNarration}" +
                         $"\nDialogue: {dialogueCopy.initialDialogue}" +
                         $"\nInst1: {dialogueCopy.instruction1}" +
                         $"\nInst2: {dialogueCopy.instruction2}" +
                         $"\nLast: {dialogueCopy.lastDialogue}");

                return dialogueCopy;
            }

            Debug.LogError($"DialogueManager: No dialogue found for NPC: {npcName}");
            return null;
        }
        
        public void MarkDialogueCompleted(string npcName)
        {
            if (completedDialogues.ContainsKey(npcName))
            {
                completedDialogues[npcName] = true;
            }
        }
        
        public bool IsDialogueCompleted(string npcName)
        {
            if (completedDialogues.TryGetValue(npcName, out bool completed))
            {
                return completed;
            }
            return false;
        }
        
        public bool AreAllDialoguesCompleted()
        {
            foreach (var completed in completedDialogues.Values)
            {
                if (!completed)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
