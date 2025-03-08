using UnityEngine;

public class TutorialProgressManager : MonoBehaviour
{
    public string[] NpcNames; // Array to hold NPC names
    public GameObject directionArrow;
    public Transform markNPCLocation;
    public Transform annieNPCLocation;
    public Transform schoolEntranceLocation;

    private bool talkedToMark = false;
    private bool talkedToAnnie = false;

    void Start()
    {
        if (directionArrow != null)
        {
            // Start by pointing to Mark
            UpdateArrowTarget(markNPCLocation);
        }
    }

    public void OnTutorialProgress(string npcName)
    {
        if (npcName == "Mark")
        {
            talkedToMark = true;
            UpdateArrowTarget(annieNPCLocation);
        }
        else if (npcName == "Annie")
        {
            talkedToAnnie = true;
            UpdateArrowTarget(schoolEntranceLocation);
        }
    }

    private void UpdateArrowTarget(Transform target)
    {
        if (directionArrow != null && target != null)
        {
            directionArrow.SetActive(true);
            directionArrow.transform.position = target.position + Vector3.up * 2f; // Float above target
        }
    }
}
