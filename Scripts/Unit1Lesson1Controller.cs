using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VirtueBanwa;  // Add this line to use the namespace

public class Unit1Lesson1Controller : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI narrationText;
    public TextMeshProUGUI dialogueText;
    public Image npcImage;
    public Image playerImage;
    
    [Header("Choice Buttons")]
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceTexts;
    
    [Header("Reward Panel")]
    public GameObject rewardPanel;
    public Image rewardImage;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI lessonLearnedText;

    private DialogueManager dialogueManager;
    private const string LESSON_FILE = "Unit1Lesson1";

    void Start()
    {
        dialogueManager = GetComponent<DialogueManager>();
        dialogueManager.lessonFile = LESSON_FILE;
        SetupUI();
    }

    void SetupUI()
    {
        // Load the lesson data
        TextAsset jsonFile = Resources.Load<TextAsset>($"DialogueData/{LESSON_FILE}");
        var lessonData = JsonUtility.FromJson<LessonData>(jsonFile.text);

        // Set up initial UI
        titleText.text = lessonData.title;
        subtitleText.text = lessonData.subtitle;
        narrationText.text = lessonData.initialNarration;

        // Set up choice buttons
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int choiceIndex = i; // Needed for closure
            choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            choiceTexts[i].text = lessonData.choices[i].text;
        }
    }

    void OnChoiceSelected(int index)
    {
        dialogueManager.OnChoiceSelected(index);
    }
}
