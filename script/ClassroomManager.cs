using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class ClassroomManager : MonoBehaviour
{
    private const string baseUrl = "http://192.168.1.11:5000/api"; // Ensure this matches your server's IP and port

    [SerializeField] private GameObject createClassroomPanel;
    [SerializeField] private TMP_InputField classroomNameInputField;
    [SerializeField] private TMP_InputField generateCodeInputField;
    [SerializeField] private Button generateButton;
    [SerializeField] private Button createButton;
    [SerializeField] private GameObject sectionTablePanel;
    [SerializeField] private GameObject sectionButtonPrefab;
    [SerializeField] private GameObject confirmationPopup;

    private string loggedInUsername = "mc102010"; // Replace with dynamic username if needed
    private string classroomToDeleteId;
    private bool sceneChangeFlag = false;

    private void Start()
    {
        try
        {
            generateButton.onClick.AddListener(GenerateUniqueCode);
            createButton.onClick.AddListener(CreateClassroom);
            DisplayClassrooms();

            if (confirmationPopup != null)
            {
                confirmationPopup.SetActive(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ClassroomManager initialization warning: {ex.Message}");
        }
    }

    private void SetupSectionButtons()
    {
        Debug.Log("Setting up section buttons...");

        foreach (Transform child in sectionTablePanel.transform)
        {
            Destroy(child.gameObject);
        }

        GridLayoutGroup grid = sectionTablePanel.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(220, 60);
            grid.spacing = new Vector2(220, 70);
        }
    }

    public void OnSectionButtonClicked(Button buttonObject)
    {
        if (!sceneChangeFlag)
        {
            string sectionName = buttonObject.GetComponentInChildren<TMP_Text>().text;
            Debug.Log($"{sectionName} button clicked.");

            string classroomCode = ExtractClassroomCodeFromText(sectionName);

            PlayerPrefs.SetString("SelectedClassroomCode", classroomCode);
            PlayerPrefs.SetString("SelectedClassroomName", sectionName);

            UnityEngine.SceneManagement.SceneManager.LoadScene("ClassroomMonitor");
        }
    }

    private string ExtractClassroomCodeFromText(string text)
    {
        string[] parts = text.Split(' ');
        return parts.Length > 1 ? parts[1] : "UnknownCode";
    }

    private void GenerateUniqueCode()
    {
        string uniqueCode = Guid.NewGuid().ToString().Substring(0, 8);
        generateCodeInputField.text = uniqueCode;
    }

    private void CreateClassroom()
    {
        string classroomName = classroomNameInputField.text;
        string uniqueCode = generateCodeInputField.text;

        if (string.IsNullOrEmpty(classroomName) || string.IsNullOrEmpty(uniqueCode))
        {
            Debug.LogWarning("Classroom name or code cannot be empty.");
            return;
        }

        StartCoroutine(CreateClassroomRequest(classroomName, uniqueCode));
    }

    private IEnumerator CreateClassroomRequest(string classroomName, string uniqueCode)
    {
        var classroomData = new ClassroomData
        {
            name = classroomName,
            code = uniqueCode,
            teacherUsername = loggedInUsername
        };

        string json = JsonUtility.ToJson(classroomData);
        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/classrooms", "POST"))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Classroom created successfully.");
                DisplayClassrooms();
                createClassroomPanel.SetActive(false);
            }
            else
            {
                Debug.LogError($"Failed to create classroom: {request.error}");
            }
        }
    }

    public void OpenCreateClassroomPanel()
    {
        createClassroomPanel.SetActive(true);
    }

    private void DisplayClassrooms()
    {
        StartCoroutine(FetchClassrooms());
    }

    private IEnumerator FetchClassrooms()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/classrooms?teacherUsername={loggedInUsername}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseContent = request.downloadHandler.text;
                Debug.Log($"Retrieved classrooms: {responseContent}");

                // Deserialize the response into a list of classrooms
                var classrooms = JsonUtility.FromJson<ClassroomList>("{\"classrooms\":" + responseContent + "}");

                foreach (Transform child in sectionTablePanel.transform)
                {
                    Destroy(child.gameObject);
                }

                SetupSectionButtons();

                foreach (var classroom in classrooms.classrooms)
                {
                    GameObject buttonObject = Instantiate(sectionButtonPrefab, sectionTablePanel.transform);

                    if (buttonObject == null)
                    {
                        Debug.LogError("Button instantiation failed.");
                        continue;
                    }

                    Button buttonComponent = buttonObject.GetComponent<Button>();
                    TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
                    LongPressHandler longPressHandler = buttonObject.GetComponent<LongPressHandler>();

                    if (buttonComponent == null || buttonText == null || longPressHandler == null)
                    {
                        Debug.LogError("Button component, text component, or LongPressHandler is missing.");
                        continue;
                    }

                    buttonText.text = classroom.name;
                    Debug.Log($"Creating button for classroom: {classroom.name}");

                    buttonComponent.onClick.AddListener(() => OnSectionButtonClicked(buttonComponent));

                    string classroomId = classroom.code;
                    longPressHandler.OnLongPress.AddListener(() => ShowConfirmationPopup(classroomId));
                }
            }
            else
            {
                Debug.LogError($"Failed to retrieve classrooms: {request.error}");
            }
        }
    }

    private void ShowConfirmationPopup(string classroomId)
    {
        classroomToDeleteId = classroomId;
        sceneChangeFlag = false; // Ensure no scene change occurs until confirmed
        confirmationPopup.SetActive(true);
    }

    private void OnConfirmDelete()
    {
        if (!string.IsNullOrEmpty(classroomToDeleteId))
        {
            StartCoroutine(DeleteClassroom(classroomToDeleteId));
        }
    }

    private IEnumerator DeleteClassroom(string classroomId)
    {
        using (UnityWebRequest request = UnityWebRequest.Delete($"{baseUrl}/classrooms/{classroomId}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Classroom deleted successfully.");
                DisplayClassrooms();
            }
            else
            {
                Debug.LogError($"Failed to delete classroom: {request.error}");
            }
        }
    }
}

// Define classes for JSON serialization/deserialization
[System.Serializable]
public class ClassroomData
{
    public string name;
    public string code;
    public string teacherUsername;
}

[System.Serializable]
public class ClassroomList
{
    public List<ClassroomData> classrooms;
}