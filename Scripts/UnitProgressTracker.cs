using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

public class UnitProgressTracker : MonoBehaviour
{
    private string baseUrl => NetworkConfig.BaseUrl;
    private string currentUsername;
    private GameProgress progressData;
    private TestManager testManager;

    // Unit structure for tracking progression
    [System.Serializable]
    public class UnitStructure
    {
        public string unitId;
        public bool preTestCompleted;
        public bool preTestPassed;
        public List<string> completedLessons = new List<string>();
        public bool allLessonsCompleted;
        public bool postTestCompleted;
        public bool postTestPassed;
        public bool unitCompleted;
    }

    public Dictionary<string, UnitStructure> units = new Dictionary<string, UnitStructure>();

    private void Awake()
    {
        testManager = GetComponent<TestManager>();
        if (testManager == null)
        {
            testManager = gameObject.AddComponent<TestManager>();
        }
    }

    private void Start()
    {
        currentUsername = PlayerPrefs.GetString("Username", "DefaultPlayer");
        LoadUnitProgressData();
    }

    public async Task LoadUnitProgressData()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{currentUsername}");
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Loaded progress data: {responseContent}");
                    
                    // Parse the response
                    JObject progress = JObject.Parse(responseContent);
                    
                    // Clear existing data
                    units.Clear();
                    
                    // Process each unit in the progress data
                    if (progress["units"] != null)
                    {
                        foreach (var unitPair in (JObject)progress["units"])
                        {
                            string unitId = unitPair.Key;
                            JObject unitData = (JObject)unitPair.Value;
                            
                            UnitStructure unit = new UnitStructure { unitId = unitId };
                            
                            // Process pre-test status
                            if (unitData["preTest"] != null)
                            {
                                unit.preTestCompleted = unitData["preTest"]["status"]?.ToString() == "Completed";
                                unit.preTestPassed = unitData["preTest"]["passed"]?.Value<bool>() ?? false;
                            }
                            
                            // Process lessons completion
                            if (unitData["lessons"] != null)
                            {
                                foreach (var lessonPair in (JObject)unitData["lessons"])
                                {
                                    string lessonId = lessonPair.Key;
                                    JObject lessonData = (JObject)lessonPair.Value;
                                    
                                    if (lessonData["status"]?.ToString() == "Completed")
                                    {
                                        unit.completedLessons.Add(lessonId);
                                    }
                                }
                                
                                // Check if all lessons are completed (6 lessons per unit)
                                unit.allLessonsCompleted = unit.completedLessons.Count >= 6;
                            }
                            
                            // Process post-test status
                            if (unitData["postTest"] != null)
                            {
                                unit.postTestCompleted = unitData["postTest"]["status"]?.ToString() == "Completed";
                                unit.postTestPassed = unitData["postTest"]["passed"]?.Value<bool>() ?? false;
                            }
                            
                            // Unit is completed if pre-test, all lessons, and post-test are completed and passed
                            unit.unitCompleted = unit.preTestCompleted && unit.preTestPassed && 
                                               unit.allLessonsCompleted && 
                                               unit.postTestCompleted && unit.postTestPassed;
                            
                            units[unitId] = unit;
                        }
                    }
                    
                    return;
                }
                
                Debug.LogError($"Failed to load unit progress: {response.StatusCode}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading unit progress: {e.Message}");
        }
    }

    public async Task<bool> UnlockNextStage(string unitId)
    {
        if (!units.TryGetValue(unitId, out UnitStructure unit))
        {
            Debug.LogError($"Unit {unitId} not found in progress data!");
            return false;
        }
        
        try
        {
            JObject updateData = new JObject
            {
                ["Username"] = currentUsername,
                ["units"] = new JObject
                {
                    [unitId] = new JObject()
                }
            };
            
            // Determine next stage and update accordingly
            if (!unit.preTestCompleted)
            {
                // If pre-test not completed, we can't unlock anything yet
                return false;
            }
            else if (unit.preTestCompleted && unit.preTestPassed && unit.completedLessons.Count == 0)
            {
                // Unlock first lesson after pre-test
                updateData["units"][unitId]["lessons"] = new JObject
                {
                    ["Lesson1"] = new JObject
                    {
                        ["status"] = "Available"
                    }
                };
            }
            else if (unit.allLessonsCompleted && !unit.postTestCompleted)
            {
                // Unlock post-test after all lessons
                updateData["units"][unitId]["postTest"] = new JObject
                {
                    ["status"] = "Available"
                };
            }
            else if (unit.unitCompleted && unitId == "Unit1")
            {
                // Unlock Unit2 pre-test after Unit1 is completed
                updateData["units"]["Unit2"] = new JObject
                {
                    ["status"] = "In Progress",
                    ["preTest"] = new JObject
                    {
                        ["status"] = "Available"
                    }
                };
            }
            else if (unit.unitCompleted && unitId == "Unit2")
            {
                // Mark game as completed after Unit2 is completed
                updateData["gameStatus"] = "Completed";
            }
            else
            {
                // No unlock needed
                return true;
            }
            
            string json = updateData.ToString();
            Debug.Log($"Updating progression: {json}");
            
            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progression updated successfully: {responseContent}");
                    
                    // Reload progress data
                    await LoadUnitProgressData();
                    return true;
                }
                else
                {
                    Debug.LogError($"Error updating progression: {response.StatusCode}");
                    return false;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception updating progression: {e.Message}");
            return false;
        }
    }

    public string GetNextAvailableStage(string unitId)
    {
        if (!units.TryGetValue(unitId, out UnitStructure unit))
        {
            // Default to pre-test if unit not found
            return $"{unitId}_PreTest";
        }
        
        if (!unit.preTestCompleted)
        {
            return $"{unitId}_PreTest";
        }
        else if (unit.preTestCompleted && unit.completedLessons.Count < 6)
        {
            // Find next uncompleted lesson
            for (int i = 1; i <= 6; i++)
            {
                string lessonId = $"Lesson{i}";
                if (!unit.completedLessons.Contains(lessonId))
                {
                    return $"{unitId}_{lessonId}";
                }
            }
            return $"{unitId}_Lesson1"; // Default if we can't find an uncompleted lesson
        }
        else if (unit.allLessonsCompleted && !unit.postTestCompleted)
        {
            return $"{unitId}_PostTest";
        }
        else if (unit.unitCompleted && unitId == "Unit1")
        {
            return "Unit2_PreTest";
        }
        
        // Default case
        return $"{unitId}_PreTest";
    }

    public bool CanAccessStage(string unitId, string stageId)
    {
        if (!units.TryGetValue(unitId, out UnitStructure unit))
        {
            // Only allow pre-test if unit not found
            return stageId == "PreTest";
        }
        
        if (stageId == "PreTest")
        {
            // Always allow pre-test
            return true;
        }
        else if (stageId.StartsWith("Lesson"))
        {
            // Lessons require pre-test completion
            if (!unit.preTestCompleted || !unit.preTestPassed)
                return false;
                
            // Extract lesson number
            if (int.TryParse(stageId.Substring(6), out int lessonNum))
            {
                // Check if previous lesson is completed
                if (lessonNum == 1)
                {
                    return true; // First lesson is available after pre-test
                }
                else
                {
                    return unit.completedLessons.Contains($"Lesson{lessonNum-1}");
                }
            }
        }
        else if (stageId == "PostTest")
        {
            // Post-test requires all lessons to be completed
            return unit.allLessonsCompleted;
        }
        
        return false;
    }
}
