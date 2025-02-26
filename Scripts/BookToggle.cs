using UnityEngine;

public class BookToggleUI : MonoBehaviour
{
    public GameObject bookPanel;

    public void ShowBook()
    {
        bookPanel.SetActive(true); // Show the book panel
    }

    public void HideBook()
    {
        bookPanel.SetActive(false); // Hide the book panel
    }
}
