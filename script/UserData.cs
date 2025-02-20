[System.Serializable]
public class RewardData
{
    public string fullName;
    public string reward;
    public string message;
    //public System.DateTime date;
}

[System.Serializable]
public class UserData
{
    public string Username;
    public string Role;
    
    // Optional fields (only used for teachers and students)
    public string Password;
    public string Section;
    public string FirstName;
    public string LastName;
    public string Character;
    public string AdminApproval;
    public string Progress;
    public RewardData[] rewards_collected; // Changed to match MongoDB schema

    public static UserData CurrentUser { get; set; }

    public static void SetAdminData(string username)
    {
        CurrentUser = new UserData
        {
            Username = username,
            Role = "Admin"
        };
    }

    // Method to set the current user's data
    public static void SetUserData(
        string username,
        string password,
        string role,
        string section,
        string firstName,
        string lastName,
        string character,
        string adminApproval = "Pending",
        string progress = "",
        RewardData[] rewards = null) // <-- Update parameter type to array
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
            AdminApproval = adminApproval,
            Progress = progress,
            rewards_collected = rewards ?? new RewardData[0] // Ensure it's never null
        };
    }
}