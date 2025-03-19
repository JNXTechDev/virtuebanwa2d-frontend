using UnityEngine;
using UnityEngine.UI;

public class DebugButton : MonoBehaviour
{
    public UnitLessonManager unitLessonManager;
    
    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(TestRetakePanel);
        }
        
        if (unitLessonManager == null)
        {
            unitLessonManager = FindObjectOfType<UnitLessonManager>();
        }
    }
    
    private void TestRetakePanel()
    {
        Debug.Log("Debug button clicked - testing retake panel");
        if (unitLessonManager != null)
        {
            unitLessonManager.ShowRetakePanel();
        }
        else
        {
            Debug.LogError("UnitLessonManager reference is missing!");
        }
    }
}
