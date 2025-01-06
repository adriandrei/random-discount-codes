using Microsoft.EntityFrameworkCore;

namespace DiscountCodes.Api;

public static class Helpers
{
    public static void ApplyMigrations(this IServiceScopeFactory scopeFactory)
    {
        var dbContext = scopeFactory.CreateScope().ServiceProvider.GetService<AppDbContext>();
        dbContext!.Database.Migrate();
    }
}