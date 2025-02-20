using UnityEngine;

public class AdjustCanvasLayers : MonoBehaviour
{
    public Canvas playerCanvas; // Assign in Inspector
    public Canvas backgroundCanvas; // Assign in Inspector

    void Start()
    {
        // Set the sorting order for the canvases
        backgroundCanvas.sortingOrder = 0; // Background canvas
        playerCanvas.sortingOrder = 1; // Player controls canvas

        // Ensure player layer is above the UI layer
        // You might need to adjust this based on your specific setup
    }
}
