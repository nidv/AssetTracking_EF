using AssetTracking_EF.Models;
using AssetTracking_EF.Services;
using System.Text;

namespace AssetTracking_EF.Menu;

// console UI 
public class Menu
{
    private readonly AssetService _assets;

    public Menu(AssetService assets) => _assets = assets;

    public async Task StartAsync()
    {
        bool running = true;
        while (running)
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("        ASSET TRACKING SYSTEM");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("1) Add Asset");
            Console.WriteLine("2) View All Assets");
            Console.WriteLine("3) Update Asset");
            Console.WriteLine("4) Remove Asset");
            Console.WriteLine("5) Search Assets");
            Console.WriteLine("6) Filter Assets");
            Console.WriteLine("7) Reports");
            Console.WriteLine("8) Export to File");
            Console.WriteLine("0) Exit");
            Console.WriteLine("----------------------------------------");
            Console.Write("Select an option: ");

            string choice = Console.ReadLine()?.Trim() ?? "";
            switch (choice)
            {
                case "1": await AddAssetForm(); break;
                case "2": await ViewAll(); break;
                case "3": await UpdateAssetForm(); break;
                case "4": await RemoveAssetForm(); break;
                case "5": await SearchForm(); break;
                case "6": await FilterMenu(); break;
                case "7": await ReportsMenu(); break;
                case "8": await ExportMenu(); break;
                case "0": running = false; Console.WriteLine("Goodbye!"); break;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    // 1) ADD
    private async Task AddAssetForm()
    {
        Console.Clear();
        Console.WriteLine("--- Add New Asset ---");

        AssetType? type = PromptAssetType();
        if (type is null) return;

        Office? office = await PickOfficeAsync(false);
        if (office is null) return;

        Asset asset = Instantiate(type.Value);
        asset.Brand = PromptString("Brand");
        asset.Model = PromptString("Model");
        asset.SerialNumber = PromptSerial();
        asset.PurchasePriceUsd = PromptDecimal("Purchase Price (USD)");
        asset.PurchaseDate = PromptDate("Purchase Date (YYYY-MM-DD)");
        asset.OfficeId = office.Id;
        asset.LocalCurrency = office.Currency;

        bool ok = await _assets.AddAsync(asset);
        Console.WriteLine(ok
            ? $"Saved {asset.AssetType}: {asset.Brand} {asset.Model} (ID {asset.Id})."
            : "Save failed: serial number already exists or input was invalid.");
        Pause();
    }

    // 2) VIEW ALL (sorted by category then purchase date, colored by lifespan)
    private async Task ViewAll()
    {
        Console.Clear();
        Console.WriteLine("--- All Assets (sorted by category, then purchase date) ---\n");
        PrintAssets(await _assets.GetAllAsync());
        Pause();
    }

    // 3) UPDATE
    private async Task UpdateAssetForm()
    {
        Console.Clear();
        Console.WriteLine("--- Update Asset ---\n");
        PrintAssets(await _assets.GetAllAsync());

        int id = PromptInt("Asset ID to update");
        Asset? asset = await _assets.GetByIdAsync(id);
        if (asset is null)
        {
            Console.WriteLine("Asset not found.");
            Pause();
            return;
        }

        Console.WriteLine("  Leave a field blank to keep the current value.");
        asset.Brand = PromptStringDefault("Brand", asset.Brand);
        asset.Model = PromptStringDefault("Model", asset.Model);
        asset.SerialNumber = PromptSerialDefault("Serial", asset.SerialNumber);
        asset.PurchasePriceUsd = PromptDecimalDefault("Price (USD)", asset.PurchasePriceUsd);
        asset.PurchaseDate = PromptDateDefault("Purchase Date", asset.PurchaseDate);

        Office? office = await PickOfficeAsync(true);
        if (office is not null)
        {
            asset.OfficeId = office.Id;
            asset.LocalCurrency = office.Currency;
        }

        bool ok = await _assets.UpdateAsync(asset);
        Console.WriteLine(ok ? "Asset updated." : "Update failed: serial number already exists.");
        Pause();
    }

    // 4) REMOVE
    private async Task RemoveAssetForm()
    {
        Console.Clear();
        Console.WriteLine("--- Remove Asset ---\n");
        PrintAssets(await _assets.GetAllAsync());

        int id = PromptInt("Asset ID to remove");
        bool deleted = await _assets.DeleteAsync(id);
        Console.ForegroundColor = deleted ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(deleted ? $"Asset {id} deleted." : $"Asset {id} not found.");
        Console.ResetColor();
        Pause();
    }

    // 5) SEARCH (brand, model, office, or purchase year)
    private async Task SearchForm()
    {
        Console.Clear();
        Console.WriteLine("--- Search Assets ---");
        Console.WriteLine("Search by brand, model, office, or a 4-digit purchase year.\n");
        PrintAssets(await _assets.SearchAsync(PromptString("Search term")));
        Pause();
    }

    // 6) FILTER
    private async Task FilterMenu()
    {
        Console.Clear();
        Console.WriteLine("--- Filter Assets ---");
        Console.WriteLine("1) Expired assets");
        Console.WriteLine("2) Computers only");
        Console.WriteLine("3) Mobile devices only");
        Console.WriteLine("4) By office");
        Console.Write("Choose: ");
        string choice = Console.ReadLine()?.Trim() ?? "";

        List<Asset> results = choice switch
        {
            "1" => await _assets.FilterAsync(AssetFilter.Expired),
            "2" => await _assets.FilterAsync(AssetFilter.Computers),
            "3" => await _assets.FilterAsync(AssetFilter.Mobiles),
            "4" => await FilterByOfficeFlowAsync(),
            _ => new()
        };
        PrintAssets(results);
        Pause();
    }

    private async Task<List<Asset>> FilterByOfficeFlowAsync()
    {
        List<Office> offices = await _assets.GetOfficesAsync();
        Console.WriteLine("\nOffices:");
        foreach (Office o in offices)
            Console.WriteLine($"  {o.Id}) {o.Name} ({o.Country})");
        string name = PromptString("Office number, name, or country");
        return await _assets.FilterByOfficeAsync(name);
    }

    // 7) REPORTS (value per office, count per office, near expiration, most expensive)
    private async Task ReportsMenu()
    {
        Console.Clear();
        Console.WriteLine("--- Reports ---");
        Console.WriteLine("1) Total Asset Value & Count per office");
        Console.WriteLine("2) Assets close to expiration");
        Console.WriteLine("3) Most expensive assets");
        Console.Write("Choose: ");
        string choice = Console.ReadLine()?.Trim() ?? "";

        switch (choice)
        {
            case "1":
                PrintOfficeSummary(await _assets.OfficeSummaryAsync());
                break;
            case "2":
                PrintAssets(await _assets.NearExpirationAsync());
                break;
            case "3":
                PrintAssets(await _assets.MostExpensiveAsync(5));
                break;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
        Pause();
    }

    // 8) EXPORT (TXT / CSV / JSON)
    private async Task ExportMenu()
    {
        Console.Clear();
        List<Asset> assets = await _assets.GetAllAsync();
        Console.WriteLine("--- Export Assets ---");
        Console.WriteLine("1) TXT  2) CSV  3) JSON");
        Console.Write("Choose: ");
        ExportFormat format = (Console.ReadLine()?.Trim()) switch
        {
            "1" => ExportFormat.Txt,
            "2" => ExportFormat.Csv,
            "3" => ExportFormat.Json,
            _ => ExportFormat.Txt
        };

        string path = await _assets.ExportAsync(assets, format);
        Console.WriteLine($"Exported {assets.Count} assets to: {Path.GetFullPath(path)}");
        Pause();
    }

    // -------- printing (reused by every view) --------
    private void PrintAssets(List<Asset> assets)
    {
        if (assets.Count == 0)
        {
            Console.WriteLine("No assets found.");
            return;
        }

        Console.WriteLine(
            "ID".PadRight(6) +
            "Type".PadRight(12) +
            "Brand".PadRight(14) +
            "Model".PadRight(16) +
            "Purchase Date".PadRight(15) +
            "Price (USD)".PadRight(13) +
            "Local Price".PadRight(14) +
            "Currency".PadRight(10) +
            "Office".PadRight(10) +
            "Serial".PadRight(10) +
            "Warranty Expiration Date");
        Console.WriteLine(new string('-', 128));

        foreach (Asset a in assets)
        {
            Console.ForegroundColor = a.LifeStatus switch
            {
                LifespanStatus.Expired => ConsoleColor.DarkMagenta,
                LifespanStatus.Yellow => ConsoleColor.Yellow,
                LifespanStatus.Red => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };

            Currency local = a.Office?.Currency ?? a.LocalCurrency;
            decimal localPrice = CurrencyConverter.ConvertFromUsd(a.PurchasePriceUsd, local);
            string localPriceStr = $"{localPrice:F2}{CurrencyConverter.Symbol(local)}";


            Console.OutputEncoding = Encoding.UTF8; //Required for currency symbols to display
            Console.WriteLine(
                a.Id.ToString().PadRight(6) +
                a.AssetType.ToString().PadRight(12) +
                a.Brand.PadRight(14) +
                a.Model.PadRight(16) +
                a.PurchaseDate.ToString("yyyy-MM-dd").PadRight(15) +
                a.PurchasePriceUsd.ToString("F2").PadRight(13) +
                localPriceStr.PadRight(14) +
                local.ToString().PadRight(10) +
                (a.Office?.Country ?? "N/A").PadRight(10) +
                a.SerialNumber.PadRight(10) +
                a.EndOfLifeDate.ToString("yyyy-MM-dd"));
        }
        Console.ResetColor();
    }

    private void PrintOfficeSummary(List<OfficeSummary> rows)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(
            "Office".PadRight(18) +
            "Currency".PadRight(10) +
            "Assets".PadRight(8) +
            "Total USD (Local)");
        Console.WriteLine(new string('-', 54));
        foreach (OfficeSummary r in rows)
        {
            decimal localTotal = CurrencyConverter.ConvertFromUsd(r.TotalUsd, r.Currency);
            Console.WriteLine(
                r.OfficeName.PadRight(18) +
                r.Currency.ToString().PadRight(10) +
                r.Count.ToString().PadRight(8) +
                $"{r.TotalUsd:F2}  ({localTotal:F2}{CurrencyConverter.Symbol(r.Currency)})");
        }
    }

    // -------- input helpers --------
    private static Asset Instantiate(AssetType type) => type switch
    {
        AssetType.Computer => new Computer(),
        AssetType.Laptop => new Laptop(),
        AssetType.Phone => new MobilePhone(),
        AssetType.Tablet => new Tablet(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private AssetType? PromptAssetType()
    {
        while (true)
        {
            Console.Write("Type (Computer/Laptop/Phone/Tablet): ");
            string input = Console.ReadLine()?.Trim().ToLower() ?? "";
            switch (input)
            {
                case "computer": return AssetType.Computer;
                case "laptop": return AssetType.Laptop;
                case "phone": return AssetType.Phone;
                case "tablet": return AssetType.Tablet;
                default:
                    Console.WriteLine("Invalid type. Please enter Computer, Laptop, Phone, or Tablet.");
                    break;
            }
        }
    }

    private async Task<Office?> PickOfficeAsync(bool allowBlank = false)
    {
        List<Office> offices = await _assets.GetOfficesAsync();
        Console.WriteLine("Offices:");
        foreach (Office o in offices)
            Console.WriteLine($"  {o.Id}) {o.Name} ({o.Country}, {o.Currency})");

        while (true)
        {
            Console.Write("Office number: ");
            string? input = Console.ReadLine();

            if (allowBlank && string.IsNullOrWhiteSpace(input))
                return null;

            if (int.TryParse(input, out int id))
            {
                Office? match = offices.FirstOrDefault(o => o.Id == id);
                if (match is not null) return match;
            }

            Console.WriteLine(allowBlank
                ? "Invalid office number. Please choose one from the list above, or leave blank to keep current."
                : "Invalid office number. Please choose one from the list above.");
        }
    }

    private static string PromptString(string label)
    {
        string? value;
        do
        {
            Console.Write($"{label}: ");
            value = Console.ReadLine()?.Trim();
        } while (string.IsNullOrWhiteSpace(value));
        return value;
    }

    private static string PromptStringDefault(string label, string current)
    {
        Console.Write($"{label} [{current}]: ");
        string? value = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? current : value;
    }

    // Serial number: up to 8 characters, unique at the DB level.
    private static string PromptSerial()
    {
        while (true)
        {
            string value = PromptString("Serial Number (up to 8 chars)");
            if (value.Length <= 8) return value;
            Console.WriteLine("Serial number must be 8 characters or fewer.");
        }
    }

    private static string PromptSerialDefault(string label, string current)
    {
        while (true)
        {
            Console.Write($"{label} [{current}]: ");
            string? value = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return current;
            if (value.Length <= 8) return value;
            Console.WriteLine("Serial number must be 8 characters or fewer.");
        }
    }

    private static decimal PromptDecimal(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (decimal.TryParse(Console.ReadLine()?.Trim(), out decimal value) && value > 0)
                return value;
            Console.WriteLine("Enter a valid positive number.");
        }
    }

    private static decimal PromptDecimalDefault(string label, decimal current)
    {
        while (true)
        {
            Console.Write($"{label} [{current}]: ");
            string? value = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return current;
            if (decimal.TryParse(value, out decimal result) && result > 0) return result;
            Console.WriteLine("Enter a valid positive number.");
        }
    }

    private static DateTime PromptDate(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime value)) return value;
            Console.WriteLine("Invalid date format. Use YYYY-MM-DD.");
        }
    }

    private static DateTime PromptDateDefault(string label, DateTime current)
    {
        Console.Write($"{label} [{current:yyyy-MM-dd}]: ");
        return DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime value) ? value : current;
    }

    private static int PromptInt(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int value)) return value;
            Console.WriteLine("Enter a valid integer.");
        }
    }

    private static void Pause()
    {
        Console.WriteLine("\nPress any key to return to the menu...");
        Console.ReadKey();
    }
}
