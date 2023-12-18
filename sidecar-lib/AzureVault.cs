using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.Extensions.Azure;

public class AzureVault
{
    DefaultAzureCredential credentialX;
    string VaultPath;

    public AzureVault()
    {
        try{
            VaultPath = Environment.GetEnvironmentVariable("VAULT_PATH")!;
        }
        catch (Exception e)
        {
            throw new Exception("Error in AzureVault.AzureVault: " + e.Message);
        }
        credentialX = new DefaultAzureCredential();
    }
    public async Task<string> GetSecret(string secretName)
    {
        try
        {
            var client = new SecretClient(new Uri(VaultPath), credentialX);
            var secret = await client.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (Exception e)
        {
            throw new Exception("Error in AzureVault.GetSecret: " + e.Message);
        }
    }

}