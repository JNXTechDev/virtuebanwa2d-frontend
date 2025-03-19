using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

public class PlayerDataManager : MonoBehaviour
{
    private static PlayerDataManager _instance;
    private string baseUrl => NetworkConfig.BaseUrl;
    
    private string username;
    private string firstName;
    private string lastName;
    private string character;
    private string role;
    private string section;

    public static PlayerDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("PlayerDataManager");
                _instance = go.AddComponent<PlayerDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public async Task LoadPlayerData(string username)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/users/{username}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);
                    JObject user = data["user"] as JObject;
                    
                    this.username = username;
                    this.firstName = user["FirstName"]?.ToString();
                    this.lastName = user["LastName"]?.ToString();
                    this.character = user["Character"]?.ToString();
                    this.role = user["Role"]?.ToString();
                    this.section = user["Section"]?.ToString();
                    
                    Debug.Log($"Loaded player data: {firstName} {lastName} ({character})");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading player data: {ex.Message}");
        }
    }

    public string GetUsername() => username;
    public string GetFirstName() => firstName;
    public string GetLastName() => lastName;
    public string GetFullName() => $"{firstName} {lastName}";
    public string GetCharacter() => character;
    public string GetRole() => role;
    public string GetSection() => section;
}
