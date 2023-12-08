﻿
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


namespace sidecar_lib;
public class Auth
{
    public string? myIssuer { get; set; }
    public string? mySecret { get; set; }
    public WebApplicationBuilder builder { get; set; }
    public Auth(WebApplicationBuilder _builder, ILogger logger)
    {

        builder = _builder;


        logger.Info("Starting Auth");
        var EndPoint = Environment.GetEnvironmentVariable("VAULT_ADDR");
        logger.Info("Vault address: " + EndPoint);
        if (EndPoint == null)
        {
            throw new Exception("Environment variable VAULT_ADDR not set");
        }

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; }
        };



        logger.Info("Initialize settings " + EndPoint);
        IAuthMethodInfo authMethod = new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000");
        // Initialize settings. You can also set proxies, custom delegates etc. here.
        var vaultClientSettings = new VaultClientSettings(EndPoint, authMethod)
        {
            Namespace = "",
            MyHttpClientProviderFunc = handler
            => new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(EndPoint)
            }
        };

        // Initialize one of the several auth methods.

        // Initialize client with settings.
        IVaultClient vaultClient = new VaultClient(vaultClientSettings);
        // Use client to read a key-value secret.
        GetSecret(logger, vaultClient, httpClientHandler).Wait();


        builder.Services.AddSingleton<IVaultClient>(vaultClient);

        logger.Info("Adding authentication. myIssuer: " + myIssuer + ", mySecret: " + mySecret);

        builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidIssuer = myIssuer,
                ValidateAudience = true,
                ValidAudience = "http://localhost",

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(mySecret))
            };
        });


        logger.Info("Auth done");

    }

    public async Task<Secret<SecretData>> GetSecret(ILogger logger, IVaultClient vaultClient, HttpClientHandler httpClientHandler)
    {
        logger.Info("Reading secret at: " + vaultClient.Settings.MyHttpClientProviderFunc(httpClientHandler).BaseAddress);
        Secret<SecretData> kv2Secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "authentication", mountPoint: "secret");
        mySecret = kv2Secret.Data.Data["Secret"].ToString()!;
        myIssuer = kv2Secret.Data.Data["Issuer"].ToString()!;
        logger.Info("mySecret: " + mySecret);
        logger.Info("myIssuer: " + myIssuer);

        return kv2Secret;
    }

}
