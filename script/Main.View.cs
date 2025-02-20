using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System;
using UnityEngine.UI;

public class MainView : MonoBehaviour
{
    // UI Elements
    public TMP_Text playerNameText;
    public TMP_Text classroomNameText;
    public GameObject boySprite;
    public GameObject girlSprite;
    public GameObject talkButton;
    public GameObject interactButton;
    public GameObject txtBox1;
    public GameObject npcGirl1;
    public GameObject profilePictureButton;
    public GameObject profileExitButton;
    public GameObject viewProfilePanel;
    public TMP_Text usernameText;
    public TMP_Text detailText;
    public TMP_Text sectionText;
    public TMP_Text roleText;
    public TMP_Text progressText;
    public TMP_Text rewardText;
    public Transform playerTransform;
    public Vector3 cameraOffset;
    public float smoothSpeed = 0.125f;
    public GameObject DialogueBoxPanel;
    public TMP_Text DialogueText;
    public GameObject congratsPanel;
    public TMP_Text congratsText;
    public Image spriteImage;
    public Image spriteImage2;
    public Sprite boyHead;
    public Sprite girlHead;

    // MongoDB API URL
  //  private const string baseUrl = "http://192.168.1.98:5000/api";
    private const string baseUrl = "https://vbdb.onrender.com/api";


    // Camera Follow reference
    public CameraFollow cameraFollow;

    private bool isNearNpc = false;

    // Start method
    void Start()
    {
        InitializeUI();
        interactButton.SetActive(true);
        DisplayPlayerInfo();
    }

    // Display player information using PlayerPrefs
    private void DisplayPlayerInfo()
    {
        // Retrieve user data from PlayerPrefs
        string username = PlayerPrefs.GetString("Username");
        string role = PlayerPrefs.GetString("Role");
        string section = PlayerPrefs.GetString("Section");
        string firstName = PlayerPrefs.GetString("FirstName");
        string lastName = PlayerPrefs.GetString("LastName");
        string character = PlayerPrefs.GetString("Character");

        // Debug logs to verify retrieved data
        Debug.Log($"Retrieved from PlayerPrefs - Username: {username}, Role: {role}, Section: {section}, FirstName: {firstName}, LastName: {lastName}, Character: {character}");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
        {
            Debug.LogError("Username or role is not set in PlayerPrefs.");
            playerNameText.text = "Player: Unknown";
            return;
        }

        // Combine FirstName and LastName
        string fullName = $"{firstName} {lastName}";

        // Display player information for the profile panel not yet open 
        playerNameText.text = fullName; // Ensure this line is correct

        // Display player information for the profile panel when open
        usernameText.text = $"#{username}";
        detailText.text = fullName;
        sectionText.text = $"{section}";
        roleText.text = $"{role}";

        Debug.Log($"Character selected: {character}");
        Debug.Log($"Full Name: {fullName}");
        ShowCharacterSprite(character);

        // Set the playerTransform for CameraFollow
        if (character == "Boy")
        {
            playerTransform = boySprite.transform;
        }
        else if (character == "Girl")
        {
            playerTransform = girlSprite.transform;
        }
        else
        {
            Debug.LogWarning("Character data not found or is invalid.");
        }

        // Initialize CameraFollow
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(); // Ensure the target is set correctly
        }
        else
        {
            Debug.LogError("CameraFollow reference is not set in the Inspector.");
        }

        DisplayClassroomInfo();
    }

    // Display classroom information using PlayerPrefs
    private void DisplayClassroomInfo()
    {
        string section = PlayerPrefs.GetString("Section");

        if (string.IsNullOrEmpty(section))
        {
            Debug.LogError("Section is not set in PlayerPrefs.");
            classroomNameText.text = "Section: Not enrolled.";
            return;
        }

        classroomNameText.text = $"{section}";
        Debug.Log($"Updated classroomNameText: {classroomNameText.text}");
    }

    // Show character sprite based on selection
    public void ShowCharacterSprite(string character)
    {
        if (boySprite != null && girlSprite != null)
        {
            boySprite.SetActive(false);
            girlSprite.SetActive(false);

            if (character == "Boy")
            {
                boySprite.SetActive(true);
                Debug.Log("Boy sprite activated.");
                if (spriteImage != null)
                {
                    spriteImage.sprite = boyHead;
                }
                if (spriteImage2 != null)
                {
                    spriteImage2.sprite = boyHead;
                }
            }
            else if (character == "Girl")
            {
                girlSprite.SetActive(true);
                Debug.Log("Girl sprite activated.");
                if (spriteImage != null)
                {
                    spriteImage.sprite = girlHead;
                }
                if (spriteImage2 != null)
                {
                    spriteImage2.sprite = girlHead;
                }
            }
            else
            {
                Debug.LogWarning("Character data not found or is invalid.");
            }
        }
        else
        {
            Debug.LogError("Character sprites are not set in the Inspector.");
        }
    }

    // Initialize UI elements
    private void InitializeUI()
    {
        if (profilePictureButton != null)
        {
            profilePictureButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnProfilePictureButtonClicked);
        }
        else
        {
            Debug.LogError("ProfilePictureButton reference is not set in the Inspector.");
        }

        if (profileExitButton != null)
        {
            profileExitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnProfileExitButtonClicked);
        }
        else
        {
            Debug.LogError("ProfileExitButton reference is not set in the Inspector.");
        }

        if (viewProfilePanel != null)
        {
            viewProfilePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("ViewProfilePanel reference is not set in the Inspector.");
        }

        if (talkButton != null)
        {
            talkButton.SetActive(true);
        }
        else
        {
            Debug.LogError("TalkButton reference is not set in the Inspector.");
        }

        if (txtBox1 != null)
        {
            txtBox1.SetActive(false);
        }
        else
        {
            Debug.LogError("TxtBox1 reference is not set in the Inspector.");
        }

        if (congratsPanel != null)
        {
            congratsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("CongratsPanel reference is not set in the Inspector.");
        }

        if (DialogueBoxPanel != null)
        {
            DialogueBoxPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("DialogueBoxPanel reference is not set in the Inspector.");
        }

        GameObject logoutButton = GameObject.Find("LogoutButton");
        if (logoutButton != null)
        {
            logoutButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Logout);
        }
        else
        {
            Debug.LogError("LogoutButton reference is not set in the Inspector.");
        }
    }

    // Update method for frame updates
    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 desiredPosition = playerTransform.position + cameraOffset;
            Vector3 smoothedPosition = Vector3.Lerp(Camera.main.transform.position, desiredPosition, smoothSpeed);
            Camera.main.transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, Camera.main.transform.position.z);
        }

        if (isNearNpc)
        {
            if (talkButton != null)
            {
                talkButton.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TalkToNpc();
            }
        }
        else
        {
            if (talkButton != null)
            {
                talkButton.SetActive(false);
            }
        }
    }

    private void DisplayPlayerProfileInfo()
    {
        // Retrieve user data from PlayerPrefs
        string username = PlayerPrefs.GetString("Username");
        string firstName = PlayerPrefs.GetString("FirstName");
        string lastName = PlayerPrefs.GetString("LastName");
        string section = PlayerPrefs.GetString("Section");
        string role = PlayerPrefs.GetString("Role");
        string progress = PlayerPrefs.GetString("Progress");
        string rewards = PlayerPrefs.GetString("Rewards");

        // Update UI elements with user data
        usernameText.text = $"#{username}";
        detailText.text = $"{firstName} {lastName}";
        sectionText.text = $"{section}";
        roleText.text = $"{role}";
        progressText.text = $"{progress}";
        rewardText.text = $"{rewards}";
    }

    // Handle profile picture button click
    private void OnProfilePictureButtonClicked()
    {
        if (viewProfilePanel != null)
        {
            viewProfilePanel.SetActive(true);
            DisplayPlayerProfileInfo();
        }
    }

    // Handle profile exit button click
    private void OnProfileExitButtonClicked()
    {
        if (viewProfilePanel != null)
        {
            viewProfilePanel.SetActive(false);
        }
    }

    // Handle NPC trigger enter
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("NPC"))
        {
            isNearNpc = true;
        }
    }

    // Handle NPC trigger exit
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("NPC"))
        {
            isNearNpc = false;
        }
    }

    // Talk to NPC
    private void TalkToNpc()
    {
        ShowIntroDialogue();
    }

    // Show introductory dialogue
    private void ShowIntroDialogue()
    {
        if (DialogueBoxPanel != null)
        {
            DialogueBoxPanel.SetActive(true);
            DialogueText.text = "Hello! Welcome to the game!";
        }
    }

    // Show congratulations message
    private void ShowCongratulations()
    {
        if (congratsPanel != null)
        {
            congratsPanel.SetActive(true);
            congratsText.text = "Congratulations! You've completed Lesson 1 of Unit 1!";
        }
    }

    // Method to handle logout
    public void Logout()
    {
        // Clear user data from PlayerPrefs
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.DeleteKey("Section");
        PlayerPrefs.DeleteKey("FirstName");
        PlayerPrefs.DeleteKey("LastName");
        PlayerPrefs.DeleteKey("Character");
        PlayerPrefs.DeleteKey("Progress");
        PlayerPrefs.DeleteKey("Rewards");
        PlayerPrefs.Save();

        Debug.Log("UserData cleared from PlayerPrefs on logout.");

        // Optionally, update the UI to reflect the logged-out state
        UpdateUIAfterLogout();

        // Load the CreateorLogIn scene
        SceneManager.LoadScene("CreateorLogIn");
    }

    private void UpdateUIAfterLogout()
    {
        playerNameText.text = "Player: Unknown";
        classroomNameText.text = "Section: Not enrolled.";
    }
}