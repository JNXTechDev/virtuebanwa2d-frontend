using MongoDB.Driver;
using MongoDB.Bson;
using UnityEngine;
using System.Collections;
using System; // Add this for TimeSpan

public class MongoDBConnection : MonoBehaviour
{
    private MongoClient client;
    private IMongoDatabase database;

    // Connection string (can be loaded from a config file or environment variable)
    private string connectionString = "mongodb+srv://vbdb:abcdefghij@cluster0.8i1sn.mongodb.net/Users?retryWrites=true&w=majority&tls=true";

    private const int MaxRetries = 3;
    private const int RetryDelay = 1000; // Delay in milliseconds

    void Start()
    {
        StartCoroutine(ConnectToMongoDB());
        StartCoroutine(CheckConnection());
    }

    private IEnumerator ConnectToMongoDB()
    {
        int retryCount = 0;
        while (retryCount < MaxRetries)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5); // Timeout for server selection
                settings.ConnectTimeout = TimeSpan.FromSeconds(5); // Timeout for connection
                settings.SocketTimeout = TimeSpan.FromSeconds(5); // Timeout for socket operations
                settings.IPv6 = false; // Disable IPv6 if not needed

                client = new MongoClient(settings);
                database = client.GetDatabase("Users");

                Debug.Log("MongoDB connection successful!");

                var collections = database.ListCollectionNames().ToList();
                Debug.Log("Collections: " + string.Join(", ", collections));

                yield break; // Exit the coroutine on success
            }
            catch (System.Exception ex)
            {
                retryCount++;
                Debug.LogWarning($"Attempt {retryCount} failed: {ex.Message}");
                if (retryCount >= MaxRetries)
                {
                    Debug.LogError("MongoDB connection failed after multiple attempts.");
                }
            }

            yield return new WaitForSeconds(RetryDelay / 1000f); // Wait before retrying
        }
    }

    private IEnumerator CheckConnection()
    {
        while (true)
        {
            yield return new WaitForSeconds(30); // Check every 30 seconds

            try
            {
                var pingResult = database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Debug.Log("MongoDB connection is active.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("MongoDB connection lost: " + ex.Message);
                StartCoroutine(ConnectToMongoDB()); // Attempt to reconnect
            }
        }
    }
}