using UnityEngine;
using MongoDB.Driver;
using System.Collections;

public class MongoDBConnection : MonoBehaviour
{
    private MongoClient client;
    private IMongoDatabase database;
    private string connectionString;

    private void Start()
    {
        // Load MONGO_URI from PlayerPrefs
        connectionString = PlayerPrefs.GetString("MONGO_URI", "");

        if (string.IsNullOrEmpty(connectionString))
        {
            Debug.LogError("❌ MongoDB initialization failed: MONGO_URI is not set in PlayerPrefs.");
            return;
        }

        StartCoroutine(ConnectToMongoDB());
    }

    private IEnumerator ConnectToMongoDB()
    {
        try
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            client = new MongoClient(settings);
            database = client.GetDatabase("Users"); // Change this if needed

            Debug.Log("✅ MongoDB connection successful!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ MongoDB connection failed: {ex.Message}");
        }
        yield break;
    }
}