using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;
using System.Linq;

public class AdminManager : MonoBehaviour
{
    // UI References
    [Header("Teacher List Panel")]
    public GameObject scrollViewTeacher;
    public GameObject teacherRowPrefab;
    public Transform teacherListContent;

    [Header("Teacher Details Panel")]
    public GameObject viewTeacherPanel;
    public TMP_Text teacherNamePanelText;
    public TMP_Text teacherEmployeeIDPanelText;
    public TMP_Text teacherUsernamePanelText;
    public Button acceptTeacherButton;
    public Button removeTeacherButton;

    [Header("Status Filters")]
    public Button allButton; // New button for showing all teachers
    public Button pendingButton;
    public Button approvedButton;
    public Button rejectedButton;
    
    [Header("Status Indicators")]
    public Color pendingColor = Color.yellow;
    public Color approvedColor = Color.green;
    public Color rejectedColor = Color.red;

    // Data tracking
    private List<TeacherData> allTeachers = new List<TeacherData>();
    private TeacherData selectedTeacher;
    private string currentStatus = "All"; // Default to show all teachers

    void Start()
    {
        // Setup buttons
        acceptTeacherButton.onClick.AddListener(AcceptTeacher);
        removeTeacherButton.onClick.AddListener(RejectTeacher);
        
        if (allButton != null) allButton.onClick.AddListener(() => FilterTeachers("All"));
        if (pendingButton != null) pendingButton.onClick.AddListener(() => FilterTeachers("Pending"));
        if (approvedButton != null) approvedButton.onClick.AddListener(() => FilterTeachers("Approved"));
        if (rejectedButton != null) rejectedButton.onClick.AddListener(() => FilterTeachers("Rejected"));
        
        // Hide teacher details panel initially
        if (viewTeacherPanel != null) 
            viewTeacherPanel.SetActive(false);
        
        // Load all teachers on start
        LoadAllTeachers();
    }

    // Load all teachers from the server
    public async void LoadAllTeachers()
    {
        try {
            // Clear the list first
            allTeachers.Clear();
            ClearTeacherList();
            
            // Fetch all categories of teachers
            var pendingTeachers = await FetchTeachers("Pending");
            var approvedTeachers = await FetchTeachers("Approved");
            var rejectedTeachers = await FetchTeachers("Rejected");
            
            // Combine all teachers into one list
            allTeachers.AddRange(pendingTeachers);
            allTeachers.AddRange(approvedTeachers);
            allTeachers.AddRange(rejectedTeachers);
            
            Debug.Log($"Loaded {allTeachers.Count} teachers in total");
            
            // Apply the current filter (which defaults to "All")
            FilterTeachers(currentStatus);
        }
        catch (Exception e) {
            Debug.LogError($"Error loading all teachers: {e.Message}");
        }
    }

    // Filter teachers based on their status
    public void FilterTeachers(string status)
    {
        currentStatus = status;
        ClearTeacherList();
        
        IEnumerable<TeacherData> filteredTeachers = allTeachers;
        
        // Apply filter if not showing all
        if (status != "All")
        {
            filteredTeachers = allTeachers.Where(t => t.AdminApproval == status);
        }
        
        // Display the filtered teachers
        foreach (var teacher in filteredTeachers)
        {
            CreateTeacherRow(teacher);
        }
        
        // Update UI to show active filter
        UpdateFilterButtonsUI(status);
        
        Debug.Log($"Filtered to {filteredTeachers.Count()} teachers with status: {status}");
    }

    // Fetch teachers from server based on approval status
    private async Task<List<TeacherData>> FetchTeachers(string status)
    {
        string endpoint = status.ToLower();
        string url = $"{NetworkConfig.BaseUrl}/teacher/{endpoint}";
        Debug.Log($"Fetching teachers with status '{status}' from: {url}");

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Teacher data received: {json}");
                    
                    // Parse the JSON array
                    TeacherListResponse teacherResponse = JsonUtility.FromJson<TeacherListResponse>("{\"teachers\":" + json + "}");
                    return teacherResponse.teachers;
                }
                else
                {
                    Debug.LogError($"Failed to fetch teachers: {response.StatusCode}");
                    return new List<TeacherData>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception fetching teachers: {e.Message}");
                return new List<TeacherData>();
            }
        }
    }

    // Create UI row for a teacher
    private void CreateTeacherRow(TeacherData teacher)
    {
        GameObject row = Instantiate(teacherRowPrefab, teacherListContent);
        
        // Set the teacher name
        TMP_Text nameText = row.transform.Find("TeacherNameText")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = $"{teacher.FirstName} {teacher.LastName}";
            
            // Find or add a button component to the name text game object
            Button nameButton = nameText.GetComponent<Button>();
            if (nameButton == null)
            {
                nameButton = nameText.gameObject.AddComponent<Button>();
                // Add a color transition for visual feedback
                ColorBlock colors = nameButton.colors;
                colors.highlightedColor = new Color(0.9f, 0.9f, 1f);
                colors.pressedColor = new Color(0.8f, 0.8f, 1f);
                nameButton.colors = colors;
            }
            
            // Clear existing listeners and add our click handler
            nameButton.onClick.RemoveAllListeners();
            nameButton.onClick.AddListener(() => {
                Debug.Log($"Teacher name clicked: {teacher.FirstName} {teacher.LastName}");
                ShowTeacherDetails(teacher);
            });
        }
        
        // Set status text and color
        TMP_Text statusText = row.transform.Find("TeacherStatusText")?.GetComponent<TMP_Text>();
        if (statusText != null)
        {
            statusText.text = teacher.AdminApproval;
            
            // Apply color based on status
            switch (teacher.AdminApproval)
            {
                case "Pending":
                    statusText.color = pendingColor;
                    break;
                case "Approved":
                    statusText.color = approvedColor;
                    break;
                case "Rejected":
                    statusText.color = rejectedColor;
                    break;
            }
        }
        
        // Make the entire row clickable as well for better UX
        Button rowButton = row.GetComponent<Button>();
        if (rowButton == null)
            rowButton = row.AddComponent<Button>();
            
        // Store teacher data reference in the button
        TeacherRowData rowData = row.GetComponent<TeacherRowData>();
        if (rowData == null)
            rowData = row.AddComponent<TeacherRowData>();
            
        rowData.teacherData = teacher;
        
        // Clear existing listeners and add our click handler
        rowButton.onClick.RemoveAllListeners();
        rowButton.onClick.AddListener(() => {
            Debug.Log($"Teacher row clicked: {teacher.FirstName} {teacher.LastName}");
            ShowTeacherDetails(rowData.teacherData);
        });
    }

    // Display teacher details in the panel
    public void ShowTeacherDetails(TeacherData teacher)
    {
        Debug.Log($"Showing details for teacher: {teacher.FirstName} {teacher.LastName}");
        
        selectedTeacher = teacher;
        
        // Update panel texts
        teacherNamePanelText.text = $"{teacher.FirstName} {teacher.LastName}";
        teacherEmployeeIDPanelText.text = $"Employee ID: {teacher.EmployeeID ?? "Not provided"}";
        teacherUsernamePanelText.text = $"Username: {teacher.Username}";
        
        // Show or hide buttons based on current status
        bool isPending = teacher.AdminApproval == "Pending";
        acceptTeacherButton.gameObject.SetActive(isPending || teacher.AdminApproval == "Rejected");
        acceptTeacherButton.GetComponentInChildren<TMP_Text>().text = 
            isPending ? "Approve Teacher" : "Restore Access";

        // Make sure the panel is active in the hierarchy
        viewTeacherPanel.SetActive(true);
        
        // Ensure the panel is visible (in case it's hidden by canvas groups or other UI)
        CanvasGroup panelCanvasGroup = viewTeacherPanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
    }

    // Approve the selected teacher
    public async void AcceptTeacher()
    {
        if (selectedTeacher == null) return;
        
        try
        {
            bool success = await UpdateTeacherStatus(selectedTeacher.Username, "Approved");
            if (success)
            {
                HandleTeacherStatusUpdated(selectedTeacher.Username, "Approved");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error approving teacher: {e.Message}");
        }
    }

    // Reject the selected teacher
    public async void RejectTeacher()
    {
        if (selectedTeacher == null) return;
        
        try
        {
            bool success = await UpdateTeacherStatus(selectedTeacher.Username, "Rejected");
            if (success)
            {
                HandleTeacherStatusUpdated(selectedTeacher.Username, "Rejected");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error rejecting teacher: {e.Message}");
        }
    }

    // Update teacher's approval status on the server
    private async Task<bool> UpdateTeacherStatus(string username, string status)
    {
        string url = $"{NetworkConfig.BaseUrl}/teacher/status";
        
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Create request body
                string json = JsonUtility.ToJson(new TeacherStatusUpdateRequest
                {
                    Username = username,
                    Status = status
                });
                
                StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // Send the request
                HttpResponseMessage response = await client.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"Successfully updated teacher status to {status}");
                    return true;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Failed to update teacher status: {response.StatusCode}, {errorResponse}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception updating teacher status: {e.Message}");
                return false;
            }
        }
    }

    // Clear the teacher list UI
    private void ClearTeacherList()
    {
        foreach (Transform child in teacherListContent)
        {
            Destroy(child.gameObject);
        }
    }
    
    // Update filter button UI states
    private void UpdateFilterButtonsUI(string activeStatus)
    {
        void UpdateButtonState(Button button, bool isActive)
        {
            if (button != null)
            {
                // Change color or add visual indicator to show active state
                ColorBlock colors = button.colors;
                colors.normalColor = isActive ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
                button.colors = colors;
            }
        }
        
        UpdateButtonState(allButton, activeStatus == "All");
        UpdateButtonState(pendingButton, activeStatus == "Pending");
        UpdateButtonState(approvedButton, activeStatus == "Approved");
        UpdateButtonState(rejectedButton, activeStatus == "Rejected");
    }

    // Refresh the teacher list with the most current data
    public void RefreshTeacherList()
    {
        LoadAllTeachers();
    }

    // Close the teacher details panel
    public void CloseTeacherPanel()
    {
        viewTeacherPanel.SetActive(false);
        selectedTeacher = null;
    }

    // Add a debug method that can be called from the Unity Editor to check if the panel works
    public void DebugShowPanel()
    {
        Debug.Log("Debug: Attempting to show teacher panel");
        
        if (viewTeacherPanel != null)
        {
            viewTeacherPanel.SetActive(true);
            Debug.Log("Debug: Panel should be visible now");
        }
        else
        {
            Debug.LogError("Debug: viewTeacherPanel is null!");
        }
    }

    // Add an OnEnable method to ensure proper setup
    private void OnEnable()
    {
        // Reset the panel state when the script becomes active
        if (viewTeacherPanel != null)
        {
            viewTeacherPanel.SetActive(false);
        }
        
        // Double check button listeners
        if (acceptTeacherButton != null && acceptTeacherButton.onClick.GetPersistentEventCount() == 0)
        {
            acceptTeacherButton.onClick.AddListener(AcceptTeacher);
        }
        
        if (removeTeacherButton != null && removeTeacherButton.onClick.GetPersistentEventCount() == 0)
        {
            removeTeacherButton.onClick.AddListener(RejectTeacher);
        }
    }

    // After successfully updating a teacher status
    private void HandleTeacherStatusUpdated(string username, string newStatus)
    {
        // Update the status in our cached list
        var teacher = allTeachers.FirstOrDefault(t => t.Username == username);
        if (teacher != null)
        {
            teacher.AdminApproval = newStatus;
        }
        
        // Reapply the current filter to update the UI
        FilterTeachers(currentStatus);
        
        // Close the panel
        viewTeacherPanel.SetActive(false);
    }
}

// Helper class to store teacher data reference in UI rows
public class TeacherRowData : MonoBehaviour
{
    public TeacherData teacherData;
}

// Classes for JSON serialization/deserialization
[Serializable]
public class TeacherData
{
    public string _id;
    public string Username;
    public string FirstName;
    public string LastName;
    public string EmployeeID;
    public string Role;
    public string AdminApproval;
    public string FullName;
    public string Character;
}

[Serializable]
public class TeacherListResponse
{
    public List<TeacherData> teachers;
}

[Serializable]
public class TeacherStatusUpdateRequest
{
    public string Username;
    public string Status;
}
