using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.KeyVault.Fluent;

namespace AzureConfigurationDiff.DiffSecrets
{
    public static class AzureSecretDiffer
    {
        public static IReadOnlyList<DiffItem> DoDiff(IReadOnlyList<ISecret> left, IReadOnlyList<ISecret> right)
        {
            var secretByNameComparer = new SecretByNameComparer();

            var leftOnly = left.Except(right, secretByNameComparer);
            var rightOnly = right.Except(left, secretByNameComparer);
            var common = left.Intersect(right, secretByNameComparer);

            var diffList = new List<DiffItem>();

            diffList.AddRange(leftOnly.Select(leftSecret => new DiffItem(
                DiffType.LeftOnly,
                leftSecret,
                null)));

            diffList.AddRange(rightOnly.Select(rightSecret => new DiffItem(
                DiffType.RightOnly,
                null,
                rightSecret)));

            diffList.AddRange(from leftSecret in common
                let rightSecret = right.Single(r => r.Name == leftSecret.Name)
                let readOnlyList = GetPropertyDifferences(leftSecret, rightSecret)
                select new DiffItem(
                    readOnlyList.Count > 0
                        ? DiffType.Modified
                        : DiffType.Unmodified,
                    leftSecret,
                    rightSecret,
                    readOnlyList));

            return diffList.OrderBy(d => d.OrderBy).ToList();
        }

        private static IReadOnlyList<DiffProperty> GetPropertyDifferences(ISecret x, ISecret y)
        {
            var differences = new List<DiffProperty>();

            Compare(nameof(x.Value), x.Value, y.Value, differences);
            Compare(nameof(x.ContentType), x.ContentType, y.ContentType, differences);

            return differences;
        }

        private static void Compare<T>(string propertyName, T left, T right, ICollection<DiffProperty> differences)
        {
            if (Equals(left, right)) return;

            differences.Add(new DiffProperty(propertyName, left?.ToString(), right?.ToString()));
        }
    }
}