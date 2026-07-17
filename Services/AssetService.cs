using AssetTracking_EF.Data;
using AssetTracking_EF.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AssetTracking_EF.Services;

// Level 2/3/4 requirement
// Async database operations, LINQ sorting/filtering, reporting, and file export
public class AssetService
{
    private readonly AssetTrackingDbContext _db;

    public AssetService(AssetTrackingDbContext db) => _db = db;

    // -------- Level 1/2 requirement - read, sorted by category then purchase date --------
    public Task<List<Asset>> GetAllAsync() =>
        _db.Assets
           .Include(a => a.Office)
           .OrderBy(a => a.AssetType)
           .ThenBy(a => a.PurchaseDate)
           .ToListAsync();

    public Task<Asset?> GetByIdAsync(int id) =>
        _db.Assets.Include(a => a.Office).FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<Office>> GetOfficesAsync() =>
        _db.Offices.OrderBy(o => o.Id).ToListAsync();

    // -------- Level 1/2 requirement - create/update/delete --------
    public async Task<bool> AddAsync(Asset asset)
    {
        _db.Assets.Add(asset);
        return await TrySaveAsync(); 
    }

    public async Task<bool> UpdateAsync(Asset asset)
    {
        _db.Assets.Update(asset);
        return await TrySaveAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        Asset? asset = await _db.Assets.FindAsync(id);
        if (asset is null)
            return false;
        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync();
        return true;
    }

    // -------- Level 4 requirement - search by brand, model, office, or purchase year --------
    public async Task<List<Asset>> SearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return [];

        term = term.Trim();
        IQueryable<Asset> query = _db.Assets.Include(a => a.Office);

        if (int.TryParse(term, out int year) && term.Length == 4)
            query = query.Where(a => a.PurchaseDate.Year == year);
        else
            query = query.Where(a =>
                a.Brand.Contains(term) ||
                a.Model.Contains(term) ||
                (a.Office != null && a.Office.Name.Contains(term)) ||
                (a.Office != null && a.Office.Country.Contains(term)));

        return await query.OrderBy(a => a.AssetType).ThenBy(a => a.PurchaseDate).ToListAsync();
    }

    // -------- Level 4 requirement - filter: expired / computers / mobiles / by office --------
    public async Task<List<Asset>> FilterAsync(AssetFilter filter)
    {
        List<Asset> all = await GetAllAsync();
        return filter switch
        {
            AssetFilter.Expired => all.Where(a => a.EndOfLifeDate <= DateTime.Today).ToList(),
            AssetFilter.Computers => all.Where(a => a.AssetType == AssetType.Computer || a.AssetType == AssetType.Laptop).ToList(),
            AssetFilter.Mobiles => all.Where(a => a.AssetType == AssetType.Phone || a.AssetType == AssetType.Tablet).ToList(),
            _ => all
        };
    }

    public async Task<List<Asset>> FilterByOfficeAsync(string officeNameOrId)
    {
        string term = officeNameOrId.Trim();
        IQueryable<Asset> query = _db.Assets.Include(a => a.Office);

        // Accept the office number, name, or country.
        // SQL Contains renders as LIKE '%term%', which is case-insensitive under LocalDB's collation
        if (int.TryParse(term, out int officeId))
            query = query.Where(a => a.OfficeId == officeId);
        else
            query = query.Where(a =>
                (a.Office != null && a.Office.Name.Contains(term)) ||
                (a.Office != null && a.Office.Country.Contains(term)));

        return await query.OrderBy(a => a.AssetType).ThenBy(a => a.PurchaseDate).ToListAsync();
    }

    // -------- Level 3 requirement - reporting --------
    public async Task<List<OfficeSummary>> OfficeSummaryAsync() =>
        await _db.Assets
            .Include(a => a.Office)
            .GroupBy(a => new { a.Office!.Id, a.Office.Name, a.Office.Currency })
            .Select(g => new OfficeSummary(
                g.Key.Name,
                g.Key.Currency,
                g.Count(),
                g.Sum(a => a.PurchasePriceUsd)))
            .ToListAsync();

    // Assets close to expiration = status (Yellow/Red).
    public async Task<List<Asset>> NearExpirationAsync() =>
        (await GetAllAsync()).Where(a => a.LifeStatus == LifespanStatus.Yellow || a.LifeStatus == LifespanStatus.Red).ToList();

    public async Task<List<Asset>> MostExpensiveAsync(int top) =>
        await _db.Assets
            .Include(a => a.Office)
            .OrderByDescending(a => a.PurchasePriceUsd)
            .Take(top)
            .ToListAsync();

    // -------- Level 4 requirement - export to TXT/CSV/JSON --------
    public async Task<string> ExportAsync(IEnumerable<Asset> assets, ExportFormat format)
    {
        Directory.CreateDirectory("Exports");
        string fileName = format switch
        {
            ExportFormat.Txt => "assets.txt",
            ExportFormat.Csv => "assets.csv",
            _ => "assets.json"
        };
        string path = Path.Combine("Exports", fileName);
        List<AssetView> rows = assets.Select(ToView).ToList();

        string content = format switch
        {
            ExportFormat.Txt => BuildTxt(rows),
            ExportFormat.Csv => BuildCsv(rows),
            _ => JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true })
        };

        await File.WriteAllTextAsync(path, content);
        return path;
    }

    // -------- helpers --------
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    private static AssetView ToView(Asset a)
    {
        Currency local = a.Office?.Currency ?? a.LocalCurrency;
        return new AssetView(
            a.Id,
            a.AssetType.ToString(),
            a.Brand,
            a.Model,
            a.PurchaseDate.ToString("yyyy-MM-dd"),
            a.PurchasePriceUsd,
            CurrencyConverter.ConvertFromUsd(a.PurchasePriceUsd, local),
            local.ToString(),
            a.Office?.Country ?? "",
            a.SerialNumber,
            a.EndOfLifeDate.ToString("yyyy-MM-dd"));
    }

    private static string BuildTxt(List<AssetView> rows)
    {
        StringBuilder sb = new();
        sb.AppendLine("ID   Type         Brand          Model            Purchased   Price(USD)  Local Price   Currency  Office       Serial Number  Warranty Expiration Date");
        foreach (AssetView r in rows)
            sb.AppendLine($"{r.Id,-4} {r.Type,-12} {r.Brand,-15} {r.Model,-16} {r.PurchaseDate,-11} {r.PriceUsd,11:F2} {r.LocalPrice,13:F2} {r.Currency,-9} {r.Office,-11} {r.Serial,-14} {r.WarrantyExpiration}");
        return sb.ToString();
    }

    private static string BuildCsv(List<AssetView> rows)
    {
        StringBuilder sb = new();
        sb.AppendLine("Id,Type,Brand,Model,Purchase,PriceUsd,LocalPrice,Currency,Office,Serial,WarrantyExpiration");
        foreach (AssetView r in rows)
            sb.AppendLine($"\"{r.Id}\",\"{r.Type}\",\"{r.Brand}\",\"{r.Model}\",\"{r.PurchaseDate}\",\"{r.PriceUsd:F2}\",\"{r.LocalPrice:F2}\",\"{r.Currency}\",\"{r.Office}\",\"{r.Serial}\",\"{r.WarrantyExpiration}\"");
        return sb.ToString();
    }
}

public enum AssetFilter { Computers, Mobiles, Expired }
public enum ExportFormat { Txt, Csv, Json }

public record OfficeSummary(string OfficeName, Currency Currency, int Count, decimal TotalUsd);
public record AssetView(
    int Id, string Type, string Brand, string Model, string PurchaseDate,
    decimal PriceUsd, decimal LocalPrice, string Currency, string Office, string Serial, string WarrantyExpiration);
