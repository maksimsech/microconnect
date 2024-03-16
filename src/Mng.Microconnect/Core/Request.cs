namespace Mng.Microconnect.Core;

public class Request
{
    public required string MethodName { get; init; }
    public required IReadOnlyCollection<object> Arguments { get; init; }
}