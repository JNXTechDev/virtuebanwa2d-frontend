using UnityEngine;
using System.IO;

public class SaveTilemapAsImage : MonoBehaviour
{
    public Camera captureCamera; // Assign your camera here
    public RenderTexture renderTexture; // Assign your RenderTexture here
    public string filePath = "Assets/TilemapImage.png";

    void Start()
    {
        SaveRenderTextureToImage();
    }

    void SaveRenderTextureToImage()
    {
        // Set the camera's target texture
        captureCamera.targetTexture = renderTexture;

        // Render the camera's view
        RenderTexture.active = renderTexture;
        captureCamera.Render();

        // Create a Texture2D to save the image
        // Change to RGBA32 for better quality
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        // Add this line to enable maximum quality
        texture.filterMode = FilterMode.Point;  // Use Point for pixel-perfect results
        texture.Apply();

    // For PNG, quality is automatic, but you can try JPG with max quality if needed
    byte[] bytes = texture.EncodeToPNG();
    // Alternative: byte[] bytes = texture.EncodeToJPG(100);  // 100 = max quality
    
        File.WriteAllBytes(filePath, bytes);

        // Clean up
        RenderTexture.active = null;
        captureCamera.targetTexture = null;

        Debug.Log($"Tilemap saved as image at {filePath}");
    }
}
