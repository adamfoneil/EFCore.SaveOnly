namespace EFCore.SaveOnly.Library;

public class SaveSet
{
    private readonly List<object> _saves = [];
    private readonly List<(object, string[])> _columnUpdates = [];
    private readonly List<object> _deletes = [];

    public void Row<T>(T entity, params string[] properties) where T : class
    {
        if (properties.Any())
        {
            _columnUpdates.Add((entity, properties));
            return;
        }

        SaveInner(entity);
    }

    private void SaveInner<T>(T entity) where T : class
    {
        if (entity is System.Collections.IEnumerable)
            throw new InvalidOperationException(
                $"Save<T>(T entity) was called with a collection type '{typeof(T).Name}'. " +
                $"Use Save<TEntity>(IEnumerable<TEntity>) and specify the element type explicitly.");

        _saves.Add(entity);
    }

    public void Rows<T>(IEnumerable<T> entities) where T : class
    {
        foreach (var entity in entities) SaveInner(entity);
    }

    public void Delete<T>(T entity) where T : class
    {
        _deletes.Add(entity);        
    }

    public object[] Saves => [.. _saves];

    public (object, string[])[] ColumnUpdates => [.. _columnUpdates];

    public object[] Deletes => [.. _deletes];
}
