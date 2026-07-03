namespace EFCore.SaveOnly.Library;

public interface IIsNew
{
    /// <summary>
    /// how do we tell if an entity is "new" -- for example if its int/long Id is 0
    /// </summary>
    Func<object, bool> IsNewConvention { get; }
}
