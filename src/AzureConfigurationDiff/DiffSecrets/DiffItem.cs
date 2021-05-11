using System.Collections.Generic;
using Microsoft.Azure.Management.KeyVault.Fluent;

namespace AzureConfigurationDiff.DiffSecrets
{
    public record DiffItem(DiffType Type, ISecret LeftSecret, ISecret RightSecret,
        IEnumerable<DiffProperty> Differences = null)
    {
        public string OrderBy => LeftSecret?.Name ?? RightSecret.Name;
    }
}