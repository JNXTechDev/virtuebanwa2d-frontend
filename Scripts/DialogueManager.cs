using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using VirtueBanwa;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcText;
    public GameObject[] choiceButtons;
    public GameObject rewardPanel;
    public Image rewardImage;
    public TextMeshProUGUI rewardText;
    
    [Header("Configuration")]
    public float typingSpeed = 0.05f;
    public string lessonFile = "lesson1";

    private DialogueData currentDialogue;
    private bool isInDialogue = false;
    private List<DialogueData> currentLessonStories;
    private int currentStoryIndex = 0;

    void Start()
    {
        LoadDialogueData();
        HideAllPanels();
    }

    void LoadDialogueData()
    {
        // Get current lesson data
        string currentLesson = PlayerPrefs.GetString("CurrentLesson", "Lesson 1");
        string lessonPath = currentLesson.Contains("2") ? 
            "DialogueData/Unit1Lesson2" : 
            "DialogueData/Unit1Lesson1";

        Debug.Log($"DialogueManager: Loading dialogue from: {lessonPath}");
        
        TextAsset jsonFile = Resources.Load<TextAsset>(lessonPath);
        if (jsonFile == null)
        {
            Debug.LogError($"Could not load dialogue file at {lessonPath}");
            return;
        }

        // For Lesson 2 (array of stories)
        if (currentLesson.Contains("2"))
        {
            string wrappedJson = $"{{\"stories\":{jsonFile.text}}}";
            var wrapper = JsonUtility.FromJson<StoriesWrapper>(wrappedJson);
            currentLessonStories = wrapper.stories;
            currentDialogue = currentLessonStories[0]; // Start with first story
        }
        // For Lesson 1 (single story)
        else
        {
            currentDialogue = JsonUtility.FromJson<DialogueData>(jsonFile.text);
        }

        // Ensure intro dialogue panel shows correct text
        if (npcText != null)
        {
            npcText.text = currentDialogue.initialDialogue;
        }
    }

    [System.Serializable]
    private class StoriesWrapper
    {
        public List<DialogueData> stories;
    }

    public void StartDialogue()
    {
        if (!isInDialogue)
        {
            isInDialogue = true;
            dialoguePanel.SetActive(true);
            StartCoroutine(TypeDialogue(currentDialogue.initialDialogue));
        }
    }

    IEnumerator TypeDialogue(string text)
    {
        npcText.text = "";
        foreach (char c in text.ToCharArray())
        {
            npcText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        ShowChoices();
    }

    void ShowChoices()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].SetActive(true);
            choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = 
                currentDialogue.choices[i].text;
        }
    }

    public void OnChoiceSelected(int choice)
    {
        HideChoices();
        StartCoroutine(ShowRewardSequence(currentDialogue.choices[choice]));
    }

    IEnumerator ShowRewardSequence(Choice choice)
    {
        // Show NPC response
        yield return TypeDialogue(choice.response);
        yield return new WaitForSeconds(1f);

        // Show reward
        rewardPanel.SetActive(true);
        rewardText.text = choice.reward.message;
        rewardImage.sprite = Resources.Load<Sprite>($"Rewards/{choice.reward.sprite}");

        yield return new WaitForSeconds(3f);
        HideAllPanels();
        isInDialogue = false;
    }

    void HideAllPanels()
    {
        dialoguePanel.SetActive(false);
        rewardPanel.SetActive(false);
        HideChoices();
    }

    void HideChoices()
    {
        foreach (GameObject button in choiceButtons)
        {
            button.SetActive(false);
        }
    }
}
