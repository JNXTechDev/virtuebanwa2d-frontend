using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameModeInspector : Editor
{
    private string[] gameModeNames;
    private int selectedGameMode;
    
    private void OnEnable()
    {
        // Get all game mode names from enum
        gameModeNames = System.Enum.GetNames(typeof(GameMode));
        
        // Get current game mode
        GameManager gameManager = (GameManager)target;
        selectedGameMode = (int)gameManager.currentGameMode;
    }
    
    public override void OnInspectorGUI()
    {
        // Get the GameManager instance
        GameManager gameManager = (GameManager)target;
        
        // Draw a horizontal line
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Game Mode Selection", EditorStyles.boldLabel);
        
        // Draw a dropdown for game mode selection
        selectedGameMode = EditorGUILayout.Popup("Current Game Mode", selectedGameMode, gameModeNames);
        
        // Update game mode if changed
        if ((int)gameManager.currentGameMode != selectedGameMode)
        {
            Undo.RecordObject(gameManager, "Change Game Mode");
            gameManager.currentGameMode = (GameMode)selectedGameMode;
            EditorUtility.SetDirty(gameManager);
        }
        
        // Draw the rest of the inspector
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Game Manager Settings", EditorStyles.boldLabel);
        DrawDefaultInspector();
    }
}
