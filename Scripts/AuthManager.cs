using UnityEngine;

public class AuthManager : MonoBehaviour
{
    private static AuthManager _instance;
    private string _currentUsername;
    private bool _isAuthenticated;

    public static AuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AuthManager");
                _instance = go.AddComponent<AuthManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public string currentUsername => _currentUsername;
    public bool isAuthenticated => _isAuthenticated;

    public void Login(string username)
    {
        _currentUsername = username;
        _isAuthenticated = true;
        Debug.Log($"User logged in: {username}");
    }

    public void Logout()
    {
        _currentUsername = null;
        _isAuthenticated = false;
        Debug.Log("User logged out");
    }
}
