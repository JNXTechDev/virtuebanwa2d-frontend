using UnityEngine;

public class LessonCanvasController : MonoBehaviour
{
    public GameObject story1Canvas;
    public GameObject story2Canvas;

    void Start()
    {
        string currentLesson = PlayerPrefs.GetString("CurrentLesson", "Lesson 1");
        
        // Hide all canvases first
        story1Canvas.SetActive(false);
        story2Canvas.SetActive(false);

        // Show appropriate canvas
        if (currentLesson == "Lesson 1")
        {
            story1Canvas.SetActive(true);
        }
        else if (currentLesson == "Lesson 2")
        {
            story2Canvas.SetActive(true);
        }
    }
}
