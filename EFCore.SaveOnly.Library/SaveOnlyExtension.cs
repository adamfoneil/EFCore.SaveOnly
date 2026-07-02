using Microsoft.EntityFrameworkCore;

namespace EFCore.SaveOnly.Library;

public static class SaveOnlyExtension
{
    public static async Task<int> SaveOnlyAsync<TDbContext>(this TDbContext dbContext, Action<SaveSet> configure, Func<TDbContext, Task<int>> saveDelegate) where TDbContext : DbContext, IIsNew
    {
        dbContext.ChangeTracker.Clear();

        SaveSet saveSet = new(dbContext.IsNewConvention);
        configure.Invoke(saveSet);

        foreach (var ins in saveSet.Inserts)
        {

        }

        foreach (var colUpdate in saveSet.ColumnUpdates)
        {

        }

        foreach (var del in saveSet.Deletes)
        {

        }

        return await saveDelegate(dbContext);
    }

    public static async Task<int> SaveOnlyAsync<TDbContext>(this TDbContext dbContext, Action<SaveSet> configure) where TDbContext : DbContext, IIsNew =>
        await SaveOnlyAsync(dbContext, configure, async (db) => await db.SaveChangesAsync());    
}
