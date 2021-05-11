using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.KeyVault.Fluent;

namespace AzureConfigurationDiff.DiffSecrets
{
    public static class AzureSecretDiffer
    {
        public static IReadOnlyList<DiffItem> DoDiff(
            IReadOnlyList<ISecret> left,
            IReadOnlyList<ISecret> right,
            ComparisonType comparisonType)
        {
            var secretByNameComparer = new SecretByNameComparer();

            var leftOnly = left.Except(right, secretByNameComparer);
            var rightOnly = right.Except(left, secretByNameComparer);

            var diffList = new List<DiffItem>();

            if (comparisonType is ComparisonType.All or ComparisonType.OnlyMissing)
            {
                diffList.AddRange(leftOnly.Select(leftSecret => new DiffItem(
                    DiffType.LeftOnly,
                    leftSecret,
                    null)));

                diffList.AddRange(rightOnly.Select(rightSecret => new DiffItem(
                    DiffType.RightOnly,
                    null,
                    rightSecret)));
            }

            if (comparisonType is ComparisonType.All or ComparisonType.OnlyModified)
            {
                var common = left.Intersect(right, secretByNameComparer);
                
                diffList.AddRange(from leftSecret in common
                    let rightSecret = right.Single(r => r.Name == leftSecret.Name)
                    let diffProperties = GetPropertyDifferences(leftSecret, rightSecret)
                    where ShouldIncludeDiff(diffProperties)
                    select new DiffItem(
                        diffProperties.Count > 0
                            ? DiffType.Modified
                            : DiffType.Unmodified,
                        leftSecret,
                        rightSecret,
                        diffProperties));
                
                bool ShouldIncludeDiff(IEnumerable<DiffProperty> differences) =>
                    comparisonType is ComparisonType.All ||
                    comparisonType is ComparisonType.OnlyModified && differences.Any();
            }
            
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