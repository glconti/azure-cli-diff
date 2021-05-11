using System.Threading.Tasks;
using AzureConfigurationDiff.Azure;

namespace AzureConfigurationDiff
{
    public static class Program
    {
        public static async Task Main() => await new App(new AzureService()).Run();
    }
}
