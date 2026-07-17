using AssetTracking_EF.Data;
using AssetTracking_EF.Menu;
using AssetTracking_EF.Services;
using Microsoft.EntityFrameworkCore;

using AssetTrackingDbContext db = new();
await db.Database.MigrateAsync();
await DbInitializer.SeedAsync(db);

Menu menu = new(new AssetService(db));
await menu.StartAsync();
