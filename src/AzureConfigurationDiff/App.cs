using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureConfigurationDiff.Azure;
using AzureConfigurationDiff.DiffSecrets;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Spectre.Console;

namespace AzureConfigurationDiff
{
    public class App
    {
        private readonly AzureService _azureService;

        public App(AzureService azureService) => _azureService = azureService;

        public async Task Run()
        {
            try
            {
                if (!ConsoleCanAcceptKeys()) return;

                await Login();

                var chooseKeyVaults = await ChooseKeyVaults();
                if (chooseKeyVaults is null) return;

                var chooseComparisonType = ChooseComparisonType();

                await CompareKeyVaults(chooseKeyVaults, chooseComparisonType);
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e);
            }
        }

        private Task Login() =>
            AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Default)
                .StartAsync("[yellow]Connecting to Azure[/]", async ctx =>
                {
                    var subscriptionName = await _azureService.Login();

                    AnsiConsole.MarkupLine($"Connected to subscription [green]{subscriptionName}.[/]");
                });

        private static ComparisonType ChooseComparisonType()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Render(new Rule("[yellow]Key Vault comparison type[/]").RuleStyle("grey").LeftAligned());

            var comparisonType = AnsiConsole.Prompt(
                new SelectionPrompt<ComparisonType>()
                    .Title("Which type of comparison you want to run?")
                    .AddChoices(Enum.GetValues<ComparisonType>()));

            AnsiConsole.MarkupLine($"Comparison type: [green]{comparisonType}[/]");

            return comparisonType;
        }

        private async Task<List<IVault>> ChooseKeyVaults()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Render(new Rule("[yellow]Key Vaults[/]").RuleStyle("grey").LeftAligned());

            var keyVaults = await AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Default)
                .StartAsync("[yellow]Retrieving Key Vaults[/]", async ctx =>
                {
                    var retrievedKeyVaults = (await _azureService.GetKeyVaults()).ToList();

                    AnsiConsole.MarkupLine($"Retrieved {retrievedKeyVaults.Count} Key Vaults.");

                    return retrievedKeyVaults;
                });

            var chosenKeyVaults = AnsiConsole.Prompt(
                new MultiSelectionPrompt<IVault>()
                    .PageSize(10)
                    .Title("Which [green]two[/] keyvaults do you want to compare??")
                    .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a choice, [green]<enter>[/] to accept)[/]")
                    .AddChoices(keyVaults)
                    .UseConverter(vault => vault.Name));

            if (chosenKeyVaults.Count == 2)
            {
                AnsiConsole.MarkupLine(
                    $"You selected [green]{chosenKeyVaults[0].Name}[/] and [green]{chosenKeyVaults[1].Name}.[/]");
                return chosenKeyVaults;
            }

            AnsiConsole.MarkupLine("[red]You must select two Key Vaults.[/]");
            return default;
        }

        private async Task CompareKeyVaults(IReadOnlyList<IVault> chooseKeyVaults, ComparisonType comparisonType)
        {
            AnsiConsole.WriteLine();

            var leftKeyVault = chooseKeyVaults[0];
            var rightKeyVault = chooseKeyVaults[1];

            List<AzureSecret> leftSecrets = default;
            List<AzureSecret> rightSecrets = default;

            await AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Default)
                .StartAsync("[yellow]Comparing secrets[/]", async ctx =>
                {
                    leftSecrets = await _azureService.ListKeyVaultSecrets(leftKeyVault);
                    rightSecrets = await _azureService.ListKeyVaultSecrets(rightKeyVault);
                });

            var differences = AzureSecretDiffer.DoDiff(leftSecrets, rightSecrets, comparisonType);
            if (!differences.Any())
            {
                AnsiConsole.Markup($"The key vaults don't have any difference with the comparison type [green]{comparisonType}[/]");
                return;
            }

            AnsiConsole.Render(BuildDiffTable(leftKeyVault, rightKeyVault, differences));
        }

        private static Table BuildDiffTable(
            IHasName leftKeyVault,
            IHasName rightKeyVault,
            IEnumerable<DiffItem> differences)
        {
            var table = new Table()
                .AddColumns($"[grey]{leftKeyVault.Name}[/]", $"[grey]{rightKeyVault.Name}[/]")
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey);

            foreach (var diffItem in differences)
            {
                switch (diffItem.Type)
                {
                    case DiffType.LeftOnly:
                        table.AddRow($"[grey]{diffItem.LeftSecret.Name}[/]", "[red]Missing[/]");
                        break;

                    case DiffType.RightOnly:
                        table.AddRow("[red]Missing[/]", $"[grey]{diffItem.RightSecret.Name}[/]");
                        break;

                    case DiffType.Modified:

                        var leftDiff = BuildLeftPropertyDiff(diffItem);
                        var rightDiff = BuildRightPropertyDiff(diffItem);

                        table.AddRow(leftDiff, rightDiff);
                        break;

                    case DiffType.Unmodified:

                        var leftValue =
                            $"[green]{diffItem.LeftSecret.Name}{Environment.NewLine}{diffItem.LeftSecret.Value}[/]";
                        var rightValue =
                            $"[green]{diffItem.RightSecret.Name}{Environment.NewLine}{diffItem.RightSecret.Value}[/]";

                        table.AddRow(leftValue, rightValue);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                table.AddEmptyRow();
            }

            return table;
        }

        private static string BuildRightPropertyDiff(DiffItem diffItem)
        {
            var rightDiff = new StringBuilder();
            rightDiff.AppendLine($"[yellow]{diffItem.OrderBy}");

            foreach (var diffItemDifference in diffItem.Differences)
            {
                rightDiff.AppendLine($"{diffItemDifference.PropertyName}:{Markup.Escape(diffItem.RightSecret.Value)}");
            }

            rightDiff.AppendLine("[/]");

            return rightDiff.ToString();
        }

        private static string BuildLeftPropertyDiff(DiffItem diffItem)
        {
            var leftDiff = new StringBuilder();
            leftDiff.AppendLine($"[yellow]{diffItem.LeftSecret.Name}");

            foreach (var diffItemDifference in diffItem.Differences)
            {
                leftDiff.AppendLine($"{diffItemDifference.PropertyName}:{Markup.Escape(diffItem.LeftSecret.Value)}");
            }

            leftDiff.AppendLine("[/]");

            return leftDiff.ToString();
        }

        private static bool ConsoleCanAcceptKeys()
        {
            // Check if we can accept key strokes
            if (AnsiConsole.Profile.Capabilities.Interactive) return true;

            AnsiConsole.MarkupLine("[red]Environment does not support interaction.[/]");
            return false;
        }
    }
}