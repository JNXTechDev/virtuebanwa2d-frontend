using UnityEngine;

public class MongoDBConfig : MonoBehaviour
{
    void Start()
    {
        // ✅ Save MONGO_URI in PlayerPrefs
        PlayerPrefs.SetString("MONGO_URI", "mongodb+srv://vbdb:abcdefghij@cluster0.8i1sn.mongodb.net/Users?retryWrites=true&w=majority");
        PlayerPrefs.Save();
        Debug.Log("MONGO_URI saved in PlayerPrefs.");
    }
}
