using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Identity.Extensions;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using AzureAPi = Microsoft.Azure.Management.Fluent.Azure;
using SecretProperties = Azure.Security.KeyVault.Secrets.SecretProperties;

namespace AzureConfigurationDiff.Azure
{
    public class AzureService
    {
        private IAzure _azureClient;
        
        public async Task<string> Login()
        {
            var azureCliCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeSharedTokenCacheCredential = true,
                ExcludeEnvironmentCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true
            });
            var credentials = new AzureIdentityFluentCredentialAdapter(azureCliCredential, "4d6d2f7a-a194-40ac-ae91-06847db8d8a2", AzureEnvironment.AzureGlobalCloud);
            
            _azureClient = await AzureAPi
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscriptionAsync();
    
            return _azureClient.GetCurrentSubscription().DisplayName;
        }

        public async Task<IEnumerable<IVault>> GetKeyVaults() => (await _azureClient.Vaults.ListAsync()).ToList();

        public async Task<List<AzureSecret>> ListKeyVaultSecrets(IVault keyVault)
        {
            var client = new SecretClient(
                new Uri(keyVault.VaultUri),
                new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeEnvironmentCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeVisualStudioCredential = true
                }));

            var secretPropertiesList = new List<SecretProperties>();
            await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
            {
                secretPropertiesList.Add(secretProperties);
            }

            var secrets = new ConcurrentBag<AzureSecret>();
            
            await secretPropertiesList.ParallelForEachAsync(async properties =>
            {
                var keyVaultSecret = (await client.GetSecretAsync(properties.Name)).Value;
                secrets.Add(new AzureSecret(properties, keyVaultSecret.Value));
            });
            
            return secrets.OrderBy(s => s.Name).ToList();
        }
    }
}