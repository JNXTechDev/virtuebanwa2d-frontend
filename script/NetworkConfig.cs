using UnityEngine;

public static class NetworkConfig
{
    // Use this for development/local testing

 //   private const string LOCAL_URL = "http://192.168.141.149:5000/api";
    private const string LOCAL_URL = "http://192.168.1.8:5000/api";

     
    
    // Use this for production
    private const string PRODUCTION_URL = "https://vbdb.onrender.com/api";
    
    // Change this to false to use production server
    private static bool useLocalServer = true; //for localhost

  //   private static bool useLocalServer = false; //for deploy using render

    public static string BaseUrl 
    {
        get
        {
            string url = useLocalServer ? LOCAL_URL : PRODUCTION_URL;
            Debug.Log($"Using API URL: {url}");
            return url;
        }
    }
    
    // Method to toggle between local and production servers at runtime
    public static void ToggleServer(bool useLocal)
    {
        useLocalServer = useLocal;
        Debug.Log($"API URL changed to: {BaseUrl}");
    }
}
