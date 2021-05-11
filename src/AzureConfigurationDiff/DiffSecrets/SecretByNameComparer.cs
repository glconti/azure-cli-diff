using System.Collections.Generic;
using Microsoft.Azure.Management.KeyVault.Fluent;

namespace AzureConfigurationDiff.DiffSecrets
{
    internal class SecretByNameComparer : IEqualityComparer<ISecret>
    {
        public bool Equals(ISecret x, ISecret y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;

            return x.Name == y.Name;
        }

        public int GetHashCode(ISecret obj) => obj.Name.GetHashCode();
    }
}