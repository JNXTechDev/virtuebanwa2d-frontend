using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasManager : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas lesson2Canvas;
    public Canvas[] otherCanvases;
    public Canvas lesson1Canvas;
    private string currentUnit;
    private string currentLesson;

    void Awake() // Change from Start to Awake
    {
        if (lesson2Canvas == null)
        {
            Debug.LogWarning("Lesson2Canvas not assigned - attempting to find in scene");
            lesson2Canvas = GameObject.Find("Lesson2Canvas")?.GetComponent<Canvas>();
        }

        // Initialize other canvases
        InitializeCanvases();

        // Get current unit and lesson from PlayerPrefs
        currentUnit = PlayerPrefs.GetString("CurrentUnit", "");
        currentLesson = PlayerPrefs.GetString("CurrentLesson", "");

        // Show appropriate canvas based on unit and lesson
        ShowRelevantCanvas();
    }

    private void InitializeCanvases()
    {
        if (lesson2Canvas != null)
        {
            lesson2Canvas.enabled = true;
        }
        else
        {
            Debug.LogError("Could not find Lesson2Canvas in scene!");
        }

        // Initialize other canvases if needed
        if (otherCanvases != null)
        {
            foreach (var canvas in otherCanvases)
            {
                if (canvas != null) canvas.enabled = false;
            }
        }
    }

    private void ShowRelevantCanvas()
    {
        if (currentUnit == "Unit 1" || currentUnit == "UNIT 1")
        {
            switch (currentLesson)
            {
                case "Lesson 1":
                case "LESSON 1":
                    lesson1Canvas.gameObject.SetActive(true);
                    lesson2Canvas.gameObject.SetActive(false);
                    Debug.Log("Showing Lesson 1 Canvas");
                    break;

                case "Lesson 2":
                case "LESSON 2":
                    lesson1Canvas.gameObject.SetActive(false);
                    lesson2Canvas.gameObject.SetActive(true);
                    Debug.Log("Showing Lesson 2 Canvas");
                    break;

                default:
                    Debug.LogWarning($"Unknown lesson: {currentLesson}");
                    break;
            }
        }
    }
}
