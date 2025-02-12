using UnityEngine;
using MongoDB.Driver;

public class MongoDBConfig : MonoBehaviour
{
    private static bool initialized = false;
    private static MongoDBConfig instance;
    private IMongoDatabase database;
    private MongoClient client;
    public bool IsInitialized => initialized;
    public IMongoDatabase Database => database;

    public static MongoDBConfig Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("MongoDBConfig instance is null!");
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            if (!initialized)
            {
                SetupMongoDBConfig();
                initialized = true;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupMongoDBConfig()
    {
        const string connectionString = "mongodb+srv://vbdb:abcdefghij@cluster0.8i1sn.mongodb.net/Users?retryWrites=true&w=majority";
        
        try
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            client = new MongoClient(settings);
            database = client.GetDatabase("Users");
            PlayerPrefs.SetString("MONGO_URI", connectionString);
            PlayerPrefs.Save();
            Debug.Log("✅ MongoDB initialized successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ MongoDB initialization failed: {ex.Message}");
            initialized = false;
        }
    }

    // Method to get collection with proper type
    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        if (!initialized || database == null)
        {
            Debug.LogError("Database not initialized!");
            return null;
        }
        return database.GetCollection<T>(collectionName);
    }
}
