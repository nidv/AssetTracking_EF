using AssetTracking_EF.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetTracking_EF.Data;


// Runtime sample-asset seeding
// Offices come from the migration's HasData. Assets are seeded here only when the table is empty
public static class DbInitializer
{
    public static async Task SeedAsync(AssetTrackingDbContext db)
    {
        if (await db.Assets.AnyAsync())
            return;

        List<Office> offices = await db.Offices.ToListAsync();
        Office sweden = offices.First(o => o.Country == "Sweden");
        Office usa = offices.First(o => o.Country == "USA");
        Office germany = offices.First(o => o.Country == "Germany");
        Office turkey = offices.First(o => o.Country == "Turkey");

        DateTime now = DateTime.Now;
        db.Assets.AddRange(
            Make(sweden, AssetType.Laptop, "MacBook", "Pro M2", "SN000001", 1800, 35),
            Make(usa, AssetType.Computer, "Dell", "Optiplex 300", "SN000002", 700, 33),
            Make(germany, AssetType.Phone, "Apple", "iPhone 15", "SN000003", 999, 4),
            Make(turkey, AssetType.Tablet, "Samsung", "Galaxy Tab S9", "SN000004", 650, 31),
            Make(sweden, AssetType.Phone, "Sony", "Xperia 5", "SN000005", 500, 40),
            Make(usa, AssetType.Laptop, "Lenovo", "ThinkPad X1", "SN000006", 1600, 10),
            Make(germany, AssetType.Computer, "ASUS", "ROG Strix", "SN000007", 2200, 34),
            Make(turkey, AssetType.Phone, "Xiaomi", "Redmi Note", "SN000008", 300, 1),
            Make(sweden, AssetType.Tablet, "Apple", "iPad Air", "SN000009", 599, 32),
            Make(usa, AssetType.Computer, "HP", "EliteDesk", "SN000010", 900, 16),
            Make(germany, AssetType.Laptop, "Microsoft", "Surface Laptop", "SN000011", 1300, 29),
            Make(turkey, AssetType.Computer, "Lenovo", "IdeaCentre", "SN000012", 550, 18)
        );
        await db.SaveChangesAsync();
    }

    private static Asset Make(Office office, AssetType type, string brand, string model, string serial, decimal usd, int monthsAgo)
    {
        Asset asset = type switch
        {
            AssetType.Computer => new Computer(),
            AssetType.Laptop => new Laptop(),
            AssetType.Phone => new MobilePhone(),
            AssetType.Tablet => new Tablet(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        asset.Brand = brand;
        asset.Model = model;
        asset.SerialNumber = serial;
        asset.PurchasePriceUsd = usd;
        asset.PurchaseDate = DateTime.Now.AddMonths(-monthsAgo);
        asset.OfficeId = office.Id;
        asset.LocalCurrency = office.Currency;
        return asset;
    }
}
