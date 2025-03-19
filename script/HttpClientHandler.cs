using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

public static class CustomHttpHandler
{
    public static HttpClientHandler GetInsecureHandler()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return handler;
    }
}
