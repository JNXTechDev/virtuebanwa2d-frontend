[System.Serializable]
public class UserData
{
    public string Username;
    public string Password;
    public string Role;
    public string Section;
    public string FirstName;
    public string LastName;
    public string Character;
    public string Progress;
    public string Rewards;

    // Static instance to store the current user's data
    public static UserData CurrentUser { get; set; }

    // Method to set the current user's data
    public static void SetUserData(
        string username,
        string password,
        string role,
        string section,
        string firstName,
        string lastName,
        string character,
        string progress = "",
        string rewards = "")
    {
        CurrentUser = new UserData
        {
            Username = username,
            Password = password,
            Role = role,
            Section = section,
            FirstName = firstName,
            LastName = lastName,
            Character = character,
            Progress = progress,
            Rewards = rewards
        };
    }
}