using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VirtueBanwa.Dialogue;
using VirtueBanwa;

public class GameModeSelector : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Dropdown gameModeDropdown;
    public Button applyButton;
    public TMP_Text statusText;

    [Header("Game Manager Reference")]
    public GameManager gameManager;
    
    // Cache for the game mode names and values
    private string[] gameModeNames;
    private GameMode[] gameModeValues;

    void Start()
    {
        // Initialize the dropdown with game mode options
        InitializeDropdown();
        
        // Set up button listener
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySelectedGameMode);
        }
        
        // If gameManager is not assigned, try to find it
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        // Check for dialog manager as well
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogWarning("DialogueManager not found in scene.");
        }
        
        // Update status text
        UpdateStatusText();
    }

    private void InitializeDropdown()
    {
        if (gameModeDropdown != null)
        {
            // Get all available game modes
            gameModeNames = System.Enum.GetNames(typeof(GameMode));
            gameModeValues = (GameMode[])System.Enum.GetValues(typeof(GameMode));
            
            // Clear existing options
            gameModeDropdown.ClearOptions();
            
            // Create list of options from enum names
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (string modeName in gameModeNames)
            {
                // Format the name for better readability
                string displayName = FormatGameModeName(modeName);
                options.Add(new TMP_Dropdown.OptionData(displayName));
            }
            
            // Add options to dropdown
            gameModeDropdown.AddOptions(options);
            
            // Set current value if game manager is available
            if (gameManager != null)
            {
                int currentIndex = System.Array.IndexOf(gameModeValues, gameManager.currentGameMode);
                if (currentIndex >= 0)
                {
                    gameModeDropdown.value = currentIndex;
                }
            }
        }
    }

    private string FormatGameModeName(string enumName)
    {
        // Example: "Unit1Lesson2" becomes "Unit 1 - Lesson 2"
        string formatted = enumName;
        
        if (enumName.Contains("Unit"))
        {
            // Extract unit and lesson numbers
            if (enumName.Contains("Lesson"))
            {
                string unitPart = enumName.Substring(0, 5); // "Unit1"
                string lessonPart = enumName.Substring(5);  // "Lesson2"
                
                int unitNumber = int.Parse(unitPart.Substring(4));
                int lessonNumber = int.Parse(lessonPart.Substring(6));
                
                formatted = $"Unit {unitNumber} - Lesson {lessonNumber}";
            }
            // Handle PreTest and PostTest
            else if (enumName.Contains("PreTest"))
            {
                string unitPart = enumName.Substring(0, 5); // "Unit1"
                int unitNumber = int.Parse(unitPart.Substring(4));
                formatted = $"Unit {unitNumber} - Pre-Test";
            }
            else if (enumName.Contains("PostTest"))
            {
                string unitPart = enumName.Substring(0, 5); // "Unit1"
                int unitNumber = int.Parse(unitPart.Substring(4));
                formatted = $"Unit {unitNumber} - Post-Test";
            }
        }
        
        return formatted;
    }

    public void ApplySelectedGameMode()
    {
        if (gameManager != null && gameModeDropdown != null)
        {
            int selectedIndex = gameModeDropdown.value;
            
            if (selectedIndex >= 0 && selectedIndex < gameModeValues.Length)
            {
                // Apply the selected game mode
                GameMode selectedMode = gameModeValues[selectedIndex];
                
                // Check if it's different from the current mode
                if (gameManager.currentGameMode != selectedMode)
                {
                    gameManager.currentGameMode = selectedMode;
                    
                    // Reinitialize the game mode
                    gameManager.SendMessage("InitializeGameMode", SendMessageOptions.DontRequireReceiver);
                    
                    // Update status
                    UpdateStatusText("Game mode changed to " + FormatGameModeName(selectedMode.ToString()));
                }
                else
                {
                    UpdateStatusText("Already in selected game mode");
                }
            }
        }
    }

    private void UpdateStatusText(string message = null)
    {
        if (statusText != null)
        {
            if (message != null)
            {
                statusText.text = message;
            }
            else if (gameManager != null)
            {
                statusText.text = "Current mode: " + 
                    FormatGameModeName(gameManager.currentGameMode.ToString());
            }
            else
            {
                statusText.text = "Game Manager not found";
            }
        }
    }
}
