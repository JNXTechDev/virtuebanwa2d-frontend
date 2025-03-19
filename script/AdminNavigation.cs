using UnityEngine;
using UnityEngine.SceneManagement;

public class AdminNavigation : MonoBehaviour
{
    [Header("Panels")]
    public GameObject teacherListPanel;
    public GameObject teacherDetailPanel;
    public GameObject dashboardPanel;
    
    private void Start()
    {
        // Ensure the admin role is set in PlayerPrefs
        if (PlayerPrefs.GetString("Role", "") != "Admin")
        {
            Debug.LogWarning("Non-admin user attempting to access admin panel. Setting admin role.");
            PlayerPrefs.SetString("Role", "Admin");
            PlayerPrefs.SetString("Username", "admin1");
            PlayerPrefs.Save();
        }
        
        // Default panel visibility
        if (teacherDetailPanel != null)
            teacherDetailPanel.SetActive(false);
            
        if (dashboardPanel != null)
            dashboardPanel.SetActive(true);
            
        if (teacherListPanel != null)
            teacherListPanel.SetActive(true);
    }
    
    // Button handler to show the teacher list panel
    public void ShowTeacherListPanel()
    {
        if (teacherListPanel != null)
            teacherListPanel.SetActive(true);
            
        if (teacherDetailPanel != null)
            teacherDetailPanel.SetActive(false);
    }
    
    // Button handler to return to main menu
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("CreateorLogin");
    }
    
    // Button handler to log out
    public void LogOut()
    {
        // Clear user session data
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.DeleteKey("Section");
        PlayerPrefs.DeleteKey("FirstName");
        PlayerPrefs.DeleteKey("LastName");
        PlayerPrefs.DeleteKey("Character");
        PlayerPrefs.Save();
        
        // Return to login screen
        SceneManager.LoadScene("CreateorLogin");
    }
}
