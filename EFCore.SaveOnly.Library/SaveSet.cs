namespace EFCore.SaveOnly.Library;

public class SaveSet(Func<object, bool> isNew)
{
    private readonly List<object> _inserts = [];    
    private readonly List<object> _updates = [];
    private readonly List<(object, string[])> _columnUpdates = [];
    private readonly List<object> _deletes = [];
    private readonly Func<object, bool> _isNew = isNew;

    public void Save<T>(T entity, params string[] properties) where T : class
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

        var insert = _isNew(entity);
        if (insert)
        {
            _inserts.Add(entity);
        }
        else
        {
            _updates.Add(entity);
        }        
    }

    public void Save<T>(IEnumerable<T> entities) where T : class
    {
        foreach (var entity in entities) SaveInner(entity);
    }

    public void Delete<T>(T entity) where T : class
    {
        _deletes.Add(entity);        
    }

    public object[] Inserts => [.. _inserts];

    public (object, string[])[] ColumnUpdates => [.. _columnUpdates];

    public object[] RowUpdates => [.. _updates];

    public object[] Deletes => [.. _deletes];
}
