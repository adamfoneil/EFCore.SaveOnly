namespace EFCore.SaveOnly.Library;

public interface IIsNew
{
    Func<object, bool> IsNewConvention { get; }
}
