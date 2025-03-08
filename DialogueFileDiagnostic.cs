using UnityEngine;
using VirtueBanwa;

public class DialogueFileDiagnostic : MonoBehaviour
{
    [Header("Input")]
    public TextAsset dialogueFile;
    
    [Header("Output")]
    public string npcName;
    public string initialDialogue;
    public int choiceCount;
    public string[] choiceTexts;
    public string[] choiceResponses;
    public string[] rewardSprites;
    
    public void ValidateFile()
    {
        if (dialogueFile == null)
        {
            Debug.LogError("No dialogue file assigned!");
            return;
        }
        
        try
        {
            DialogueData data = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            if (data == null)
            {
                Debug.LogError("Failed to parse dialogue file!");
                return;
            }
            
            npcName = data.npcName;
            initialDialogue = data.initialDialogue;
            
            if (data.choices != null)
            {
                choiceCount = data.choices.Length;
                choiceTexts = new string[choiceCount];
                choiceResponses = new string[choiceCount];
                rewardSprites = new string[choiceCount];
                
                for (int i = 0; i < choiceCount; i++)
                {
                    choiceTexts[i] = data.choices[i].text;
                    choiceResponses[i] = data.choices[i].response;
                    rewardSprites[i] = data.choices[i].reward.sprite;
                }
            }
            
            Debug.Log($"Dialogue file {dialogueFile.name} is valid.");
            Debug.Log($"NPC Name: {npcName}");
            Debug.Log($"Initial Dialogue: {initialDialogue}");
            Debug.Log($"Choices: {choiceCount}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error validating dialogue file: {ex.Message}");
        }
    }
}
