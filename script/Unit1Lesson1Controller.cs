using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VirtueBanwa.Dialogue; // Add proper namespace

public class Unit1Lesson1Controller : MonoBehaviour
{
    [Header("NPC References")]
    public GameObject teacherNPC;
    public GameObject student1NPC;
    public GameObject student2NPC;
    
    [Header("UI References")]
    public GameObject lessonPanel;
    public TMP_Text lessonTitle;
    public TMP_Text lessonDescription;
    
    private DialogueManager dialogueManager;
    private GameManager gameManager;
    
    void Start()
    {
        // Find required components
        dialogueManager = FindObjectOfType<DialogueManager>();
        gameManager = FindObjectOfType<GameManager>();
        
        if (dialogueManager == null || gameManager == null)
        {
            Debug.LogError("Required components not found!");
            return;
        }
        
        // Hide lesson panel initially
        if (lessonPanel != null)
        {
            lessonPanel.SetActive(false);
        }
        
        // Initialize lesson content
        InitializeLessonContent();
    }
    
    private void InitializeLessonContent()
    {
        if (lessonTitle != null)
        {
            lessonTitle.text = "Unit 1 - Lesson 1";
        }
        
        if (lessonDescription != null)
        {
            lessonDescription.text = "Welcome to your first lesson! Talk to the teacher to begin.";
        }
    }
    
    // Method to show lesson panel
    public void ShowLessonPanel()
    {
        if (lessonPanel != null)
        {
            lessonPanel.SetActive(true);
        }
    }
    
    // Method to hide lesson panel
    public void HideLessonPanel()
    {
        if (lessonPanel != null)
        {
            lessonPanel.SetActive(false);
        }
    }
    
    // Create dialogues for Unit 1 Lesson 1 NPCs
    public NPCDialogue CreateTeacherDialogue()
    {
        NPCDialogue dialogue = new NPCDialogue
        {
            npcName = "Teacher",
            title = "Unit 1 - Lesson 1",
            subtitle = "Introduction",
            initialNarration = "The teacher is ready to begin the first lesson.",
            initialDialogue = "Welcome to Unit 1, Lesson 1! Today we'll be learning about the basics of Virtue Banwa.",
            lessonLearned = "Basics of Virtue Banwa",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "I'm ready to learn!",
                    response = "Excellent! Please talk to the students around the room to learn more.",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "Started Unit 1 Lesson 1",
                        sprite = "OneStar",
                        score = 10
                    }
                },
                new DialogueChoice
                {
                    text = "What will we be learning today?",
                    response = "Today we'll be covering the fundamentals of digital citizenship. Speak with the other students to learn more about it.",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "Started Unit 1 Lesson 1",
                        sprite = "OneStar",
                        score = 10
                    }
                }
            }
        };
        
        return dialogue;
    }
    
    // Add more NPC dialogue methods as needed
}
