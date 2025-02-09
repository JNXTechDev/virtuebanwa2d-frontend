using UnityEngine;
using MongoDB.Driver;

public class MongoDBManager : MonoBehaviour
{
    private static MongoDBManager _instance;
    private IMongoDatabase _database;
    private MongoClient _client;

    public static MongoDBManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("MongoDBManager is NULL! Make sure it is attached to a GameObject in the scene.");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Keep the object alive across scenes
            InitializeMongoDB();
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    private void InitializeMongoDB()
    {
        string connectionString = "mongodb+srv://vbdb:abcdefghij@cluster0.8i1sn.mongodb.net/Users?retryWrites=true&w=majority";
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase("Users");

        Debug.Log("MongoDB Connected Successfully!");
    }

    public IMongoDatabase GetDatabase()
    {
        return _database;
    }
}
