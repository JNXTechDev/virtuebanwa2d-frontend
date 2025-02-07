using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ConfirmationPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private System.Action onConfirmAction;

    private void Start()
    {
        try
        {
            yesButton.onClick.AddListener(OnYesButtonClicked);
            noButton.onClick.AddListener(OnNoButtonClicked);
            gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ConfirmationPopup initialization warning: {ex.Message}");
        }
    }

    public void Show(string message, System.Action onConfirm)
    {
        messageText.text = message;
        onConfirmAction = onConfirm;
        popupPanel.SetActive(true);
    }

    private void Hide()
    {
        popupPanel.SetActive(false);
    }

    private void OnYesButtonClicked()
    {
        onConfirmAction?.Invoke();
        Hide();
    }

    private void OnNoButtonClicked()
    {
        Hide();
    }
}
