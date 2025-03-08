using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // Add this namespace

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI DialogueText;
    public TextMeshProUGUI NameTxt;
    public TextMeshProUGUI QuestInstructionTxt;
    public string npcName;
    public string[] lines;
    public string questInstructions;
    public string newQuestInstructions;
    public float textSpeed = 0.05f;
    private int index;
    public GameObject playerUsernameObject;

    private InputAction clickAction; // Input action for mouse/touch

    void Start()
    {
        DialogueText.text = string.Empty;
        NameTxt.text = npcName;
        QuestInstructionTxt.text = questInstructions;
        StartDialogue();

        // Initialize the new Input System
        var inputActionAsset = new InputActionAsset();
        clickAction = new InputAction("Click", binding: "<Mouse>/leftButton");
        clickAction.AddBinding("<Touchscreen>/press");
        clickAction.Enable();
    }

    void StartDialogue()
    {
        index = 0;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            DialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    public void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            DialogueText.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            StartCoroutine(EndDialogue());
        }
    }

    IEnumerator EndDialogue()
    {
        yield return new WaitForSeconds(0.5f);

        if (!string.IsNullOrEmpty(newQuestInstructions))
        {
            QuestInstructionTxt.text = newQuestInstructions;
            yield return new WaitForSeconds(0.5f);
        }

        gameObject.SetActive(false);
    }

    private string GetCurrentUsername()
    {
        if (playerUsernameObject != null)
        {
            TMP_Text usernameText = playerUsernameObject.GetComponent<TMP_Text>();
            if (usernameText != null)
            {
                return usernameText.text;
            }
        }
        return PlayerPrefs.GetString("PlayerUsername", "Player");
    }

    // Method to load dialogue dynamically
    public void LoadDialogue(string[] newLines, string newNpcName, string newQuestInstructions)
    {
        lines = newLines;
        npcName = newNpcName;
        questInstructions = newQuestInstructions;
        NameTxt.text = newNpcName;
        QuestInstructionTxt.text = newQuestInstructions;
        StartDialogue();
    }

    void Update()
    {
        // Check for mouse click or touch using the new Input System
        if (clickAction.triggered)
        {
            if (DialogueText.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                DialogueText.text = lines[index];
            }
        }
    }

    void OnDestroy()
    {
        // Clean up the input action
        clickAction.Disable();
    }
}