using UnityEngine;
using TMPro;

public class LessonButton : MonoBehaviour
{
    public string lessonName; // Holds the lesson name
    public TMP_Text lessonText; // Reference to the TextMeshPro component for the lesson name

    // Method to set the lesson name and update the UI
    public void SetLessonName(string name)
    {
        lessonName = name;
        if (lessonText != null)
        {
            lessonText.text = name;
        }
    }
}
