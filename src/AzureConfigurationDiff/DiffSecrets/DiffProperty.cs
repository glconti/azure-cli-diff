namespace AzureConfigurationDiff.DiffSecrets
{
    public record DiffProperty(string PropertyName, string LeftValue, string RightValue);
}