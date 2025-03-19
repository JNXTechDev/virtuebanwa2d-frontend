#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VirtueBanwa.Dialogue;

public class DialogueEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private NPCDialogue currentDialogue;
    private string[] npcNames = { "Janica", "Mark", "Annie", "Rojan" };
    private int selectedNpcIndex = 0;
    
    [MenuItem("Virtue Banwa/Dialogue Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogueEditor>("Dialogue Editor");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Virtue Banwa Dialogue Editor", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("NPC:", GUILayout.Width(50));
        selectedNpcIndex = EditorGUILayout.Popup(selectedNpcIndex, npcNames, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        if (currentDialogue == null)
        {
            currentDialogue = new NPCDialogue();
            currentDialogue.npcName = npcNames[selectedNpcIndex];
            currentDialogue.choices = new List<DialogueChoice>();
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
        currentDialogue.npcName = EditorGUILayout.TextField("NPC Name:", currentDialogue.npcName);
        currentDialogue.title = EditorGUILayout.TextField("Title:", currentDialogue.title);
        currentDialogue.subtitle = EditorGUILayout.TextField("Subtitle:", currentDialogue.subtitle);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue Content", EditorStyles.boldLabel);
        currentDialogue.initialNarration = EditorGUILayout.TextArea(currentDialogue.initialNarration, GUILayout.Height(50));
        currentDialogue.initialDialogue = EditorGUILayout.TextArea(currentDialogue.initialDialogue, GUILayout.Height(50));
        currentDialogue.lessonLearned = EditorGUILayout.TextField("Lesson Learned:", currentDialogue.lessonLearned);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue Choices", EditorStyles.boldLabel);
        
        if (currentDialogue.choices == null)
        {
            currentDialogue.choices = new List<DialogueChoice>();
        }
        
        for (int i = 0; i < currentDialogue.choices.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Choice {i + 1}");
            
            DialogueChoice choice = currentDialogue.choices[i];
            
            choice.text = EditorGUILayout.TextField("Text:", choice.text);
            choice.response = EditorGUILayout.TextArea(choice.response, GUILayout.Height(50));
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Reward");
            
            if (choice.reward == null)
            {
                choice.reward = new DialogueReward();
            }
            
            choice.reward.type = EditorGUILayout.TextField("Type:", choice.reward.type);
            choice.reward.message = EditorGUILayout.TextField("Message:", choice.reward.message);
            choice.reward.sprite = EditorGUILayout.TextField("Sprite:", choice.reward.sprite);
            choice.reward.score = EditorGUILayout.IntField("Score:", choice.reward.score);
            
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Remove Choice"))
            {
                currentDialogue.choices.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        if (GUILayout.Button("Add Choice"))
        {
            currentDialogue.choices.Add(new DialogueChoice());
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate C# Code"))
        {
            GenerateCode();
        }
    }
    
    private void GenerateCode()
    {
        string code = "private NPCDialogue Create" + currentDialogue.npcName + "Dialogue()\n{\n";
        code += "    NPCDialogue dialogue = new NPCDialogue\n    {\n";
        code += $"        npcName = \"{currentDialogue.npcName}\",\n";
        code += $"        title = \"{currentDialogue.title}\",\n";
        code += $"        subtitle = \"{currentDialogue.subtitle}\",\n";
        code += $"        initialNarration = \"{currentDialogue.initialNarration}\",\n";
        code += $"        initialDialogue = \"{currentDialogue.initialDialogue}\",\n";
        code += $"        lessonLearned = \"{currentDialogue.lessonLearned}\",\n";
        code += "        choices = new List<DialogueChoice>\n        {\n";
        
        foreach (var choice in currentDialogue.choices)
        {
            code += "            new DialogueChoice\n            {\n";
            code += $"                text = \"{choice.text}\",\n";
            code += $"                response = \"{choice.response}\",\n";
            if (choice.reward != null)
            {
                code += "                reward = new DialogueReward\n                {\n";
                code += $"                    type = \"{choice.reward.type}\",\n";
                code += $"                    message = \"{choice.reward.message}\",\n";
                code += $"                    sprite = \"{choice.reward.sprite}\",\n";
                code += $"                    score = {choice.reward.score}\n";
                code += "                }\n";
            }
            code += "            },\n";
        }
        
        code += "        }\n    };\n\n";
        code += "    return dialogue;\n}";
        
        EditorGUIUtility.systemCopyBuffer = code;
        Debug.Log("Code copied to clipboard!");
    }
}
#endif
