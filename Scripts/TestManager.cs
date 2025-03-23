using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO; // Add this for file handling
using System.Linq; // Ensure this is included for LINQ methods

public class TestManager : MonoBehaviour
{
    [System.Serializable]
    public class TestQuestion
    {
        public string question;
        public string[] options;
        public int correctAnswerIndex;
        public string explanation;
    }

    [System.Serializable]
    public class TestData
    {
        public string unitId;
        public string testType; // "PreTest" or "PostTest"
        public List<TestQuestion> questions = new List<TestQuestion>();
        public int passingScore = 70; // Default passing percentage
    }

    private string baseUrl => NetworkConfig.BaseUrl;
    private string currentUsername;
    private GameProgress progressData;

    [Header("Test Configuration")]
    public TestData currentTest;
    
    private int currentScore = 0;
    private int totalQuestions = 0;
    private List<int> userAnswers = new List<int>();
    
    void Start()
    {
        currentUsername = PlayerPrefs.GetString("Username", "DefaultPlayer");
        // Initialize with empty test data
        currentTest = new TestData();
    }
    
    public async Task<bool> LoadTestData(string unitId, string testType)
    {
        try
        {
            string filePath = $"Resources/DialogueData/Unit 1/pretest.json";
            string fullPath = Path.Combine(Application.dataPath, filePath);

            if (File.Exists(fullPath))
            {
                string jsonContent = File.ReadAllText(fullPath);
                Debug.Log($"Loaded test data from file: {fullPath}");

                JObject testData = JObject.Parse(jsonContent);

                currentTest = new TestData
                {
                    unitId = unitId,
                    testType = testType,
                    questions = new List<TestQuestion>()
                };

                if (testData["preTest"]?["questions"] != null)
                {
                    foreach (var questionObj in testData["preTest"]["questions"])
                    {
                        var choices = questionObj["choices"].ToObject<List<JObject>>();

                        TestQuestion question = new TestQuestion
                        {
                            question = questionObj["question"].ToString(),
                            correctAnswerIndex = choices
                                .Select((choice, index) => new { choice, index })
                                .FirstOrDefault(c => c.choice["score"].Value<int>() == 1)?.index ?? -1,
                            options = choices
                                .Select(c => c["text"].ToString())
                                .ToArray()
                        };

                        currentTest.questions.Add(question);
                    }
                }

                totalQuestions = currentTest.questions.Count;
                ResetTest();

                Debug.Log($"Loaded {totalQuestions} questions.");
                return true;
            }
            else
            {
                Debug.LogError($"Test data file not found: {fullPath}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading test data: {e.Message}");
            return false;
        }
    }
    
    public void RecordAnswer(int questionIndex, int answerIndex)
    {
        if (questionIndex >= 0 && questionIndex < userAnswers.Count)
        {
            userAnswers[questionIndex] = answerIndex;
            
            // Update score if this is the correct answer
            if (currentTest.questions != null && 
                questionIndex < currentTest.questions.Count && 
                answerIndex == currentTest.questions[questionIndex].correctAnswerIndex)
            {
                currentScore++;
            }
        }
    }
    
    public void ResetTest()
    {
        currentScore = 0;
        userAnswers = new List<int>();
        
        // Initialize user answers list with -1 (unanswered)
        for (int i = 0; i < totalQuestions; i++)
        {
            userAnswers.Add(-1);
        }
    }
    
    public int GetCorrectAnswers()
    {
        return currentScore; // Return the number of correct answers
    }

    public int GetTotalQuestions()
    {
        return totalQuestions; // Return the total number of questions
    }
    
    public bool IsPassed()
    {
        return GetScore() >= currentTest.passingScore;
    }

    public int GetScore()
    {
        // Calculate percentage score
        if (totalQuestions > 0)
        {
            return (int)((float)currentScore / totalQuestions * 100);
        }
        return 0;
    }
    
    public async Task<bool> SaveTestResult(string unitId, string testType, int score, bool passed)
    {
        try
        {
            // Create data object for API
            JObject data = new JObject
            {
                ["Username"] = currentUsername,
                ["units"] = new JObject
                {
                    [unitId] = new JObject()
                }
            };
            
            // Add test result based on whether it's a pre-test or post-test
            if (testType == "PreTest")
            {
                data["units"][unitId]["preTest"] = new JObject
                {
                    ["status"] = "Completed",
                    ["score"] = score,
                    ["passed"] = passed,
                    ["date"] = System.DateTime.Now
                };
                
                // If this was passed, we need to unlock the first lesson
                if (passed)
                {
                    data["units"][unitId]["lessons"] = new JObject
                    {
                        ["Lesson1"] = new JObject
                        {
                            ["status"] = "Available"
                        }
                    };
                }

                // Call GameManager to save progress
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.SavePreTestProgress(unitId, score, passed);
                }
            }
            else if (testType == "PostTest")
            {
                data["units"][unitId]["postTest"] = new JObject
                {
                    ["status"] = "Completed",
                    ["score"] = score,
                    ["passed"] = passed,
                    ["date"] = System.DateTime.Now
                };
                
                // If this was passed and it's Unit1, unlock Unit2
                if (passed && unitId == "Unit1")
                {
                    data["units"]["Unit2"] = new JObject
                    {
                        ["status"] = "In Progress",
                        ["preTest"] = new JObject
                        {
                            ["status"] = "Available"
                        }
                    };
                }
                else if (passed && unitId == "Unit2")
                {
                    // This is the final unit, mark everything as completed
                    data["gameStatus"] = "Completed";
                }
            }
            
            string json = data.ToString();
            Debug.Log($"Saving test result: {json}");
            
            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Test result saved successfully: {responseContent}");
                    return true;
                }
                else
                {
                    Debug.LogError($"Error saving test result: {response.StatusCode}");
                    return false;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception saving test result: {e.Message}");
            return false;
        }
    }
}
