namespace Mng.Microconnect.Core;

public class Request
{
    public required string MethodName { get; init; }
    public required IReadOnlyCollection<RequestArgument> Arguments { get; init; }

    public record RequestArgument(string Type, object Value);
}