using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

public class AchievementsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject achievementEntryPrefab;
    [SerializeField] private Transform achievementsContainer;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TextMeshProUGUI noAchievementsText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

   // [Header("Configuration")]

  [SerializeField]  private string baseUrl => NetworkConfig.BaseUrl;
   //  [SerializeField] private  string baseUrl => NetworkConfig.BaseUrl + "/api"; 

    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool sortByDate = true;
    [SerializeField] private bool newestFirst = true;

    private string currentUsername;
    private Dictionary<string, Sprite> rewardSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, JObject> dialogueCache = new Dictionary<string, JObject>();

    private void Start()
    {
        // Set up UI event listeners
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshAchievements);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        // Load reward sprites
        LoadRewardSprites();

        // Get the current username
        currentUsername = PlayerPrefs.GetString("Username", "Player");

        // Load achievements if configured to do so
        if (loadOnStart && gameObject.activeInHierarchy)
        {
            RefreshAchievements();
        }
    }

    private void OnEnable()
    {
        // Reload achievements whenever the panel becomes active
        if (!loadOnStart || string.IsNullOrEmpty(currentUsername)) 
        {
            currentUsername = PlayerPrefs.GetString("Username", "Player");
        }
        
        RefreshAchievements();
    }

    public void RefreshAchievements()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        if (noAchievementsText != null)
            noAchievementsText.gameObject.SetActive(false);
            
        ClearExistingAchievements();
        
        // Start the async process
        FetchAndDisplayAchievements();
    }

    private void LoadRewardSprites()
    {
        // Load all the star rewards
        string[] rewardNames = { "OneStar", "TwoStar", "ThreeStar", "FourStar", "FiveStar" };
        
        foreach (string rewardName in rewardNames)
        {
            Sprite sprite = Resources.Load<Sprite>($"Rewards/{rewardName}");
            if (sprite != null)
            {
                rewardSprites[rewardName] = sprite;
                Debug.Log($"Loaded reward sprite: {rewardName}");
            }
            else
            {
                Debug.LogError($"Failed to load reward sprite: {rewardName}");
            }
        }
    }

    private void ClearExistingAchievements()
    {
        if (achievementsContainer != null)
        {
            // Clear existing entries
            foreach (Transform child in achievementsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private async void FetchAndDisplayAchievements()
    {
        try
        {
            var achievements = await GetPlayerAchievements(currentUsername);
            
            Debug.Log($"Found {achievements.Count} achievements for user {currentUsername}");
            
            if (achievements.Count == 0)
            {
                if (noAchievementsText != null)
                {
                    noAchievementsText.gameObject.SetActive(true);
                    noAchievementsText.text = "No achievements yet. Complete lessons to earn rewards!";
                }
            }
            else
            {
                // First, organize achievements by category
                var tutorialAchievements = achievements.Where(a => a.Source == "Tutorial").ToList();
                var lessonAchievements = achievements.Where(a => a.Source != "Tutorial").ToList();
                
                // Sort within each category if needed
                if (sortByDate)
                {
                    if (newestFirst)
                    {
                        tutorialAchievements = tutorialAchievements.OrderByDescending(a => a.Date).ToList();
                        lessonAchievements = lessonAchievements.OrderByDescending(a => a.Date).ToList();
                    }
                    else
                    {
                        tutorialAchievements = tutorialAchievements.OrderBy(a => a.Date).ToList();
                        lessonAchievements = lessonAchievements.OrderBy(a => a.Date).ToList();
                    }
                }
                
                // Create entries for tutorial achievements first
                int createdEntries = 0;
                
                // Process tutorial achievements first
                foreach (var achievement in tutorialAchievements)
                {
                    bool success = CreateAchievementEntry(achievement);
                    if (success) createdEntries++;
                    await Task.Delay(30); // Short delay for smoother UI updates
                }
                
                // Then process lesson achievements
                foreach (var achievement in lessonAchievements)
                {
                    bool success = CreateAchievementEntry(achievement);
                    if (success) createdEntries++;
                    await Task.Delay(30); // Short delay for smoother UI updates
                }
                
                Debug.Log($"Created {createdEntries} achievement entries out of {achievements.Count} achievements");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error fetching achievements: {ex.Message}\n{ex.StackTrace}");
            
            if (noAchievementsText != null)
            {
                noAchievementsText.gameObject.SetActive(true);
                noAchievementsText.text = "Failed to load achievements. Please try again later.";
            }
        }
        finally
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }
    }

    private async Task<List<AchievementData>> GetPlayerAchievements(string username)
    {
        List<AchievementData> achievements = new List<AchievementData>();
        
        try
        {
            using (HttpClient client = new HttpClient())
            {
                // Fix the URL path - use proper endpoint path with no double "/api"
                string url = $"{baseUrl}/game_progress/{username}";
                Debug.Log($"Requesting achievements from: {url}");
                
                HttpResponseMessage response = await client.GetAsync(url);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Debug.LogWarning($"No game progress found for user: {username}");
                    return achievements; // Return empty list instead of throwing exception
                }
                
                response.EnsureSuccessStatusCode();
                
                string content = await response.Content.ReadAsStringAsync();
                Debug.Log($"API Response: {content.Substring(0, Math.Min(content.Length, 500))}...");
                
                // Parse the response - we use JObject for more flexible JSON navigation
                JObject data = JObject.Parse(content);
                int achievementCount = 0;
                
                // Create a helper HashSet to track unique achievements and prevent duplicates
                HashSet<string> addedAchievements = new HashSet<string>();

                // Process tutorial achievements
                if (data["tutorial"] is JObject tutorial)
                {
                    // Check the main tutorial reward
                    if (tutorial["reward"] != null && !string.IsNullOrEmpty(tutorial["reward"].ToString()))
                    {
                        // Create a unique key for this achievement
                        string achievementKey = $"Tutorial_Completion_{tutorial["reward"]}";
                        
                        // Only add if we haven't seen this achievement before
                        if (!addedAchievements.Contains(achievementKey))
                        {
                            achievements.Add(new AchievementData
                            {
                                Source = "Tutorial",
                                Title = "Tutorial Completion",
                                Description = "Completed the game tutorial",
                                RewardSprite = tutorial["reward"].ToString(),
                                Date = ParseDateTime(tutorial["date"]?.ToString())
                            });
                            achievementCount++;
                            addedAchievements.Add(achievementKey);
                            Debug.Log($"Added tutorial completion achievement with reward: {tutorial["reward"]}");
                        }
                    }
                    
                    // Check tutorial checkpoints
                    if (tutorial["checkpoints"] is JObject checkpoints)
                    {
                        Debug.Log($"Found {checkpoints.Count} tutorial checkpoints");
                        foreach (var checkpoint in checkpoints)
                        {
                            string npcName = checkpoint.Key;
                            JObject checkpointData = checkpoint.Value as JObject;
                            
                            if (checkpointData != null && 
                                checkpointData["reward"] != null && 
                                !string.IsNullOrEmpty(checkpointData["reward"].ToString()))
                            {
                                // Create a unique key for this achievement
                                string achievementKey = $"Tutorial_{npcName}_{checkpointData["reward"]}";
                                
                                // Only add if we haven't seen this achievement before
                                if (!addedAchievements.Contains(achievementKey))
                                {
                                    // Try to get extra data from dialogue file
                                    JObject dialogueData = await GetNPCDialogueData(npcName);
                                    string message = checkpointData["message"]?.ToString() ?? $"Met with {npcName}";
                                    
                                    // Create the achievement
                                    achievements.Add(new AchievementData
                                    {
                                        Source = "Tutorial",
                                        Title = $"Tutorial: {npcName}",
                                        Description = message,
                                        RewardSprite = checkpointData["reward"].ToString(),
                                        Date = ParseDateTime(checkpointData["date"]?.ToString()),
                                        NpcName = npcName
                                    });
                                    achievementCount++;
                                    addedAchievements.Add(achievementKey);
                                    Debug.Log($"Added {npcName} tutorial checkpoint with reward: {checkpointData["reward"]}");
                                }
                            }
                        }
                    }
                }
                
                // Process unit/lesson achievements, but only those with rewards
                if (data["units"] is JObject units)
                {
                    foreach (var unit in units)
                    {
                        string unitName = unit.Key; // e.g. Unit1
                        JObject unitData = unit.Value as JObject;
                        
                        if (unitData != null && unitData["lessons"] is JObject lessons)
                        {
                            foreach (var lesson in lessons)
                            {
                                string lessonName = lesson.Key; // e.g. Lesson1
                                JObject lessonData = lesson.Value as JObject;
                                
                                // Check lesson rewards - only add if it has a reward
                                if (lessonData != null && 
                                    lessonData["reward"] != null && 
                                    !string.IsNullOrEmpty(lessonData["reward"].ToString()))
                                {
                                    string formattedUnitName = FormatUnitName(unitName);
                                    string formattedLessonName = FormatLessonName(lessonName);
                                    
                                    // Create a unique key for this achievement
                                    string achievementKey = $"{unitName}_{lessonName}_{lessonData["reward"]}";
                                    
                                    // Only add if we haven't seen this achievement before
                                    if (!addedAchievements.Contains(achievementKey))
                                    {
                                        achievements.Add(new AchievementData
                                        {
                                            Source = formattedUnitName,
                                            Title = $"{formattedUnitName}: {formattedLessonName}",
                                            Description = $"Completed {formattedLessonName} in {formattedUnitName}",
                                            RewardSprite = lessonData["reward"].ToString(),
                                            Date = ParseDateTime(lessonData["lastAttempt"]?.ToString())
                                        });
                                        achievementCount++;
                                        addedAchievements.Add(achievementKey);
                                        Debug.Log($"Added lesson achievement: {formattedUnitName}: {formattedLessonName}");
                                    }
                                }
                                
                                // Check NPC specific rewards within lessons - only add if they have rewards
                                if (lessonData != null && lessonData["rewards"] is JObject npcRewards)
                                {
                                    foreach (var npcReward in npcRewards)
                                    {
                                        string npcName = npcReward.Key;
                                        JObject rewardData = npcReward.Value as JObject;
                                        
                                        if (rewardData != null && 
                                            rewardData["sprite"] != null && 
                                            !string.IsNullOrEmpty(rewardData["sprite"].ToString()))
                                        {
                                            // Create a unique key for this achievement
                                            string achievementKey = $"{unitName}_{lessonName}_{npcName}_{rewardData["sprite"]}";
                                            
                                            // Only add if we haven't seen this achievement before
                                            if (!addedAchievements.Contains(achievementKey))
                                            {
                                                string formattedUnitName = FormatUnitName(unitName);
                                                string message = rewardData["message"]?.ToString() ?? 
                                                    $"Interaction with {npcName} in {formattedUnitName}";
                                                
                                                achievements.Add(new AchievementData
                                                {
                                                    Source = formattedUnitName,
                                                    Title = $"{npcName} - {formattedUnitName}",
                                                    Description = message,
                                                    RewardSprite = rewardData["sprite"].ToString(),
                                                    Date = ParseDateTime(rewardData["date"]?.ToString()),
                                                    NpcName = npcName
                                                });
                                                achievementCount++;
                                                addedAchievements.Add(achievementKey);
                                                Debug.Log($"Added NPC achievement: {npcName} in {formattedUnitName}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                Debug.Log($"Total achievements found: {achievementCount}, Unique: {addedAchievements.Count}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error retrieving player achievements: {ex.Message}\n{ex.StackTrace}");
            // Instead of re-throwing, log and return empty list to avoid crashing
            return achievements;
        }
        
        return achievements;
    }

    private async Task<JObject> GetNPCDialogueData(string npcName)
    {
        // Check cache first
        if (dialogueCache.ContainsKey(npcName))
        {
            return dialogueCache[npcName];
        }
        
        // Try to load dialogue file from resources
        try
        {
            TextAsset dialogueFile = null;
            
            // Check if this is a tutorial NPC
            dialogueFile = Resources.Load<TextAsset>($"DialogueData/Tutorial{npcName}");
            
            // If not found, try unit lesson NPCs
            if (dialogueFile == null)
            {
                // We'd need to implement a mapping system for unit NPCs
                // For now, we'll return null for non-tutorial NPCs
                return null;
            }
            
            if (dialogueFile != null)
            {
                JObject dialogueData = JObject.Parse(dialogueFile.text);
                dialogueCache[npcName] = dialogueData; // Cache for future use
                return dialogueData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not load dialogue for {npcName}: {ex.Message}");
        }
        
        return null;
    }

    private bool CreateAchievementEntry(AchievementData achievement)
    {
        if (achievementEntryPrefab == null || achievementsContainer == null)
        {
            Debug.LogError("Cannot create achievement entry: prefab or container is null");
            return false;
        }
        
        try
        {
            // Instantiate the prefab
            GameObject entryObject = Instantiate(achievementEntryPrefab, achievementsContainer);
            
            if (entryObject == null)
            {
                Debug.LogError("Failed to instantiate achievement entry prefab");
                return false;
            }
            
            // Set fixed height directly with LayoutElement instead of using a custom component
            RectTransform rect = entryObject.GetComponent<RectTransform>();
            LayoutElement layoutElement = entryObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = entryObject.AddComponent<LayoutElement>();
            }
            
            // Set preferred height and prevent flexible height expansion
            layoutElement.preferredHeight = 180f;
            layoutElement.minHeight = 180f;
            layoutElement.flexibleHeight = 0;
            
            // Get references to the UI components
            Image rewardImage = entryObject.transform.Find("RewardImageIcon")?.GetComponent<Image>();
            TextMeshProUGUI titleText = entryObject.transform.Find("RewardTitleText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = entryObject.transform.Find("RewardDescriptionText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI dateText = entryObject.transform.Find("RewardDateText")?.GetComponent<TextMeshProUGUI>();
            
            // Check if components are found
            if (rewardImage == null) Debug.LogError("RewardImageIcon not found or missing Image component");
            if (titleText == null) Debug.LogError("RewardTitleText not found or missing TextMeshProUGUI component");
            if (descText == null) Debug.LogError("RewardDescriptionText not found or missing TextMeshProUGUI component");
            if (dateText == null) Debug.LogError("RewardDateText not found or missing TextMeshProUGUI component");
            
            // Set the icon
            if (rewardImage != null)
            {
                if (rewardSprites.TryGetValue(achievement.RewardSprite, out Sprite sprite))
                {
                    rewardImage.sprite = sprite;
                    rewardImage.preserveAspect = true;
                    rewardImage.color = Color.white;
                    Debug.Log($"Set reward sprite: {achievement.RewardSprite} for {achievement.Title}");
                }
                else
                {
                    Debug.LogWarning($"No sprite found for reward: {achievement.RewardSprite}");
                    rewardImage.color = new Color(1f, 1f, 1f, 0.3f); // Make it semi-transparent
                }
            }
            
            // Set the title
            if (titleText != null)
            {
                titleText.text = achievement.Title;
            }
            
            // Set the description
            if (descText != null)
            {
                descText.text = achievement.Description;
            }
            
            // Set the date
            if (dateText != null)
            {
                dateText.text = achievement.Date.ToString("MMM dd, yyyy - h:mm tt");
            }
            
            Debug.Log($"Created achievement entry: {achievement.Title} - {achievement.RewardSprite}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating achievement entry: {ex.Message}");
            return false;
        }
    }

    private DateTime ParseDateTime(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.Now;
            
        try
        {
            // Try ISO format first (used by MongoDB/API)
            if (DateTime.TryParse(dateString, out DateTime result))
                return result;
                
            // Try other common formats
            string[] formats = { 
                "yyyy-MM-ddTHH:mm:ss.fffZ", 
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss" 
            };
            
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, 
                                     DateTimeStyles.None, out result))
            {
                return result;
            }
            
            Debug.LogWarning($"Could not parse date: {dateString}, using current time");
            return DateTime.Now;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing date '{dateString}': {ex.Message}");
            return DateTime.Now;
        }
    }

    private string FormatUnitName(string unitKey)
    {
        // Convert "Unit1" to "UNIT 1"
        return unitKey.Replace("Unit", "UNIT ").Insert(5, " ").Trim();
    }

    private string FormatLessonName(string lessonKey)
    {
        // Convert "Lesson1" to "Lesson 1"
        return lessonKey.Insert(6, " ").Trim();
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (refreshButton != null)
            refreshButton.onClick.RemoveAllListeners();
            
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }

    // Data class to hold achievement information
    [System.Serializable]
    private class AchievementData
    {
        public string Source { get; set; } = ""; // Tutorial, UNIT 1, etc.
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string RewardSprite { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public string NpcName { get; set; } = "";
    }
}
