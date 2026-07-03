using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.SaveOnly.Library;

public static class SaveOnlyExtension
{
    /// <summary>
    /// saves changes from a SaveSet using an explicit SaveChangesAsync delegate -- giving you direct control over the actual save method
    /// </summary>
    public static async Task<int> SaveOnlyAsync<TDbContext>(
        this TDbContext dbContext,
        Action<SaveSet> configure,
        Func<TDbContext, Task<int>> saveDelegate) where TDbContext : DbContext
    {
        dbContext.ChangeTracker.Clear();

        SaveSet saveSet = new();
        configure.Invoke(saveSet);

        foreach (var entity in saveSet.Saves)
        {
            var state = IsNewEntity(dbContext, entity) ? EntityState.Added : EntityState.Modified;
            AttachState(dbContext, entity, state);
        }

        foreach (var (entity, properties) in saveSet.ColumnUpdates)
        {
            if (IsNewEntity(dbContext, entity))
            {
                throw new InvalidOperationException($"Cannot perform a column-specific update on a new entity of type {entity.GetType().Name}. Use Save(entity) without properties.");
            }

            EntityEntry entry = AttachState(dbContext, entity, EntityState.Unchanged);

            foreach (var prop in properties)
            {
                entry.Property(prop).IsModified = true;
            }
        }

        foreach (var del in saveSet.Deletes)
        {
            AttachState(dbContext, del, EntityState.Deleted);
        }

        return await saveDelegate(dbContext);
    }

    /// <summary>
    /// saves changes from a SaveSet using an ordinary SaveChangesAsync
    /// </summary>
    public static async Task<int> SaveOnlyAsync<TDbContext>(
        this TDbContext dbContext,
        Action<SaveSet> configure) where TDbContext : DbContext =>
        await SaveOnlyAsync(dbContext, configure, async db => await db.SaveChangesAsync());

    private static bool IsNewEntity(DbContext dbContext, object entity)
    {
        var entityType = dbContext.Model.FindEntityType(entity.GetType())
            ?? throw new InvalidOperationException($"Type '{entity.GetType().Name}' is not registered in the DbContext model.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Type '{entity.GetType().Name}' has no primary key defined.");

        foreach (var property in primaryKey.Properties)
        {
            var value = property.PropertyInfo!.GetValue(entity);
            var clrDefault = property.ClrType.IsValueType ? Activator.CreateInstance(property.ClrType) : null;
            if (!Equals(value, clrDefault)) return false;
        }

        return true;
    }

    private static EntityEntry AttachState(DbContext dbContext, object entity, EntityState state)
    {
        EntityEntry entry = dbContext.Entry(entity);

        if (entry.State != EntityState.Detached)
        {
            throw new InvalidOperationException($"Entity of type {entity.GetType().Name} has already been added to the save set.");
        }

        entry.State = state;
        return entry;
    }
}
