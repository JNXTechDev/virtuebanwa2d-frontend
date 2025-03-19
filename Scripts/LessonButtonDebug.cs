using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LessonButtonDebug : MonoBehaviour
{
    public UnitLessonManager unitLessonManager;
    public string lessonName = "Tutorial";
    public string unitName = "UNIT 1";
    
    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        button.onClick.AddListener(DebugShowRetake);
        
        if (unitLessonManager == null)
        {
            unitLessonManager = FindObjectOfType<UnitLessonManager>();
            if (unitLessonManager == null)
            {
                Debug.LogError("Could not find UnitLessonManager in the scene!");
            }
        }
        
        // If this is attached to a lesson button, try to get the lesson name
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null && text.text.Contains("Tutorial"))
        {
            lessonName = "Tutorial";
        }
    }
    
    public void DebugShowRetake()
    {
        Debug.Log($"Debug button clicked - forcing retake panel for {unitName} - {lessonName}");
        if (unitLessonManager != null)
        {
            unitLessonManager.ShowRetakePanel(lessonName);
        }
    }
}
