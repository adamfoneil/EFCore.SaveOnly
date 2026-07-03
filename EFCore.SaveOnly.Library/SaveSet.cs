namespace EFCore.SaveOnly.Library;

public class SaveSet
{
    private readonly List<object> _saves = [];
    private readonly List<(object, string[])> _columnUpdates = [];
    private readonly List<object> _deletes = [];

    public SaveSet Row<T>(T entity, params string[] properties) where T : class
    {
        if (properties.Any())
        {
            _columnUpdates.Add((entity, properties));
            return this;
        }

        SaveInner(entity);
        return this;
    }

    private void SaveInner<T>(T entity) where T : class
    {
        if (entity is System.Collections.IEnumerable)
            throw new InvalidOperationException(
                $"Save<T>(T entity) was called with a collection type '{typeof(T).Name}'. " +
                $"Use Save<TEntity>(IEnumerable<TEntity>) and specify the element type explicitly.");

        _saves.Add(entity);
    }

    public SaveSet Rows<T>(IEnumerable<T> entities) where T : class
    {
        foreach (var entity in entities) SaveInner(entity);
        return this;
    }

    public SaveSet Delete<T>(T entity) where T : class
    {
        _deletes.Add(entity);
        return this;
    }

    public object[] Saves => [.. _saves];

    public (object, string[])[] ColumnUpdates => [.. _columnUpdates];

    public object[] Deletes => [.. _deletes];
}
