using UnityEngine;

public class MongoDBConfig : MonoBehaviour
{
    private static bool initialized = false;
    private static MongoDBConfig instance;

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
        
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("MONGO_URI", "")))
        {
            PlayerPrefs.SetString("MONGO_URI", connectionString);
            PlayerPrefs.Save();
            Debug.Log("✅ MongoDB connection string saved to PlayerPrefs");
        }
        else
        {
            Debug.Log("ℹ️ MongoDB connection string already exists in PlayerPrefs");
        }
    }
}
