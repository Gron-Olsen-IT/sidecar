
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Web;
using System.Security.Cryptography.X509Certificates;
using ILogger = NLog.ILogger;
using NLog.LayoutRenderers;
using Microsoft.AspNetCore.Http;
using System.Net;


namespace sidecar_lib;


public class AuthSidecar
{
    public ILogger logger;
    public readonly IVaultClient vaultClient;
    public readonly string mySecret;
    public readonly string myIssuer;

    public AuthSidecar(ILogger _logger)
    {
        logger = _logger;

        //var EndPoint = "https://vaultservice:8201/";
        var endPoint = Environment.GetEnvironmentVariable("VAULT_ADDR");
        logger.Info("Vault address: " + endPoint);
        if (endPoint == null)
        {
            throw new Exception("Environment variable VAULT_ADDR not set");
        }
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; }
        };

        vaultClient = GetVaultClient(httpClientHandler, endPoint);
        var secrets = GetSecrets(vaultClient, httpClientHandler).Result;
        mySecret = secrets.Item1;
        myIssuer = secrets.Item2;




    }


    public IVaultClient GetVaultClient(HttpClientHandler httpClientHandler, string endPoint)
    {
        // Initialize one of the several auth methods.
        IAuthMethodInfo authMethod = new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000");
        // Initialize settings. You can also set proxies, custom delegates etc. here.
        var vaultClientSettings = new VaultClientSettings(endPoint, authMethod)
        {
            Namespace = "",
            MyHttpClientProviderFunc = handler
            => new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(endPoint)
            }
        };
        // Initialize client with settings.
        IVaultClient vaultClient = new VaultClient(vaultClientSettings);
        return vaultClient;
    }

    public async Task<Tuple<string, string>> GetSecrets(IVaultClient vaultClient, HttpClientHandler httpClientHandler)
    {
        logger.Info("Reading secret at: " + vaultClient.Settings.MyHttpClientProviderFunc(httpClientHandler).BaseAddress);
        Secret<SecretData> kv2Secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "authentication", mountPoint: "secret");
        string mySecret = kv2Secret.Data.Data["Secret"].ToString()!;
        string myIssuer = kv2Secret.Data.Data["Issuer"].ToString()!;
        logger.Info("mySecret: " + mySecret);
        logger.Info("myIssuer: " + myIssuer);
        return new Tuple<string, string>(mySecret, myIssuer);
    }


    public TokenValidationParameters GetTokenValidationParameters()
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret)),
            ValidateIssuer = true,
            ValidIssuer = myIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        return tokenValidationParameters;
    }

    
}
