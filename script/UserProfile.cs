using UnityEngine;

public static class UserProfile
{
    public static class CurrentUser
    {
        public static string Character
        {
            get { return PlayerPrefs.GetString("Character", "Boy"); }
            set { PlayerPrefs.SetString("Character", value); }
        }

        public static string Username
        {
            get { return PlayerPrefs.GetString("Username"); }
        }

        public static string Role
        {
            get { return PlayerPrefs.GetString("Role"); }
        }
    }
}
