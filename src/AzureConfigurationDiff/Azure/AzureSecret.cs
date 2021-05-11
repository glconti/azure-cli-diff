using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Secret.Update;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using SecretProperties = Azure.Security.KeyVault.Secrets.SecretProperties;

namespace AzureConfigurationDiff.Azure
{
    public class AzureSecret : ISecret
    {
        public AzureSecret(SecretProperties secretProperties, string value)
        {
            Name = secretProperties.Name;
            Id = secretProperties.Id.ToString();
            Enabled = secretProperties.Enabled;
            Managed = secretProperties.Managed;
            Value = value;
            // Tags = secretProperties.Tags;
        }

        public string Id { get; }
        public string Name { get; }
        

        public bool? Enabled { get; set; }

        public string Key { get; }
        
        public bool Managed { get; }
        
        public string Kid { get; }
        
        public SecretAttributes Attributes { get; }
        
        public string Value { get; }
        
        public string ContentType { get; }
        
        public IReadOnlyDictionary<string, string> Tags { get; }

        public SecretBundle Inner { get; }

        public IUpdate Update()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ISecret> ListVersions()
        {
            throw new NotImplementedException();
        }

        public Task<IPagedCollection<ISecret>> ListVersionsAsync(CancellationToken cancellationToken = new())
        {
            throw new NotImplementedException();
        }
    }
}