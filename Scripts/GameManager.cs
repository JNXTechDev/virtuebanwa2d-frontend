private void InitializeGameMode()
{
    Debug.Log($"Initializing game mode: {currentGameMode}");
    
    // Clear any existing quests and dialogues
    dialogueManager.Reset();
    questManager.Reset();
    
    // Setup the appropriate content based on the current game mode
    switch (currentGameMode)
    {
        case GameMode.Tutorial:
            SetupTutorialMode();
            break;
        case GameMode.Unit1Lesson1:
            SetupUnit1Lesson1();
            break;
        case GameMode.Unit1Lesson2:
            SetupUnit1Lesson2();
            break;
        case GameMode.Unit1Lesson3:
            SetupUnit1Lesson3();
            break;
        case GameMode.Unit1Lesson4:
            SetupUnit1Lesson4();
            break;
        case GameMode.Unit1Lesson5:
            SetupUnit1Lesson5();
            break;
        case GameMode.Unit1Lesson6:
            SetupUnit1Lesson6();
            break;
        // Add other modes as you implement them
        default:
            Debug.LogWarning($"Game mode {currentGameMode} not implemented yet.");
            break;
    }
}

// Add setup methods for each lesson
private void SetupUnit1Lesson1()
{
    Debug.Log("Setting up Unit 1 Lesson 1");
    
    // Add NPCs and their dialogues for this lesson
    dialogueManager.AddDialogueForNPC("Teacher", CreateTeacherDialogueForLesson1());
    dialogueManager.AddDialogueForNPC("Student1", CreateStudent1DialogueForLesson1());
    // Add more NPCs as needed
    
    // Create quest sequence for this lesson
    questManager.CreateQuestSequence(
        "Unit1Lesson1", 
        new string[] { "Teacher", "Student1" },
        OnUnit1Lesson1Complete
    );
    
    // Start with the first quest target
    SetNextQuestTarget();
}

// Add similar methods for other lessons
private void SetupUnit1Lesson2()
{
    // Similar implementation for Lesson 2
    // ...
}

// Callback for lesson completion
private void OnUnit1Lesson1Complete()
{
    Debug.Log("Unit 1 Lesson 1 completed!");
    SaveLessonCompletionStatus("Unit1", "Lesson1");
}

// Helper method for saving lesson completion
private async void SaveLessonCompletionStatus(string unitName, string lessonName)
{
    // Similar to SaveTutorialCompletionStatus but for regular lessons
    // ...
}
