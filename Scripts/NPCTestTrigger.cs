using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCTestTrigger : MonoBehaviour
{
    [Header("Test UI")]
    public GameObject testPanel;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI[] answerTexts;
    public Button nextQuestionButton;
    public Button prevQuestionButton;
    public Button submitButton;
    public TextMeshProUGUI questionCountText;
    
    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI passFailText;
    public TextMeshProUGUI feedbackText;
    [SerializeField] private Button continueAfterTestButton;
    
    [Header("NPC Reference")]
    [SerializeField] private NPCDialogueTrigger dialogueTrigger;
    
    private TestManager testManager;
    private UnitProgressTracker progressTracker;
    private int currentQuestionIndex = 0;
    private List<int> userAnswers = new List<int>();
    
    private void Start()
    {
        testManager = FindObjectOfType<TestManager>();
        if (testManager == null)
        {
            Debug.LogError("TestManager not found! Ensure it is added to the scene.");
        }

        progressTracker = FindObjectOfType<UnitProgressTracker>();
        
        if (testManager == null)
            Debug.LogError("TestManager not found!");
            
        if (progressTracker == null)
            Debug.LogError("UnitProgressTracker not found!");
        
        // If we don't have a dialogue trigger assigned, try to get it from this GameObject
        if (dialogueTrigger == null)
            dialogueTrigger = GetComponent<NPCDialogueTrigger>();
        
        // Hide test panels initially
        if (testPanel != null)
            testPanel.SetActive(false);
        if (resultPanel != null)
            resultPanel.SetActive(false);
            
        // Setup buttons
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        if (nextQuestionButton != null)
            nextQuestionButton.onClick.AddListener(OnNextQuestion);
        if (prevQuestionButton != null)
            prevQuestionButton.onClick.AddListener(OnPreviousQuestion);
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitTest);
        if (continueAfterTestButton != null)
            continueAfterTestButton.onClick.AddListener(OnContinueAfterTest);
            
        // Setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int answerIndex = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(answerIndex));
        }
    }
    
    public async void StartTest()
    {
        if (testManager == null)
        {
            Debug.LogError("Cannot start test: TestManager not initialized. Ensure it is assigned in the scene.");
            return;
        }
        
        // Get current test info from PlayerPrefs
        string unitId = PlayerPrefs.GetString("CurrentUnit", "Unit1");
        string testType = PlayerPrefs.GetString("CurrentTest", "PreTest");
        
        // Load test data
        bool success = await testManager.LoadTestData(unitId, testType);
        if (!success)
        {
            Debug.LogError("Failed to load test data");
            return;
        }
        
        // Hide dialogue panel from dialogue trigger if it exists
        if (dialogueTrigger != null && dialogueTrigger.dialoguePanel != null)
            dialogueTrigger.dialoguePanel.SetActive(false);
            
        // Show test panel
        if (testPanel != null)
            testPanel.SetActive(true);
            
        // Reset to first question
        currentQuestionIndex = 0;
        InitializeUserAnswers();
        ShowCurrentQuestion();
    }
    
    private void InitializeUserAnswers()
    {
        userAnswers.Clear();
        int questionCount = testManager.currentTest?.questions?.Count ?? 0;
        for (int i = 0; i < questionCount; i++)
            userAnswers.Add(-1);
    }
    
    private void ShowCurrentQuestion()
    {
        if (currentQuestionIndex < 0 || currentQuestionIndex >= testManager.currentTest.questions.Count)
        {
            Debug.LogError("Invalid question index.");
            return;
        }

        var question = testManager.currentTest.questions[currentQuestionIndex];

        // Set question text
        if (questionText != null)
        {
            questionText.text = question.question;
            Debug.Log($"Displaying question: {question.question}");
        }

        // Set question count
        if (questionCountText != null)
        {
            questionCountText.text = $"Question {currentQuestionIndex + 1} of {testManager.currentTest.questions.Count}";
        }

        // Setup answer options
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < question.options.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerTexts[i].text = question.options[i];

                bool isSelected = userAnswers.Count > currentQuestionIndex &&
                                  userAnswers[currentQuestionIndex] == i;

                answerButtons[i].image.color = isSelected ? new Color(0.8f, 0.9f, 1f) : Color.white;
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        UpdateNavigationButtons();
    }
    
    private void UpdateNavigationButtons()
    {
        if (prevQuestionButton != null)
            prevQuestionButton.interactable = currentQuestionIndex > 0;
            
        if (nextQuestionButton != null)
            nextQuestionButton.interactable = currentQuestionIndex < testManager.currentTest.questions.Count - 1;
            
        if (submitButton != null)
            submitButton.gameObject.SetActive(currentQuestionIndex == testManager.currentTest.questions.Count - 1);
    }
    
    private void OnAnswerSelected(int answerIndex)
    {
        if (currentQuestionIndex < userAnswers.Count)
            userAnswers[currentQuestionIndex] = answerIndex;
        
        testManager.RecordAnswer(currentQuestionIndex, answerIndex);
        
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].image.color = (i == answerIndex) ? new Color(0.8f, 0.9f, 1f) : Color.white;
        }
        
        if (currentQuestionIndex < testManager.currentTest.questions.Count - 1)
        {
            Invoke("OnNextQuestion", 0.75f);
        }
    }
    
    private void OnNextQuestion()
    {
        if (currentQuestionIndex < testManager.currentTest.questions.Count - 1)
        {
            currentQuestionIndex++;
            ShowCurrentQuestion();
        }
    }
    
    private void OnPreviousQuestion()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            ShowCurrentQuestion();
        }
    }
    
    private async void OnSubmitTest()
    {
        if (!AreAllQuestionsAnswered())
        {
            Debug.Log("Please answer all questions before submitting.");
            return;
        }
        
        if (testPanel != null)
            testPanel.SetActive(false);
            
        // Changed CalculateTestScore to GetScore to match TestManager
        int score = testManager.GetScore();
        bool passed = testManager.IsPassed();
        
        ShowResults(score, passed);
        
        // Save results
        string unitId = PlayerPrefs.GetString("CurrentUnit", "Unit1");
        string testType = PlayerPrefs.GetString("CurrentTest", "PreTest");
        
        await testManager.SaveTestResult(unitId, testType, score, passed);
        
        if (passed)
        {
            await progressTracker.UnlockNextStage(unitId);
        }
    }
    
    private bool AreAllQuestionsAnswered()
    {
        return !userAnswers.Contains(-1);
    }
    
    private void ShowResults(int score, bool passed)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            int correctAnswers = testManager.GetCorrectAnswers();
            int totalQuestions = testManager.GetTotalQuestions();

            if (scoreText != null)
                scoreText.text = $"Your Score: {correctAnswers} / {totalQuestions}";

            if (passFailText != null)
            {
                passFailText.text = passed ? "PASSED!" : "FAILED";
                passFailText.color = passed ? Color.green : Color.red;
            }

            if (feedbackText != null)
            {
                string testType = PlayerPrefs.GetString("CurrentTest", "PreTest");
                feedbackText.text = passed
                    ? (testType == "PreTest"
                        ? "Great job! You can now proceed to the lessons."
                        : "Congratulations! You've completed this unit!")
                    : "You didn't pass this time. Don't worry, you can try again.";
            }
        }
    }
    
    private void OnContinueAfterTest()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Outside");
    }
    
    private void OnDestroy()
    {
        if (nextQuestionButton != null)
            nextQuestionButton.onClick.RemoveAllListeners();
        if (prevQuestionButton != null)
            prevQuestionButton.onClick.RemoveAllListeners();
        if (submitButton != null)
            submitButton.onClick.RemoveAllListeners();
        if (continueAfterTestButton != null)
            continueAfterTestButton.onClick.RemoveAllListeners();
            
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].onClick.RemoveAllListeners();
        }
    }
}