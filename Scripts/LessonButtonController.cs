using UnityEngine;
using TMPro;

public class LessonButtonController : MonoBehaviour
{
    public TextMeshProUGUI lessonText; // Reference to the LessonText
    public TextMeshProUGUI statusText; // Reference to the StatusText

    public void SetLessonName(string name)
    {
        if (lessonText != null)
        {
            lessonText.text = name;
        }
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status.ToUpper();
        }
    }
}
